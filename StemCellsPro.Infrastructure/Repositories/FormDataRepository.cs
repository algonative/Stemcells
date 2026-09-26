using System.Data;
using System.Globalization;
using Dapper;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;
using StemCellsPro.Shared.Exceptions;
using StemCellsPro.Shared.Requests;
using StemCellsPro.Shared.Responses;

namespace StemCellsPro.Infrastructure.Repositories;

public class FormDataRepository : IFormDataRepository
{
    private const string DefaultSchema = "dbo";
    private const int DefaultPageSize = 25;
    private const int MaxPageSize = 100;

    private readonly DapperContext _context;

    public FormDataRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<int>> AddFormAsync(string formName, string userName, Dictionary<string, string> formData, string attachments = "")
    {
        var dataTable = DictionaryToDataTable(formData);

        using var connection = _context.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@FormName", formName, DbType.String, size: 100);
        parameters.Add("@FormData", dataTable.AsTableValuedParameter("dbo.Form_Data"));
        parameters.Add("@Attachment", attachments, DbType.String, size: 50);
        parameters.Add("@AddedBy", userName, DbType.String, size: 500);

        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
          "dbo.FORM_ADD_OPERATION",
          parameters,
          commandType: CommandType.StoredProcedure);

        if (result is null)
        {
            throw new AppException("Form add operation did not return a result.");
        }

        var values = (IDictionary<string, object>)result;
        if (!values.TryGetValue("Data", out var data))
        {
            throw new AppException("Form add operation did not return a record id.");
        }

        var message = values.TryGetValue("STATUS_MESSAGE", out var statusMessage)
            ? Convert.ToString(statusMessage, CultureInfo.InvariantCulture) ?? "Form saved successfully."
            : "Form saved successfully.";

        return new ApiResponse<int>(Convert.ToInt32(data, CultureInfo.InvariantCulture), message);
    }
    // ─── UPDATE FORM (USING FORM_MODIFY_OPERATION SP) ─────────────────────────
    public async Task<ApiResponse<int>> UpdateFormAsync(string formName, int id, string userName, Dictionary<string, string> formData, string attachments = "")
    {
        var dataTable = DictionaryToDataTable(formData);

        using var connection = _context.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@FormName", formName, DbType.String, size: 100);
        parameters.Add("@FormData", dataTable.AsTableValuedParameter("dbo.FORM_DATA"));
        parameters.Add("@FormNo", id, DbType.Int64);
        parameters.Add("@UserName", userName, DbType.String, size: 100);

        var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
          "dbo.FORM_MODIFY_OPERATION",
          parameters,
          commandType: CommandType.StoredProcedure);

        if (result is null)
        {
            throw new AppException("Form modify operation did not return a result.");
        }

        var values = (IDictionary<string, object>)result;
        var statusMessage = values.TryGetValue("STATUS_MESSAGE", out var msg) 
            ? Convert.ToString(msg, CultureInfo.InvariantCulture) 
            : "Form updated successfully.";
            
        // Check RESULT bit if provided
        if (values.TryGetValue("RESULT", out var resBit))
        {
            var isSuccess = false;
            if (resBit is bool b) isSuccess = b;
            else if (resBit is int i) isSuccess = i == 1;
            else if (resBit is string s) isSuccess = s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
            
            if (!isSuccess) throw new AppException(statusMessage);
        }

        return new ApiResponse<int>(id, statusMessage);
    }

    public async Task<int> DeleteFormAsync(string formName, int id, string userName)
    {
        using var connection = _context.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@FormName", formName, DbType.String, size: 100);
        parameters.Add("@FormNo", id, DbType.Int64);
        parameters.Add("@UserName", userName, DbType.String, size: 100);

        return await connection.ExecuteAsync(
            "dbo.FORM_DELETE_OPERATION",
            parameters,
            commandType: CommandType.StoredProcedure);
    }

    public async Task<PagedResponse<IReadOnlyList<Dictionary<string, object?>>>> SearchFormDataAsync(FormSearchRequest request)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? DefaultPageSize : Math.Min(request.PageSize, MaxPageSize);
        var offset = (pageNumber - 1) * pageSize;

        using var connection = _context.CreateConnection();
        var metadata = await LoadFormMetadataAsync(connection, request.FormName);

        var parameters = new DynamicParameters();
        parameters.Add("@Offset", offset, DbType.Int32);
        parameters.Add("@PageSize", pageSize, DbType.Int32);

        var filterClauses = new List<string>();
        for (var index = 0; index < request.Filters.Count; index++)
        {
            filterClauses.Add(BuildFilterClause(request.Filters[index], index, metadata, parameters));
        }

        var whereSql = filterClauses.Count == 0
            ? string.Empty
            : $" WHERE {string.Join(" AND ", filterClauses)}";

        var sortColumn = string.IsNullOrWhiteSpace(request.SortBy)
            ? ResolveDefaultSortColumn(metadata.ActualColumns)
            : ResolveAllowedColumn(request.SortBy, metadata);

        var sortDirection = request.SortDescending ? "DESC" : "ASC";
        var tableName = QuoteQualifiedTable(DefaultSchema, metadata.FormName);
        var selectColumns = string.Join(", ", metadata.ActualColumns.Select(QuoteIdentifier));

        var countSql = $"SELECT COUNT(1) FROM {tableName}{whereSql};";
        var dataSql = $"""
            SELECT {selectColumns}
            FROM {tableName}
            {whereSql}
            ORDER BY {QuoteIdentifier(sortColumn)} {sortDirection}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await connection.QueryAsync(dataSql, parameters);

        var result = rows
            .Select(row => ((IDictionary<string, object>)row)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value == DBNull.Value ? null : item.Value,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();

        return new PagedResponse<IReadOnlyList<Dictionary<string, object?>>>(
            result,
            pageNumber,
            pageSize,
            totalRecords,
            "Data fetched successfully.");
    }

    private DataTable DictionaryToDataTable(Dictionary<string, string> dict)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("COLUMN_NAME", typeof(string));
        dataTable.Columns.Add("COLUMN_VALUE", typeof(string));

        if (dict != null)
        {
            foreach (var item in dict)
            {
                var col = item.Key.ToUpperInvariant();
                // Filter out primary keys and audit columns. The SP handles these natively!
                if (col == "FORM_NO" || col == "ID" || col == "VER_NO" || 
                    col == "ADDED_BY" || col == "ADDED_ON" || col == "CREATED_BY" || col == "CREATED_ON" || col == "CREATEDAT" ||
                    col == "MODIFY_BY" || col == "MODIFY_ON" || col == "MODIFIED_BY" || col == "MODIFIED_ON" || col == "MODIFIEDAT")
                {
                    continue;
                }
                
                // If it is 'Active', it's allowed.
                dataTable.Rows.Add(item.Key, item.Value);
            }
        }
        
        return dataTable;
    }

    private static string BuildFilterClause(
        FormFilterRequest filter,
        int index,
        FormMetadata metadata,
        DynamicParameters parameters)
    {
        var column = ResolveAllowedColumn(filter.Field, metadata);
        var columnSql = QuoteIdentifier(column);
        var parameterName = $"Filter{index}";
        var normalizedOperator = string.IsNullOrWhiteSpace(filter.Operator)
            ? "eq"
            : filter.Operator.Trim().ToLowerInvariant();

        switch (normalizedOperator)
        {
            case "eq":
                if (string.IsNullOrWhiteSpace(filter.Value))
                {
                    return $"{columnSql} IS NULL";
                }

                parameters.Add(parameterName, ConvertToSqlValue(filter.Value, metadata.GetFieldType(column)));
                return $"{columnSql} = @{parameterName}";

            case "neq":
                if (string.IsNullOrWhiteSpace(filter.Value))
                {
                    return $"{columnSql} IS NOT NULL";
                }

                parameters.Add(parameterName, ConvertToSqlValue(filter.Value, metadata.GetFieldType(column)));
                return $"{columnSql} <> @{parameterName}";

            case "contains":
                parameters.Add(parameterName, $"%{EscapeLikeValue(filter.Value ?? string.Empty)}%");
                return $"{columnSql} LIKE @{parameterName} ESCAPE '\\'";

            case "startswith":
                parameters.Add(parameterName, $"{EscapeLikeValue(filter.Value ?? string.Empty)}%");
                return $"{columnSql} LIKE @{parameterName} ESCAPE '\\'";

            case "endswith":
                parameters.Add(parameterName, $"%{EscapeLikeValue(filter.Value ?? string.Empty)}");
                return $"{columnSql} LIKE @{parameterName} ESCAPE '\\'";

            case "gt":
                parameters.Add(parameterName, ConvertToSqlValue(filter.Value, metadata.GetFieldType(column)));
                return $"{columnSql} > @{parameterName}";

            case "gte":
                parameters.Add(parameterName, ConvertToSqlValue(filter.Value, metadata.GetFieldType(column)));
                return $"{columnSql} >= @{parameterName}";

            case "lt":
                parameters.Add(parameterName, ConvertToSqlValue(filter.Value, metadata.GetFieldType(column)));
                return $"{columnSql} < @{parameterName}";

            case "lte":
                parameters.Add(parameterName, ConvertToSqlValue(filter.Value, metadata.GetFieldType(column)));
                return $"{columnSql} <= @{parameterName}";

            case "in":
                return BuildInClause(filter, metadata, column, columnSql, parameterName, parameters);

            case "between":
                return BuildBetweenClause(filter, metadata, column, columnSql, parameterName, parameters);

            default:
                throw new AppException($"Unsupported filter operator '{filter.Operator}' for field '{filter.Field}'.");
        }
    }

    private static string BuildInClause(
        FormFilterRequest filter,
        FormMetadata metadata,
        string column,
        string columnSql,
        string parameterName,
        DynamicParameters parameters)
    {
        if (filter.Values is null || filter.Values.Count == 0)
        {
            throw new AppException($"Filter field '{filter.Field}' requires at least one value.");
        }

        var parameterNames = new List<string>();
        for (var index = 0; index < filter.Values.Count; index++)
        {
            var itemParameterName = $"{parameterName}_{index}";
            parameters.Add(itemParameterName, ConvertToSqlValue(filter.Values[index], metadata.GetFieldType(column)));
            parameterNames.Add($"@{itemParameterName}");
        }

        return $"{columnSql} IN ({string.Join(", ", parameterNames)})";
    }

    private static string BuildBetweenClause(
        FormFilterRequest filter,
        FormMetadata metadata,
        string column,
        string columnSql,
        string parameterName,
        DynamicParameters parameters)
    {
        if (filter.Values is null || filter.Values.Count != 2)
        {
            throw new AppException($"Filter field '{filter.Field}' requires exactly two values for the between operator.");
        }

        var startParameterName = $"{parameterName}_Start";
        var endParameterName = $"{parameterName}_End";

        parameters.Add(startParameterName, ConvertToSqlValue(filter.Values[0], metadata.GetFieldType(column)));
        parameters.Add(endParameterName, ConvertToSqlValue(filter.Values[1], metadata.GetFieldType(column)));

        return $"{columnSql} BETWEEN @{startParameterName} AND @{endParameterName}";
    }

    private static object ConvertToSqlValue(string? value, string fieldType)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DBNull.Value;
        }

        var normalizedType = fieldType.ToUpperInvariant();
        try
        {
            if (normalizedType.Contains("BIT", StringComparison.Ordinal))
            {
                return value is "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       value.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }

            if (normalizedType.Contains("BIGINT", StringComparison.Ordinal))
            {
                return long.Parse(value, CultureInfo.InvariantCulture);
            }

            if (normalizedType.Contains("INT", StringComparison.Ordinal))
            {
                return int.Parse(value, CultureInfo.InvariantCulture);
            }

            if (normalizedType.Contains("DECIMAL", StringComparison.Ordinal) ||
                normalizedType.Contains("NUMERIC", StringComparison.Ordinal) ||
                normalizedType.Contains("MONEY", StringComparison.Ordinal))
            {
                return decimal.Parse(value, CultureInfo.InvariantCulture);
            }

            if (normalizedType.Contains("FLOAT", StringComparison.Ordinal) ||
                normalizedType.Contains("REAL", StringComparison.Ordinal))
            {
                return double.Parse(value, CultureInfo.InvariantCulture);
            }

            if (normalizedType.Contains("DATE", StringComparison.Ordinal) ||
                normalizedType.Contains("TIME", StringComparison.Ordinal))
            {
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
            }
        }
        catch (FormatException)
        {
            throw new AppException($"Value '{value}' is not valid for field type '{fieldType}'.");
        }

        return value;
    }

    private async Task<FormMetadata> LoadFormMetadataAsync(IDbConnection connection, string formName)
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            throw new AppException("Form Name is missing.");
        }

        var definedFields = (await connection.QueryAsync<FormFieldMetadata>(
            """
            SELECT FIELD_NAME AS FieldName, UPPER(FIELD_TYPE) AS FieldType
            FROM form_defs
            WHERE FORM_NAME = @FormName
              AND ACTIVE = 1
            """,
            new { FormName = formName })).ToList();

        if (definedFields.Count == 0)
        {
            throw new AppException($"Form '{formName}' is not registered or has no active fields.");
        }

        var tableExists = await connection.ExecuteScalarAsync<int>(
            "SELECT CASE WHEN OBJECT_ID(@QualifiedName, 'U') IS NULL THEN 0 ELSE 1 END;",
            new { QualifiedName = QuoteQualifiedTable(DefaultSchema, formName) });

        if (tableExists == 0)
        {
            throw new AppException($"Form '{formName}' does not have a backing table.");
        }

        var actualColumns = (await connection.QueryAsync<string>(
            """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @SchemaName
              AND TABLE_NAME = @TableName
            ORDER BY ORDINAL_POSITION
            """,
            new { SchemaName = DefaultSchema, TableName = formName })).ToList();

        if (actualColumns.Count == 0)
        {
            throw new AppException($"Form '{formName}' does not have queryable columns.");
        }

        var fieldTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in definedFields)
        {
            if (!string.IsNullOrWhiteSpace(field.FieldName) && !string.IsNullOrWhiteSpace(field.FieldType))
            {
                fieldTypes[field.FieldName] = field.FieldType;
            }
        }

        foreach (var systemColumn in actualColumns.Where(IsSystemColumn))
        {
            fieldTypes.TryAdd(systemColumn, InferSystemFieldType(systemColumn));
        }

        return new FormMetadata(formName, actualColumns, fieldTypes);
    }

    private static string ResolveAllowedColumn(string? requestedColumn, FormMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(requestedColumn))
        {
            throw new AppException("Filter or sort field is missing.");
        }

        // Smart Mapping: If the frontend generically asks to filter by "Id", 
        // we automatically map it to the actual primary key (e.g., Form_No)
        if (requestedColumn.Equals("Id", StringComparison.OrdinalIgnoreCase) || 
            requestedColumn.Equals("ID", StringComparison.OrdinalIgnoreCase))
        {
            var keyColumn = ResolveKeyColumn(metadata.ActualColumns);
            if (!string.IsNullOrWhiteSpace(keyColumn))
            {
                return keyColumn;
            }
        }

        var actualColumn = metadata.ActualColumns.FirstOrDefault(column =>
            string.Equals(column, requestedColumn, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(actualColumn) || !metadata.FieldTypes.ContainsKey(actualColumn))
        {
            throw new AppException($"Field '{requestedColumn}' is not valid for form '{metadata.FormName}'.");
        }

        return actualColumn;
    }

    private static string ResolveDefaultSortColumn(IReadOnlyList<string> columns)
    {
        var preferredColumn = columns.FirstOrDefault(column =>
            string.Equals(column, "Form_No", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column, "FORM_NO", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column, "ID", StringComparison.OrdinalIgnoreCase));

        return preferredColumn ?? columns[0];
    }

    private static string ResolveKeyColumn(IReadOnlyList<string> columns)
    {
        var keyColumn = columns.FirstOrDefault(column =>
            string.Equals(column, "Form_No", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column, "FORM_NO", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column, "Id", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(column, "ID", StringComparison.OrdinalIgnoreCase));

        keyColumn ??= columns.FirstOrDefault(column =>
            column.EndsWith("_ID", StringComparison.OrdinalIgnoreCase) || 
            column.EndsWith("_NO", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(keyColumn))
        {
            throw new AppException("Unable to resolve the primary key column for this form.");
        }

        return keyColumn;
    }

    private static bool IsSystemColumn(string columnName)
        => columnName.Equals("Form_No", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("FORM_NO", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("CreatedOn", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("AddedAt", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("AddedOn", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("AddedBy", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("ModifiedAt", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("ModifiedOn", StringComparison.OrdinalIgnoreCase) ||
           columnName.Equals("ModifiedBy", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<Dictionary<string, object?>>> GetAuditTrailAsync(string formName, int id)
    {
        using var connection = _context.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@FormName", formName, DbType.String);
        parameters.Add("@RecordId", id, DbType.Int32);

        var rawRows = await connection.QueryAsync<dynamic>(
            "sp_GetFormAuditTrail",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var result = new List<Dictionary<string, object?>>();
        foreach (var row in rawRows)
        {
            var dict = (IDictionary<string, object?>)row;
            result.Add(new Dictionary<string, object?>(dict));
        }

        return result;
    }

    private static string InferSystemFieldType(string columnName)
        => columnName.Equals("Active", StringComparison.OrdinalIgnoreCase)
            ? "BIT"
            : columnName.Equals("Form_No", StringComparison.OrdinalIgnoreCase) ||
              columnName.Equals("FORM_NO", StringComparison.OrdinalIgnoreCase) ||
              columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
              columnName.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
              columnName.EndsWith("_ID", StringComparison.OrdinalIgnoreCase) ||
              columnName.EndsWith("_NO", StringComparison.OrdinalIgnoreCase)
                ? "INT"
                : columnName.Contains("At", StringComparison.OrdinalIgnoreCase) ||
           columnName.Contains("On", StringComparison.OrdinalIgnoreCase)
            ? "DATETIME"
            : "NVARCHAR";

    private static string EscapeLikeValue(string value)
        => value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal)
            .Replace("[", "[[]", StringComparison.Ordinal);

    private static string QuoteQualifiedTable(string schemaName, string tableName)
        => $"{QuoteIdentifier(schemaName)}.{QuoteIdentifier(tableName)}";

    private static string QuoteIdentifier(string identifier)
        => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private sealed class FormFieldMetadata
    {
        public string FieldName { get; init; } = string.Empty;
        public string FieldType { get; init; } = "NVARCHAR";
    }

    private sealed class FormMetadata(
        string formName,
        IReadOnlyList<string> actualColumns,
        IReadOnlyDictionary<string, string> fieldTypes)
    {
        public string FormName { get; } = formName;
        public IReadOnlyList<string> ActualColumns { get; } = actualColumns;
        public IReadOnlyDictionary<string, string> FieldTypes { get; } = fieldTypes;

        public string GetFieldType(string fieldName)
            => FieldTypes.TryGetValue(fieldName, out var fieldType) ? fieldType : "NVARCHAR";
    }
}

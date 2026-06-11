using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;

namespace StemCellsPro.Infrastructure.Repositories;

public class FormDataRepository : IFormDataRepository
{
    private readonly DapperContext _context;

    public FormDataRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<int> AddFormAsync(string formName, string userName, Dictionary<string, string> formData, string attachments = "")
    {
        var dataTable = DictionaryToDataTable(formData);

        using var connection = _context.CreateConnection();
        
        var parameters = new DynamicParameters();
        parameters.Add("@FormName", formName, DbType.String, size: 100);
        parameters.Add("@FormData", dataTable.AsTableValuedParameter("dbo.Form_Data"));
        parameters.Add("@Attachment", attachments, DbType.String, size: 50);
        parameters.Add("@AddedBy", userName, DbType.String, size: 500);

        var result = await connection.ExecuteAsync("dbo.InsertFormData", parameters, commandType: CommandType.StoredProcedure);
        return result;
    }

    public async Task<int> DeleteFormAsync(int id)
    {
        // Assuming a standard delete proc or raw SQL. 
        // We use a raw SQL for now, can be updated to SP.
        var query = "DELETE FROM Forms WHERE Id = @Id";
        using var connection = _context.CreateConnection();
        return await connection.ExecuteAsync(query, new { Id = id });
    }

    public async Task<DataTable> GetFormDataAsync(string query)
    {
        // Executes the raw query passed by the client (similar to the legacy system).
        // WARNING: This is prone to SQL injection. In production, this should be heavily parameterized or sanitized.
        using var connection = _context.CreateConnection() as SqlConnection;
        using var cmd = new SqlCommand(query, connection);
        
        var dataTable = new DataTable();
        using var adapter = new SqlDataAdapter(cmd);
        
        await connection!.OpenAsync();
        adapter.Fill(dataTable);

        return dataTable;
    }

    private DataTable DictionaryToDataTable(Dictionary<string, string> dict)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("Key", typeof(string));
        dataTable.Columns.Add("Value", typeof(string));

        if (dict != null)
        {
            foreach (var item in dict)
            {
                dataTable.Rows.Add(item.Key, item.Value);
            }
        }
        
        return dataTable;
    }
}

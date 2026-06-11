using Dapper;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;

namespace StemCellsPro.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly DapperContext _context;
    private readonly ITenantService _tenantService;

    public AuditService(DapperContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task LogActionAsync(string action, string entityName, string entityId, string details)
    {
        var query = @"
            INSERT INTO AuditLogs (TenantId, Action, EntityName, EntityId, Details, CreatedAt) 
            VALUES (@TenantId, @Action, @EntityName, @EntityId, @Details, @CreatedAt)";

        using var connection = _context.CreateConnection();
        // Fire and forget or await, depending on requirements. Assuming AuditLogs table exists.
        try
        {
            await connection.ExecuteAsync(query, new 
            {
                TenantId = _tenantService.GetTenantId(),
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch
        {
            // Fallback logging if DB fails, so we don't break main flow
        }
    }
}

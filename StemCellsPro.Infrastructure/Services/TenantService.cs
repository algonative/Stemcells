using StemCellsPro.Application.Interfaces;

namespace StemCellsPro.Infrastructure.Services;

public class TenantService : ITenantService
{
    private string _tenantId = string.Empty;
    private string _connectionString = string.Empty;

    public string GetTenantId()
    {
        return _tenantId;
    }

    public void SetTenantId(string tenantId)
    {
        _tenantId = tenantId;
    }

    public string GetConnectionString()
    {
        return _connectionString;
    }

    public void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }
}

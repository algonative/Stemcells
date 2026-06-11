namespace StemCellsPro.Application.Interfaces;

public interface ITenantService
{
    string GetTenantId();
    void SetTenantId(string tenantId);
    string GetConnectionString();
    void SetConnectionString(string connectionString);
}

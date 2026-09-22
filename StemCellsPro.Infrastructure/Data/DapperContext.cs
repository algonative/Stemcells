using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StemCellsPro.Application.Interfaces;

namespace StemCellsPro.Infrastructure.Data;

public class DapperContext
{
    private readonly IConfiguration _configuration;
    private readonly ITenantService _tenantService;
    private readonly string _masterConnectionString;

    public DapperContext(IConfiguration configuration, ITenantService tenantService)
    {
        _configuration = configuration;
        _tenantService = tenantService;
        _masterConnectionString = _configuration.GetConnectionString("DefaultConnection") 
                            ?? throw new Exception("DefaultConnection missing in appsettings");
    }

    public IDbConnection CreateConnection(bool useMaster = false) //master- apicalldb 
    {
        if (useMaster)
        {
            return new SqlConnection(_masterConnectionString);
        }

        var tenantDbStr = _tenantService.GetConnectionString();
        if (!string.IsNullOrEmpty(tenantDbStr))
        {
            return new SqlConnection(tenantDbStr);
        }

        // Fallback to master if no tenant string is set (e.g. during login)
        return new SqlConnection(_masterConnectionString);
    }
        
    public IDbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);
}

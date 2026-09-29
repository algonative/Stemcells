using Dapper;
using StemCellsPro.Application.DTOs;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;
using StemCellsPro.Shared.Exceptions;

namespace StemCellsPro.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly DapperContext _context;
    private readonly ICacheService _cacheService;

    public AuthService(DapperContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<string> GetDbConnectionStringAsync(string token)
    {
        var cacheKey = $"DbConn_{token}";
        var cachedConn = await _cacheService.GetAsync<string>(cacheKey);
        
        if (!string.IsNullOrEmpty(cachedConn))
        {
            return cachedConn;
        }

        var query = "exec [SP_GetDBSettings] @Token";
        using var connection = _context.CreateConnection(useMaster: true);
        var result = await connection.ExecuteScalarAsync<string>(query, new { Token = token });
        var dbConn = result ?? string.Empty;

        if (!string.IsNullOrEmpty(dbConn))
        {
            await _cacheService.SetAsync(cacheKey, dbConn, TimeSpan.FromMinutes(15));
        }

        return dbConn;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        // 1. App-Level Authentication against APICallDB
        // Use system credentials to retrieve the routing token for the provided AppID
        var query = "exec [sp_LoginCheck] @Login, @Password, @AppID";
        using var connection = _context.CreateConnection(useMaster: true);
        
        using var multi = await connection.QueryMultipleAsync(query, new { Login = "admin", Password = "12345", AppID = request.AppID });
        
        var firstTable = await multi.ReadAsync<string>();
        var status = firstTable.FirstOrDefault();

        if (status == "0")
        {
            throw new UnauthorizedException("Failed to authenticate application against Master DB.");
        }
        
        var secondTable = await multi.ReadAsync<string>();
        var token = secondTable.FirstOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            throw new AppException("Token generation failed in the APICallDB.");
        }

        // 2. Resolve the Tenant AppDb Connection String
        var dbConnectionString = await GetDbConnectionStringAsync(token);
        if (string.IsNullOrEmpty(dbConnectionString))
        {
            throw new AppException("Could not resolve Tenant Database connection string from token.");
        }

        // 3. User-Level Authentication against AppDb
        using var tenantConnection = new Microsoft.Data.SqlClient.SqlConnection(dbConnectionString);
        
        var userQuery = @"SELECT UserId AS Id, Username, FullName, Email, IsActive 
                          FROM [dbo].[ApplicationUsers] 
                          WHERE Username = @Username 
                            AND PasswordHash = CONVERT(NVARCHAR(500), HASHBYTES('SHA2_256', CAST(@Password AS VARCHAR(500))), 2) 
                            AND IsActive = 1";
                            
        var user = await tenantConnection.QueryFirstOrDefaultAsync<StemCellsPro.Domain.Entities.User>(
            userQuery,
            new { Username = request.Login, Password = request.Password }
        );

        if (user == null)
        {
            throw new UnauthorizedException("Invalid username or password.");
        }

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            UserId = user.Id
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var query = "exec [sp_ValidateToken] @Token";
        using var connection = _context.CreateConnection(useMaster: true);
        var dbToken = await connection.ExecuteScalarAsync<object>(query, new { Token = token });
        
        if (dbToken == null || dbToken == DBNull.Value) return false;
        
        var resultStr = dbToken.ToString();
        if (string.IsNullOrEmpty(resultStr)) return false;
        
        // If the SP returns the token itself
        if (resultStr == token) return true;
        
        // If the SP returns a BIT or INT 1 (success)
        if (resultStr == "1" || resultStr.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        
        return false;
    }

    public async Task LogoutAsync(string token)
    {
        var query = "exec [sp_revoketoken_logout] @Token";
        using var connection = _context.CreateConnection(useMaster: true);
        await connection.ExecuteAsync(query, new { Token = token });
    }
}

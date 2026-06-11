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
        // 1. Call existing SP to check credentials
        var query = "exec [sp_LoginCheck] @Login, @Password, @AppID";
        using var connection = _context.CreateConnection(useMaster: true);
        
        // Simulating the DataSet return from the legacy code
        // In Dapper, we can read multiple result sets if the SP returns multiple tables.
        using var multi = await connection.QueryMultipleAsync(query, new { request.Login, request.Password, request.AppID });
        
        var firstTable = await multi.ReadAsync<string>();
        var status = firstTable.FirstOrDefault();

        if (status == "0")
        {
            throw new UnauthorizedException("Invalid login details.");
        }
        
        var secondTable = await multi.ReadAsync<string>();
        var token = secondTable.FirstOrDefault();

        if (string.IsNullOrEmpty(token))
        {
            throw new AppException("Token generation failed in the database.");
        }

        return new LoginResponseDto
        {
            Success = true,
            Message = "Login successful.",
            Token = token,
            UserId = 1 // Replace with actual parsing if SP returns it
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var query = "exec [sp_ValidateToken] @Token";
        using var connection = _context.CreateConnection(useMaster: true);
        var dbToken = await connection.ExecuteScalarAsync<string>(query, new { Token = token });
        return dbToken == token;
    }
}

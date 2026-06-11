using System.Security.Claims;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Shared.Exceptions;

namespace StemCellsPro.Api.Middlewares;

public class CustomAuthMiddleware
{
    private readonly RequestDelegate _next;

    public CustomAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService, ITenantService tenantService)
    {
        // Bypass authentication for Swagger assets and endpoints
        if (context.Request.Path != null && (context.Request.Path.StartsWithSegments("/swagger") || context.Request.Path.StartsWithSegments("/api-docs")))
        {
            await _next(context);
            return;
        }
        // Skip auth for login endpoint
        if (context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        // if (string.IsNullOrEmpty(token))
        // {
        //     throw new UnauthorizedException("Token is missing.");
        // }

        // // Validate token using DB
        // var isValid = await authService.ValidateTokenAsync(token);
        // if (!isValid)
        // {
        //     throw new UnauthorizedException("Invalid or expired token.");
        // }

        // For testing without token, hardcode a token or set a dummy one.
        var token = "DUMMY_TEST_TOKEN";
        
        // Also mock the db connection string fetching since it requires a valid token
        // In a real scenario without a token, you'd fallback to a default DB or Master DB.
        var dbConnString = "Server=192.168.1.26;Database=APICallDB;User Id=adminsys;Password=adminsys;TrustServerCertificate=True;"; 

        if (!string.IsNullOrEmpty(dbConnString))
        {
            tenantService.SetConnectionString(dbConnString);
        }
        else
        {
            throw new UnauthorizedException("Unable to resolve database connection for token.");
        }

        // Optionally, fetch user details and set claims
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("Token", token)
        }, "CustomAuth");

        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }
}

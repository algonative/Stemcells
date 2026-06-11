using StemCellsPro.Application.Interfaces;
using StemCellsPro.Shared.Exceptions;

namespace StemCellsPro.Api.Middlewares;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Bypass authentication for Swagger assets and endpoints
        if (context.Request.Path != null && (context.Request.Path.StartsWithSegments("/swagger") || context.Request.Path.StartsWithSegments("/api-docs")))
        {
            await _next(context);
            return;
        }
        // Extract TenantId from header, typical for multi-tenant APIs
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(tenantId))
        {
            tenantService.SetTenantId(tenantId);
        }
        else
        {
            // For a strict multi-tenant app, we might throw if missing.
            // throw new AppException("Tenant Id is required.");
        }

        await _next(context);
    }
}

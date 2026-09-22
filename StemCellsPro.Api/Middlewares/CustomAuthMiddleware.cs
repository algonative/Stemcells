namespace StemCellsPro.Api.Middlewares;

[Obsolete("Use ApiTokenAuthenticationHandler with AddAuthentication/UseAuthentication instead.")]
public class CustomAuthMiddleware
{
    private readonly RequestDelegate _next;

    public CustomAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }
}

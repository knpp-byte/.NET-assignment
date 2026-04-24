namespace TaskManagement.Api.Middleware;

public sealed class TenantMiddleware
{
    private const string TenantHeaderName = "X-Tenant-Id";
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsSwaggerRequest(context))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(TenantHeaderName, out var tenantId)
            || string.IsNullOrWhiteSpace(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Missing X-Tenant-Id header." });
            return;
        }

        context.Items["TenantId"] = tenantId.ToString().Trim();
        await _next(context);
    }

    private static bool IsSwaggerRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/swagger");
    }
}

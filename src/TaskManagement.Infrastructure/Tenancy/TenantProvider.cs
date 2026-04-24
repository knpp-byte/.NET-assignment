using Microsoft.AspNetCore.Http;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Tenancy;

public sealed class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TenantId =>
        _httpContextAccessor.HttpContext?.Items["TenantId"]?.ToString() ?? string.Empty;
}

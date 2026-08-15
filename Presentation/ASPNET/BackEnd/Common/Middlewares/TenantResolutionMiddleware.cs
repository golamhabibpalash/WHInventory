using Application.Common.Tenancy;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ASPNET.BackEnd.Common.Middlewares;

/// <summary>
/// Resolves the ambient tenant for the current request. Must run after UseAuthentication so the
/// TenantId claim is available.
///
/// Order of precedence:
///   1. TenantId claim on an authenticated principal (the normal path).
///   2. Host subdomain, e.g. acme.ustock.app -> slug "acme" (needed at login, before a token exists).
///
/// When both are present they must agree — a token issued for one tenant is rejected on another
/// tenant's host.
/// </summary>
public class TenantResolutionMiddleware
{
    private const string TenantSlugItemKey = "TenantSlug";
    private static readonly string[] NonTenantHostLabels = { "www", "localhost", "app", "api" };

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IMemoryCache cache)
    {
        var slug = ResolveSlug(context);
        if (slug != null)
        {
            context.Items[TenantSlugItemKey] = slug;
        }

        var claimTenantId = context.User?.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(TenantClaimTypes.TenantId)?.Value
            : null;

        if (!string.IsNullOrEmpty(claimTenantId))
        {
            // A host-scoped request must match the token's tenant.
            if (slug != null)
            {
                var hostTenantId = await ResolveTenantIdBySlugAsync(context, cache, slug);
                if (hostTenantId != null && hostTenantId != claimTenantId)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Token does not belong to this tenant.");
                    return;
                }
            }

            tenantContext.SetTenant(claimTenantId);
        }
        else if (slug != null)
        {
            var tenantId = await ResolveTenantIdBySlugAsync(context, cache, slug);
            if (tenantId != null)
            {
                tenantContext.SetTenant(tenantId);
            }
        }

        await _next(context);
    }

    private static string? ResolveSlug(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrEmpty(host)) return null;

        // Bare hosts and IP addresses carry no tenant label.
        if (System.Net.IPAddress.TryParse(host, out _)) return null;

        var labels = host.Split('.');
        if (labels.Length < 2) return null;

        var first = labels[0].ToLowerInvariant();
        if (NonTenantHostLabels.Contains(first)) return null;

        return first;
    }

    private static async Task<string?> ResolveTenantIdBySlugAsync(HttpContext context, IMemoryCache cache, string slug)
    {
        var cacheKey = $"tenant-slug:{slug}";
        if (cache.TryGetValue<string?>(cacheKey, out var cached))
        {
            return cached;
        }

        var dataContext = context.RequestServices.GetRequiredService<QueryContext>();
        var tenantId = await dataContext.Tenant
            .AsNoTracking()
            .Where(x => x.Slug == slug && x.IsActive && !x.IsDeleted)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        cache.Set(cacheKey, tenantId, TimeSpan.FromMinutes(5));
        return tenantId;
    }
}

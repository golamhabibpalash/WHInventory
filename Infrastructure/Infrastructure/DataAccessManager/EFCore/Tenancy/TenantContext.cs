using Application.Common.Tenancy;

namespace Infrastructure.DataAccessManager.EFCore.Tenancy;

/// <summary>
/// Scoped, mutable holder for the ambient tenant. Defaults to "no tenant, not root", which
/// makes every tenant-scoped query return nothing until something resolves a tenant — an
/// unresolved request leaks no data.
/// </summary>
public class TenantContext : ITenantContext
{
    public string? TenantId { get; private set; }

    public bool IsRoot { get; private set; }

    public void SetTenant(string? tenantId)
    {
        TenantId = tenantId;
        IsRoot = false;
    }

    public void SetRootScope()
    {
        TenantId = null;
        IsRoot = true;
    }
}

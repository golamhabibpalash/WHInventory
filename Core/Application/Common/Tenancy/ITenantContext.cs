namespace Application.Common.Tenancy;

/// <summary>
/// Ambient tenant for the current scope. Resolved per request by the tenant middleware,
/// or set explicitly by startup/seed code that has to operate outside a request.
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; }

    /// <summary>
    /// When true the tenant query filters are bypassed. Reserved for platform administration
    /// and startup seeding — never set from a normal user request.
    /// </summary>
    bool IsRoot { get; }

    void SetTenant(string? tenantId);

    void SetRootScope();
}

namespace Application.Common.Tenancy;

public static class TenantDefaults
{
    /// <summary>
    /// Fixed id for the tenant that adopts all pre-multi-tenancy data on upgrade.
    /// </summary>
    public const string DefaultTenantId = "00000000-0000-0000-0000-000000000001";

    public const string DefaultTenantSlug = "default";

    public const string DefaultTenantName = "Default";

    /// <summary>
    /// Host labels that never identify a tenant, so they cannot be claimed as one either. Shared
    /// with the tenant resolution middleware so the two can never disagree.
    /// </summary>
    public static readonly string[] ReservedSlugs =
    {
        "www", "localhost", "app", "api", "admin", "mail", "static", "assets", DefaultTenantSlug
    };
}

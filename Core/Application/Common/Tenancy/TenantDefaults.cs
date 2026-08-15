namespace Application.Common.Tenancy;

public static class TenantDefaults
{
    /// <summary>
    /// Fixed id for the tenant that adopts all pre-multi-tenancy data on upgrade.
    /// </summary>
    public const string DefaultTenantId = "00000000-0000-0000-0000-000000000001";

    public const string DefaultTenantSlug = "default";

    public const string DefaultTenantName = "Default";
}

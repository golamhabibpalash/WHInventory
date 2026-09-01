namespace Application.Common.Services.TenantManager;

public class ProvisionTenantDto
{
    public required string TenantId { get; init; }
    public required string CompanyName { get; init; }
    public required string AdminEmail { get; init; }
    public required string AdminPassword { get; init; }
    public string? AdminFirstName { get; init; }
    public string? AdminLastName { get; init; }

    /// <summary>
    /// When true the administrator must confirm their address before signing in, and a
    /// confirmation link is emailed. Public sign-up sets this from the identity configuration;
    /// a tenant an operator created by hand does not need it, the operator vouched for them.
    /// </summary>
    public bool RequireEmailConfirmation { get; init; }
}

/// <summary>
/// Brings a newly created tenant up to the state a usable installation needs: its own company
/// record, the six system warehouses the inventory ledger posts against, the seeded payment
/// methods, and a first administrator who can sign in.
///
/// Implemented in Infrastructure because it drives the same seeders startup uses and creates an
/// Identity user, neither of which the Application layer can reach.
/// </summary>
public class TenantSignUpPolicyDto
{
    /// <summary>Whether anonymous visitors may create their own organisation.</summary>
    public bool PublicSignUpEnabled { get; init; }

    /// <summary>Whether a new administrator must confirm their address before signing in.</summary>
    public bool RequireEmailConfirmation { get; init; }
}

public interface ITenantProvisioningService
{
    /// <summary>
    /// Installation-level sign-up policy. Read here rather than in the handler so the Application
    /// layer stays free of configuration plumbing.
    /// </summary>
    TenantSignUpPolicyDto GetSignUpPolicy();

    Task ProvisionAsync(ProvisionTenantDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identity holds a single global user table, so an address already in use by any tenant
    /// cannot become the administrator of a new one. Checked before the tenant row is written so
    /// a failure does not leave an unusable tenant behind.
    /// </summary>
    Task<bool> IsEmailAvailableAsync(string email);

    /// <summary>
    /// Active user count per tenant. Lives here because ApplicationUser belongs to Identity and
    /// is not part of IQueryContext, so a query handler cannot reach it.
    /// </summary>
    Task<Dictionary<string, int>> GetUserCountsAsync(IEnumerable<string> tenantIds, CancellationToken cancellationToken = default);
}

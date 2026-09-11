namespace Application.Common.Services.CurrentUserManager;

/// <summary>
/// Read-only view of the caller making the current request, for handlers that need real
/// per-row authorization (e.g. "may this user see this ticket") rather than the page-level,
/// per-module role gate every other feature in this app relies on. No other feature has needed
/// per-row ownership checks before Ticketing, hence this being new rather than pre-existing.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }

    /// <summary>True if the caller holds the given role — same JWT role claims the rest of the
    /// app already issues via ISecurityService's role assignment, just readable from within a
    /// handler instead of only via the [Authorize] attribute.</summary>
    bool IsInRole(string role);
}

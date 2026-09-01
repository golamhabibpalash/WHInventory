using Infrastructure.SecurityManager.NavigationMenu;

namespace Infrastructure.SecurityManager.Roles;

public class RoleHelper
{
    public static List<string> GetAdminRoles()
    {
        var roles = new List<string>();
        roles = NavigationTreeStructure.GetCompleteFirstMenuNavigationSegment();
        return roles;
    }

    //make sure or cross check with NavigationTreeStructure
    public static string GetProfileRole()
    {
        return "Profiles";
    }

    /// <summary>
    /// Roles that administer the platform rather than one organisation. Only the installation's
    /// own administrator holds these; a tenant administrator created by provisioning does not, or
    /// they could reach the tenant registry and every other organisation in it.
    /// </summary>
    public static readonly string[] PlatformRoles = { "Tenants" };

    public static bool IsPlatformRole(string role)
    {
        return PlatformRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}

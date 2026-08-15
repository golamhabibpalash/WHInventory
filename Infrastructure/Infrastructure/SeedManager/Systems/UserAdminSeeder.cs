using Application.Common.Tenancy;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Infrastructure.SeedManager.Systems;

public class UserAdminSeeder
{
    private readonly IdentitySettings _identitySettings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantContext _tenantContext;
    public UserAdminSeeder(
        IOptions<IdentitySettings> identitySettings,
        UserManager<ApplicationUser> userManager,
        ITenantContext tenantContext
        )
    {
        _identitySettings = identitySettings.Value;
        _userManager = userManager;
        _tenantContext = tenantContext;
    }

    public async Task GenerateDataAsync()
    {
        var adminEmail = _identitySettings.DefaultAdmin.Email;
        var adminPassword = _identitySettings.DefaultAdmin.Password;
        var roles = RoleHelper.GetAdminRoles();

        var applicationUser = await _userManager.FindByEmailAsync(adminEmail);

        if (applicationUser == null)
        {
            applicationUser = new ApplicationUser(adminEmail, "Root", "Admin");
            applicationUser.EmailConfirmed = true;
            applicationUser.TenantId = _tenantContext.TenantId;
            await _userManager.CreateAsync(applicationUser, adminPassword);
        }
        else if (string.IsNullOrEmpty(applicationUser.TenantId))
        {
            applicationUser.TenantId = _tenantContext.TenantId;
            await _userManager.UpdateAsync(applicationUser);
        }

        foreach (var role in roles)
        {
            if (!await _userManager.IsInRoleAsync(applicationUser, role))
            {
                await _userManager.AddToRoleAsync(applicationUser, role);
            }
        }
    }
}

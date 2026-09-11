using Application.Common.Services.EmailManager;
using Application.Common.Services.TenantManager;
using Application.Common.Tenancy;
using Infrastructure.SecurityManager.AspNetIdentity;
using Infrastructure.SecurityManager.Roles;
using Infrastructure.SeedManager.Systems;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Encodings.Web;

namespace Infrastructure.TenantManager;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly ITenantContext _tenantContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CompanySeeder _companySeeder;
    private readonly SystemWarehouseSeeder _systemWarehouseSeeder;
    private readonly PaymentMethodSeeder _paymentMethodSeeder;
    private readonly QuickShortcutSeeder _quickShortcutSeeder;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TenantProvisioningService> _logger;
    private readonly IConfiguration _configuration;

    public TenantProvisioningService(
        ITenantContext tenantContext,
        UserManager<ApplicationUser> userManager,
        CompanySeeder companySeeder,
        SystemWarehouseSeeder systemWarehouseSeeder,
        PaymentMethodSeeder paymentMethodSeeder,
        QuickShortcutSeeder quickShortcutSeeder,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TenantProvisioningService> logger,
        IConfiguration configuration
        )
    {
        _tenantContext = tenantContext;
        _userManager = userManager;
        _companySeeder = companySeeder;
        _systemWarehouseSeeder = systemWarehouseSeeder;
        _paymentMethodSeeder = paymentMethodSeeder;
        _quickShortcutSeeder = quickShortcutSeeder;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _configuration = configuration;
    }

    public TenantSignUpPolicyDto GetSignUpPolicy()
    {
        return new TenantSignUpPolicyDto
        {
            PublicSignUpEnabled = _configuration.GetValue<bool>("AllowPublicTenantSignUp"),
            RequireEmailConfirmation = _configuration.GetValue<bool>("AspNetIdentity:SignIn:RequireConfirmedEmail")
        };
    }

    public async Task<bool> IsEmailAvailableAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email.Trim()) == null;
    }

    public async Task<Dictionary<string, int>> GetUserCountsAsync(
        IEnumerable<string> tenantIds,
        CancellationToken cancellationToken = default)
    {
        var ids = tenantIds.ToList();
        if (ids.Count == 0) return new Dictionary<string, int>();

        return await _userManager.Users
            .AsNoTracking()
            .Where(x => x.TenantId != null && ids.Contains(x.TenantId) && x.IsDeleted != true)
            .GroupBy(x => x.TenantId!)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);
    }

    public async Task ProvisionAsync(ProvisionTenantDto request, CancellationToken cancellationToken = default)
    {
        // The seeders below write through the ambient tenant, so the scope has to move to the new
        // tenant for the duration and be put back afterwards - the rest of this request (the audit
        // rows the save writes, most of all) still belongs to the caller's own tenant.
        var callerTenantId = _tenantContext.TenantId;
        var callerWasRoot = _tenantContext.IsRoot;

        try
        {
            _tenantContext.SetTenant(request.TenantId);

            await _companySeeder.GenerateDataAsync(request.CompanyName);
            await _systemWarehouseSeeder.GenerateDataAsync();
            await _paymentMethodSeeder.GenerateDataAsync();
            await _quickShortcutSeeder.GenerateDataAsync();

            await CreateAdministratorAsync(request);
        }
        finally
        {
            if (callerWasRoot)
            {
                _tenantContext.SetRootScope();
            }
            else
            {
                _tenantContext.SetTenant(callerTenantId);
            }
        }
    }

    private async Task CreateAdministratorAsync(ProvisionTenantDto request)
    {
        var user = new ApplicationUser(
            request.AdminEmail,
            string.IsNullOrWhiteSpace(request.AdminFirstName) ? "Tenant" : request.AdminFirstName!,
            string.IsNullOrWhiteSpace(request.AdminLastName) ? "Admin" : request.AdminLastName!,
            request.CompanyName
            )
        {
            TenantId = request.TenantId,
            EmailConfirmed = !request.RequireEmailConfirmation
        };

        var created = await _userManager.CreateAsync(user, request.AdminPassword);

        if (!created.Succeeded)
        {
            throw new Exception(
                $"The tenant was created but its administrator was not: {string.Join(", ", created.Errors.Select(e => e.Description))}");
        }

        // Every module role except the platform-level ones: a tenant administrator runs their own
        // organisation, and must not be able to reach the tenant registry itself.
        var roles = RoleHelper.GetAdminRoles()
            .Where(role => !RoleHelper.IsPlatformRole(role));

        foreach (var role in roles)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        if (request.RequireEmailConfirmation)
        {
            await SendConfirmationEmailAsync(user);
        }
    }

    /// <summary>
    /// A mail failure must not undo the sign-up: the tenant and its administrator already exist,
    /// and the address can be confirmed later from a resent link. Logged loudly instead.
    /// </summary>
    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        try
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var httpRequest = _httpContextAccessor?.HttpContext?.Request;
            var callbackUrl = $"{httpRequest?.Scheme}://{httpRequest?.Host}/Accounts/EmailConfirm?email={user.Email}&code={code}";
            var encodedCallbackUrl = HtmlEncoder.Default.Encode(callbackUrl);

            await _emailService.SendEmailAsync(
                user.Email ?? "",
                "Confirm your email",
                $"Please confirm your account by <a href='{encodedCallbackUrl}'>clicking here</a>.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Tenant sign-up completed but the confirmation email could not be sent. Email={Email} TenantId={TenantId}",
                user.Email, user.TenantId);
        }
    }
}

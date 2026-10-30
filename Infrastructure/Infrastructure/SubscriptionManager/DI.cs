using Application.Common.Services.SubscriptionManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SubscriptionManager;

public static class DI
{
    public static IServiceCollection RegisterSubscriptionManager(this IServiceCollection services, IConfiguration configuration)
    {
        // Scoped so the guard can cache the current tenant's subscription for the request.
        services.AddScoped<ISubscriptionGuard, SubscriptionGuard>();
        services.AddScoped<ISubscriptionQuotaService, SubscriptionQuotaService>();
        services.AddScoped<ITenantSubscriptionAdminService, TenantSubscriptionAdminService>();
        services.AddScoped<ISubscriptionRequestService, SubscriptionRequestService>();

        // Stripe adapter only when enabled and keyed; otherwise billing is manual-only.
        var stripeEnabled = configuration.GetValue<bool>("Stripe:Enabled")
            && !string.IsNullOrWhiteSpace(configuration["Stripe:SecretKey"]);

        if (stripeEnabled)
        {
            services.AddScoped<IBillingGateway, StripeBillingGateway>();
        }
        else
        {
            services.AddScoped<IBillingGateway, NullBillingGateway>();
        }

        return services;
    }
}

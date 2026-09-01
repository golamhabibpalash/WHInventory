using Application.Common.Services.TenantManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.TenantManager;

public static class DI
{
    public static IServiceCollection RegisterTenantManager(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

        return services;
    }
}

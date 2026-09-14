namespace Application.Common.Tenancy;

/// <summary>
/// Temporarily elevates to root scope for a single read so a caller holding the platform-level
/// "Tenants" role (see RoleHelper.PlatformRoles) can view — never write — a ticket belonging to a
/// tenant other than their own. Always restores the caller's original ambient tenant afterward,
/// success or failure. This mirrors the exact elevate-then-restore shape
/// TenantProvisioningService.ProvisionAsync already uses for the one other place in this app that
/// legitimately crosses tenant boundaries mid-request — kept here as a shared helper since
/// several read-only ticket handlers all need the identical wrapper, rather than each repeating
/// the same try/finally.
///
/// Deliberately NOT in Application.Features.* — that whole namespace is blanket-registered into
/// DI by reflection (see Application.DependencyInjection.AddApplicationServices), which also
/// swept up this class's own compiler-generated async state-machine type and crashed the app at
/// startup trying to construct it as a service. Living beside ITenantContext instead (which every
/// call site already references) avoids that scan entirely, with no extra usings needed.
/// </summary>
public static class TicketTenantElevation
{
    public static async Task<T> RunAsync<T>(ITenantContext tenantContext, bool elevate, Func<Task<T>> action)
    {
        if (!elevate)
        {
            return await action();
        }

        var callerTenantId = tenantContext.TenantId;
        var callerWasRoot = tenantContext.IsRoot;
        try
        {
            tenantContext.SetRootScope();
            return await action();
        }
        finally
        {
            if (callerWasRoot)
            {
                tenantContext.SetRootScope();
            }
            else
            {
                tenantContext.SetTenant(callerTenantId);
            }
        }
    }
}

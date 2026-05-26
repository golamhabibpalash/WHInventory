using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ASPNET.BackEnd.Common.Filters;

public class AuditFieldActionFilter : IAsyncActionFilter
{
    private static readonly string[] AuditFields = ["CreatedById", "UpdatedById", "DeletedById"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            foreach (var arg in context.ActionArguments.Values)
            {
                if (arg == null) continue;
                foreach (var fieldName in AuditFields)
                {
                    var prop = arg.GetType().GetProperty(fieldName);
                    if (prop != null && prop.PropertyType == typeof(string))
                        prop.SetValue(arg, userId);
                }
            }
        }
        await next();
    }
}

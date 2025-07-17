namespace LogService.API.Filters;

using System.Security.Claims;

using LogService.SharedKernel.Keys;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class RequireMatchingRoleHeaderAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        const string className = nameof(RequireMatchingRoleHeaderAttribute);

        var user = context.HttpContext.User;
        var jwtRole = user.FindFirst(ClaimTypes.Role)?.Value;
        var headerRole = context.HttpContext.Request.Headers["x-user-role"].ToString();

        if (!string.IsNullOrWhiteSpace(headerRole) && !string.IsNullOrWhiteSpace(jwtRole))
        {
            if (!string.Equals(headerRole, jwtRole, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ForbidResult(LogMessageDefaults.Messages[LogMessageKeys.Auth_RoleMismatch]);
                return;
            }
        }

        await next();
    }
}

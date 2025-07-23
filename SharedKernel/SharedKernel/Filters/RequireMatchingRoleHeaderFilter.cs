namespace SharedKernel.Filters;

using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


public class RequireMatchingRoleHeaderFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var jwtRole = user.FindFirst(ClaimTypes.Role)?.Value;
        var headerRole = context.HttpContext.Request.Headers["x-user-role"].ToString();

        if (string.IsNullOrWhiteSpace(jwtRole) || string.IsNullOrWhiteSpace(headerRole))
        {
            context.Result = new ForbidResult("Missing role information in JWT or header.");
            return;
        }

        if (!string.Equals(headerRole, jwtRole, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new ForbidResult("Role mismatch between JWT and x-user-role header.");
            return;
        }

        await next();
    }
}

namespace LogService.API.Filters;

using System.Security.Claims;

using Microsoft.AspNetCore.Mvc.Filters;

public class RequireMatchingRoleHeaderFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var jwtRole = user.FindFirst(ClaimTypes.Role)?.Value;
        var headerRole = context.HttpContext.Request.Headers["x-user-role"].ToString();

        if (!string.IsNullOrWhiteSpace(headerRole) && !string.IsNullOrWhiteSpace(jwtRole))
        {
            if (!string.Equals(headerRole, jwtRole, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        await next();
    }
}

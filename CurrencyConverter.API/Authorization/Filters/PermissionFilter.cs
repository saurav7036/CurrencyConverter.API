using CurrencyConverter.API.Authorization.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace CurrencyConverter.API.Authorization.Filters
{
    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated ?? false)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionClaim = user.FindFirst("permissions");
            if (permissionClaim == null || string.IsNullOrWhiteSpace(permissionClaim.Value))
            {
                context.Result = new ForbidResult();
                return;
            }

            var permissions = JsonSerializer.Deserialize<Dictionary<string, bool>>(permissionClaim.Value);
            if (permissions == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var requiredAttr = context.ActionDescriptor.EndpointMetadata
                .OfType<PermissionRequirementAttribute>()
                .FirstOrDefault();

            if (requiredAttr == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var key = requiredAttr.PermissionKey;

            if (!permissions.TryGetValue(key, out var isAllowed) || !isAllowed)
            {
                context.Result = new ForbidResult();
                return;
            }

            await Task.CompletedTask;
            return;
        }
    }
}

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace DotNetAdmin.Core.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AccessFilterAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        // ── Not logged in ─────────────────────────────────────────────────────
        if (!(user.Identity?.IsAuthenticated ?? false))
        {
            if (httpContext.Request.Path.StartsWithSegments("/api"))
                context.Result = new JsonResult(new { status = false, message = "Unauthorized" }) { StatusCode = 401 };
            else
                context.Result = new RedirectResult("/auth/login");
            return;
        }

        // ── Derive named route from endpoint metadata ──────────────────────────
        var endpoint = httpContext.GetEndpoint();
        var routeName = endpoint?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
        var method = httpContext.Request.Method;

        if (routeName == null)
        {
            await next();
            return;
        }

        // ── RBAC check ────────────────────────────────────────────────────────
        var db = httpContext.RequestServices.GetRequiredService<AppDbContext>();
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            context.Result = new RedirectResult("/auth/login");
            return;
        }

        var userWithRoles = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (userWithRoles == null)
        {
            context.Result = new RedirectResult("/auth/login");
            return;
        }

        var roles = userWithRoles.UserRoles.Select(ur => ur.Role).ToList();

        // Administrator bypasses all RBAC checks
        if (roles.Any(r => r.Name == "Administrator"))
        {
            await next();
            return;
        }

        var roleIds = roles.Select(r => r.Id).ToList();
        var hasAccess = await db.RolePermissions
            .Include(rp => rp.Permission)
            .AnyAsync(rp => roleIds.Contains(rp.RoleId) &&
                            rp.Permission.Name == routeName &&
                            rp.Permission.Method == method.ToUpper());

        if (!hasAccess)
        {
            if (httpContext.Request.Path.StartsWithSegments("/api"))
            {
                context.Result = new JsonResult(new { status = false, message = "Forbidden" }) { StatusCode = 403 };
            }
            else
            {
                // Flash 'Unauthorized.' and redirect to referrer
                httpContext.Session.SetString("flash_key", "error");
                httpContext.Session.SetString("flash_message", "Unauthorized.");
                var referrer = httpContext.Request.Headers["Referer"].ToString();
                var redirect = string.IsNullOrWhiteSpace(referrer) ? "/admin/v1/dashboard" : referrer;
                context.Result = new RedirectResult(redirect);
            }
            return;
        }

        await next();
    }
}

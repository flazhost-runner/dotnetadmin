using Microsoft.AspNetCore.Mvc.Filters;

namespace DotNetAdmin.Core.Filters;

public class AdminViewDataFilter : IAsyncActionFilter
{
    private readonly ISettingCacheService _settingCache;
    private readonly AppDbContext _db;

    public AdminViewDataFilter(ISettingCacheService settingCache, AppDbContext db)
    {
        _settingCache = settingCache;
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Controller controller)
        {
            // ── Theme + Setting ───────────────────────────────────────────────────
            var setting = await _settingCache.GetSettingAsync();
            var theme = ThemeConfig.GetTheme(setting?.Theme ?? "Blue");

            controller.ViewBag.ThemePrimary = theme.Primary;
            controller.ViewBag.ThemeSecondary = theme.Secondary;
            controller.ViewBag.ThemeLight = theme.Light;
            controller.ViewBag.ThemeDark = theme.Dark;
            controller.ViewBag.ThemeName = theme.Name;
            controller.ViewBag.Themes = ThemeConfig.Themes;
            controller.ViewBag.Setting = setting;
            controller.ViewBag.SettingName = setting?.Name ?? "DotNetAdmin";
            controller.ViewBag.SettingLogo = setting?.Logo;
            controller.ViewBag.SettingCopyright = setting?.Copyright;
            controller.ViewBag.AppName = setting?.Name ?? "DotNetAdmin";

            // ── Auth user ─────────────────────────────────────────────────────────
            var userId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var user = await _db.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    controller.ViewBag.AuthName = user.Name;
                    controller.ViewBag.AuthPicture = user.Picture; // always set (null → JS fallback)
                    controller.ViewBag.AuthUser = user;

                    var roles = user.UserRoles.Select(ur => ur.Role).ToList();
                    var isAdmin = roles.Any(r => r.Name == "Administrator");

                    // HasAccessFn: Func<routeName, method, bool> — used in sidebar gating
                    controller.ViewBag.HasAccessFn = new Func<string, string, bool>((routeName, method) =>
                    {
                        if (isAdmin) return true;
                        var roleIds = roles.Select(r => r.Id).ToList();
                        return _db.RolePermissions
                            .Include(rp => rp.Permission)
                            .Any(rp => roleIds.Contains(rp.RoleId) &&
                                       rp.Permission.Name == routeName &&
                                       rp.Permission.Method == method.ToUpper());
                    });

                    // HasRoleFn: Func<roleName, bool> — used in views to gate UI by role name
                    controller.ViewBag.HasRoleFn = new Func<string, bool>(roleName =>
                        roles.Any(r => r.Name == roleName));
                }
            }

            // ── Flash (one-shot from session) ─────────────────────────────────────
            var flashKey = context.HttpContext.Session.GetString("flash_key");
            var flashMsg = context.HttpContext.Session.GetString("flash_message");
            if (flashKey != null)
            {
                controller.ViewBag.FlashKey = flashKey;
                controller.ViewBag.FlashMessage = flashMsg;
                context.HttpContext.Session.Remove("flash_key");
                context.HttpContext.Session.Remove("flash_message");
            }
        }

        await next();
    }
}

using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Filters;
using DotNetAdmin.Core.Services;

namespace DotNetAdmin.Modules.Dashboard;

[Route("admin/v1/dashboard")]
[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;
    private readonly ISettingCacheService _settingCache;

    public DashboardController(AppDbContext db, ISettingCacheService settingCache)
    {
        _db = db;
        _settingCache = settingCache;
    }

    [HttpGet("", Name = "admin.v1.dashboard.index")]
    public async Task<IActionResult> Index()
    {
        var setting = await _settingCache.GetSettingAsync();
        ViewBag.Title = "Dashboard";
        ViewBag.UserCount = await _db.Users.CountAsync();
        ViewBag.RoleCount = await _db.Roles.CountAsync();
        ViewBag.PermissionCount = await _db.Permissions.CountAsync();
        ViewBag.ActiveTheme = setting?.Theme ?? "Blue";
        return View();
    }
}

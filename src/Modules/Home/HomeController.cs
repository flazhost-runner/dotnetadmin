using DotNetAdmin.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAdmin.Modules.Home;

[Route("")]
public class HomeController : Controller
{
    private readonly ISettingCacheService _settingCache;

    public HomeController(ISettingCacheService settingCache)
    {
        _settingCache = settingCache;
    }

    [HttpGet("", Name = "web.home.root")]
    public Task<IActionResult> Root() => Index();

    [HttpGet("home", Name = "web.home.index")]
    public async Task<IActionResult> Index()
    {
        var setting = await _settingCache.GetSettingAsync();
        ViewBag.Setting = setting;
        ViewBag.Title = setting?.Name ?? "Home";
        return View("Landing");
    }
}

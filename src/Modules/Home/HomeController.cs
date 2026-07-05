using DotNetAdmin.Core.Services;
using DotNetAdmin.Modules.Setting;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAdmin.Modules.Home;

/// <summary>
/// Halaman home (frontend publik).
/// - Slug aktif 'default' → dirender via view Razor lokal (fe/default, landing
///   v6 — head/header/footer terpisah + aset di wwwroot/fe/default).
/// - Slug lain (hasil switch/download) → HTML mentah self-contained yang
///   ter-cache di storage/fe/templates. Bila HTML tak tersedia (offline, gagal
///   unduh) → fallback ke landing default lokal agar halaman selalu tampil.
/// </summary>
[Route("")]
public class HomeController : Controller
{
    private readonly ISettingCacheService _settingCache;
    private readonly IFeTemplateService _feTemplate;

    public HomeController(ISettingCacheService settingCache, IFeTemplateService feTemplate)
    {
        _settingCache = settingCache;
        _feTemplate = feTemplate;
    }

    [HttpGet("", Name = "web.home.root")]
    public Task<IActionResult> Root() => Index();

    [HttpGet("home", Name = "web.home.index")]
    public async Task<IActionResult> Index()
    {
        var setting = await _settingCache.GetSettingAsync();
        var slug = _feTemplate.GetActiveSlug(setting);

        if (!_feTemplate.IsDefaultView(slug))
        {
            var html = await _feTemplate.GetActiveHtmlAsync(setting);
            if (html != null) return Content(html, "text/html; charset=utf-8");
        }

        ViewBag.Setting = setting;
        ViewBag.Title = setting?.Name ?? "Home";
        return View("Landing");
    }
}

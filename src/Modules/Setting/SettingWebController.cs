using DotNetAdmin.Core.Errors;
using DotNetAdmin.Core.Filters;

namespace DotNetAdmin.Modules.Setting;

[Route("admin/v1/setting")]
[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class SettingWebController : Controller
{
    private readonly ISettingService _settingService;
    private readonly IFeCatalogService _catalogService;

    public SettingWebController(ISettingService settingService, IFeCatalogService catalogService)
    {
        _settingService = settingService;
        _catalogService = catalogService;
    }

    [HttpGet("", Name = "admin.v1.setting.index")]
    public async Task<IActionResult> Index([FromQuery] string? fe_search, [FromQuery] string? fe_category, [FromQuery] int fe_page = 1)
    {
        var setting = await _settingService.GetAsync();
        var catalog = await _catalogService.GetCatalogAsync(fe_search, fe_category, fe_page, 12, setting.FeTemplate);
        var categories = await _catalogService.GetCategoriesAsync();

        ViewBag.Title = "Setting";
        ViewBag.CatalogResult = catalog;
        ViewBag.Categories = categories;
        ViewBag.FeSearch = fe_search;
        ViewBag.FeCategory = fe_category;
        return View(setting);
    }

    [HttpPut("update", Name = "admin.v1.setting.update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] SettingUpdateDto dto)
    {
        try
        {
            await _settingService.UpdateAsync(dto);
            HttpContext.Session.SetSuccess("Save Setting Success.");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
        }
        return RedirectToRoute("admin.v1.setting.index");
    }

    [HttpGet("fe-preview/{slug}", Name = "admin.v1.setting.fe_preview")]
    public async Task<IActionResult> FePreview(string slug)
    {
        var html = await _catalogService.GetPreviewHtmlAsync(slug);
        if (html == null) return NotFound();
        return Content(html, "text/html");
    }
}

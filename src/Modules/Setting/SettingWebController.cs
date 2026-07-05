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
    public async Task<IActionResult> Index(
        [FromQuery] string? q_name, [FromQuery] string? q_category,
        [FromQuery] int q_page = 1, [FromQuery] int q_page_size = 12)
    {
        var setting = await _settingService.GetAsync();
        // Template aktif disematkan (pin) ke halaman 1 agar admin langsung
        // melihat pilihan saat ini (paritas NodeAdmin SettingController.index).
        var catalog = await _catalogService.GetCatalogAsync(q_name, q_category, q_page, q_page_size, setting.FeTemplate);
        var categories = await _catalogService.GetCategoriesAsync();

        ViewBag.Title = "Setting";
        ViewBag.CatalogResult = catalog;
        ViewBag.Categories = categories;
        ViewBag.QName = q_name;
        ViewBag.QCategory = q_category;
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
        try
        {
            var html = await _catalogService.GetPreviewHtmlAsync(slug);
            return Content(html, "text/html; charset=utf-8");
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, ex.Message);
        }
    }
}

using DotNetAdmin.Core.Filters;

namespace DotNetAdmin.Modules.Components;

[Route("admin/v1/components")]
[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class ComponentsController : Controller
{
    [HttpGet("", Name = "admin.v1.components.index")]
    public IActionResult Index()
    {
        ViewBag.Title = "UI Components";
        return View();
    }
}

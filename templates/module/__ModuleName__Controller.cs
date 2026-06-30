using DotNetAdmin.Core.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetAdmin.Modules.__ModuleName__;

[Route("admin/v1/__moduleName__")]
[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class __ModuleName__Controller : Controller
{
    private readonly I__ModuleName__Service _service;

    public __ModuleName__Controller(I__ModuleName__Service service)
    {
        _service = service;
    }

    [HttpGet("", Name = "admin.v1.__moduleName__.index")]
    public async Task<IActionResult> Index()
    {
        var items = await _service.GetAllAsync();
        ViewBag.Title = "__ModuleName__";
        return View(items);
    }
}

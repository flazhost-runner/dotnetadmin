using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Access.Permission.Dtos;

namespace DotNetAdmin.Modules.Access.Permission;

[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class PermissionWebController : Controller
{
    private readonly IPermissionService _permissionService;
    private readonly IPermissionSyncService _permissionSync;

    public PermissionWebController(IPermissionService permissionService, IPermissionSyncService permissionSync)
    {
        _permissionService = permissionService;
        _permissionSync = permissionSync;
    }

    [HttpGet("/admin/v1/access/permission", Name = "admin.v1.access.permission.index")]
    public async Task<IActionResult> Index([FromQuery] PermissionFilterDto filter)
    {
        // Auto-discover: upsert permission dari route ter-registrasi (idempoten)
        // — paritas NodeAdmin PermissionController.index → getAllRegisteredRoute.
        await _permissionSync.SyncAsync();
        var result = await _permissionService.GetAllAsync(filter);
        ViewBag.Title = "Permission Management";
        ViewBag.Result = result;
        ViewBag.Filter = filter;
        return View("~/Views/AccessPermission/Index.cshtml");
    }

    [HttpGet("/admin/v1/access/permission/create", Name = "admin.v1.access.permission.create")]
    public IActionResult Create()
    {
        var (errors, old) = HttpContext.Session.GetFieldErrors();
        ViewBag.Title = "Create Permission";
        ViewBag.FieldErrors = errors;
        ViewBag.OldInput = old;
        return View("~/Views/AccessPermission/Create.cshtml");
    }

    [HttpPost("/admin/v1/access/permission", Name = "admin.v1.access.permission.store")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Store([FromForm] PermissionFormDto dto)
    {
        try
        {
            await _permissionService.CreateAsync(dto);
            HttpContext.Session.SetSuccess("Create Permission Success.");
            return RedirectToRoute("admin.v1.access.permission.index");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetFieldErrors(
                ex.Errors ?? new Dictionary<string, string> { ["_"] = ex.Message },
                new Dictionary<string, string>
                {
                    ["name"] = dto.Name, ["guard_name"] = dto.GuardName,
                    ["method"] = dto.Method ?? "", ["desc"] = dto.Desc ?? "", ["status"] = dto.Status,
                });
            HttpContext.Session.SetError(ex.Message);
            return RedirectToRoute("admin.v1.access.permission.create");
        }
    }

    [HttpGet("/admin/v1/access/permission/{id}/edit", Name = "admin.v1.access.permission.edit")]
    public async Task<IActionResult> Edit(string id)
    {
        var perm = await _permissionService.GetByIdAsync(id);
        var (errors, old) = HttpContext.Session.GetFieldErrors();
        ViewBag.Title = "Edit Permission";
        ViewBag.Permission = perm;
        ViewBag.FieldErrors = errors;
        ViewBag.OldInput = old;
        return View("~/Views/AccessPermission/Edit.cshtml");
    }

    [HttpPut("/admin/v1/access/permission/{id}", Name = "admin.v1.access.permission.update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, [FromForm] PermissionFormDto dto)
    {
        try
        {
            await _permissionService.UpdateAsync(id, dto);
            HttpContext.Session.SetSuccess("Update Permission Success.");
            return RedirectToRoute("admin.v1.access.permission.index");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetFieldErrors(
                ex.Errors ?? new Dictionary<string, string> { ["_"] = ex.Message },
                new Dictionary<string, string>
                {
                    ["name"] = dto.Name, ["guard_name"] = dto.GuardName,
                    ["method"] = dto.Method ?? "", ["desc"] = dto.Desc ?? "", ["status"] = dto.Status,
                });
            HttpContext.Session.SetError(ex.Message);
            return RedirectToRoute("admin.v1.access.permission.edit", new { id });
        }
    }

    [HttpDelete("/admin/v1/access/permission/{id}", Name = "admin.v1.access.permission.delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _permissionService.DeleteAsync(id);
            HttpContext.Session.SetSuccess("Delete Permission Success.");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
        }
        return RedirectToRoute("admin.v1.access.permission.index");
    }

    [HttpDelete("/admin/v1/access/permission/delete_selected", Name = "admin.v1.access.permission.delete_selected")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected([FromForm] string ids)
    {
        try
        {
            var idList = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            await _permissionService.DeleteSelectedAsync(idList);
            HttpContext.Session.SetSuccess("Delete Permission Success.");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
        }
        return RedirectToRoute("admin.v1.access.permission.index");
    }
}

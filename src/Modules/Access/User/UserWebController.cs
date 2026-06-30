using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Access.User.Dtos;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetAdmin.Modules.Access.User;

[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class UserWebController : Controller
{
    private readonly IUserService _userService;

    public UserWebController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("/admin/v1/access/user", Name = "admin.v1.access.user.index")]
    public async Task<IActionResult> Index([FromQuery] UserFilterDto filter)
    {
        var result = await _userService.GetAllAsync(filter);
        ViewBag.Title = "User Management";
        ViewBag.Result = result;
        ViewBag.Filter = filter;
        return View("~/Views/AccessUser/Index.cshtml");
    }

    [HttpGet("/admin/v1/access/user/create", Name = "admin.v1.access.user.create")]
    public async Task<IActionResult> Create()
    {
        var (errors, old) = HttpContext.Session.GetFieldErrors();
        ViewBag.Title = "Create User";
        ViewBag.Roles = await _userService.GetAllRolesAsync();
        ViewBag.Timezones = GetTimezones();
        ViewBag.FieldErrors = errors;
        ViewBag.OldInput = old;
        return View("~/Views/AccessUser/Create.cshtml");
    }

    [HttpPost("/admin/v1/access/user", Name = "admin.v1.access.user.store")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Store([FromForm] UserCreateDto dto)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            await _userService.CreateAsync(dto, actorId);
            HttpContext.Session.SetSuccess("Create User Success.");
            return RedirectToRoute("admin.v1.access.user.index");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetFieldErrors(
                ex.Errors ?? new Dictionary<string, string> { ["_"] = ex.Message },
                BuildOld(dto));
            HttpContext.Session.SetError(ex.Message);
            return RedirectToRoute("admin.v1.access.user.create");
        }
    }

    [HttpGet("/admin/v1/access/user/{id}/edit", Name = "admin.v1.access.user.edit")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        var (errors, old) = HttpContext.Session.GetFieldErrors();
        ViewBag.Title = "Edit User";
        ViewBag.User = user;
        ViewBag.Roles = await _userService.GetAllRolesAsync();
        ViewBag.Timezones = GetTimezones();
        ViewBag.FieldErrors = errors;
        ViewBag.OldInput = old;
        return View("~/Views/AccessUser/Edit.cshtml");
    }

    [HttpPut("/admin/v1/access/user/{id}", Name = "admin.v1.access.user.update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string id, [FromForm] UserUpdateDto dto)
    {
        try
        {
            var actorId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            await _userService.UpdateAsync(id, dto, actorId);
            HttpContext.Session.SetSuccess("Update User Success.");
            return RedirectToRoute("admin.v1.access.user.index");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetFieldErrors(
                ex.Errors ?? new Dictionary<string, string> { ["_"] = ex.Message },
                BuildOldUpdate(dto));
            HttpContext.Session.SetError(ex.Message);
            return RedirectToRoute("admin.v1.access.user.edit", new { id });
        }
    }

    [HttpDelete("/admin/v1/access/user/{id}", Name = "admin.v1.access.user.delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _userService.DeleteAsync(id);
            HttpContext.Session.SetSuccess("Delete User Success.");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
        }
        return RedirectToRoute("admin.v1.access.user.index");
    }

    [HttpDelete("/admin/v1/access/user/delete_selected", Name = "admin.v1.access.user.delete_selected")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected([FromForm] string ids)
    {
        try
        {
            var idList = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            await _userService.DeleteSelectedAsync(idList);
            HttpContext.Session.SetSuccess("Delete User Success.");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
        }
        return RedirectToRoute("admin.v1.access.user.index");
    }

    private static List<string> GetTimezones() =>
        TimeZoneInfo.GetSystemTimeZones().Select(t => t.Id).OrderBy(t => t).ToList();

    private static Dictionary<string, string> BuildOld(UserCreateDto dto) => new()
    {
        ["code"] = dto.Code, ["name"] = dto.Name, ["phone"] = dto.Phone ?? "",
        ["email"] = dto.Email, ["timezone"] = dto.Timezone, ["status"] = dto.Status,
        ["blocked"] = dto.Blocked ? "1" : "0", ["blocked_reason"] = dto.blocked_reason ?? ""
    };

    private static Dictionary<string, string> BuildOldUpdate(UserUpdateDto dto) => new()
    {
        ["code"] = dto.Code, ["name"] = dto.Name, ["phone"] = dto.Phone ?? "",
        ["email"] = dto.Email, ["timezone"] = dto.Timezone, ["status"] = dto.Status,
        ["blocked"] = dto.Blocked ? "1" : "0", ["blocked_reason"] = dto.blocked_reason ?? ""
    };
}

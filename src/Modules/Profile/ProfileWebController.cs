using DotNetAdmin.Core.Errors;
using DotNetAdmin.Core.Filters;
using System.Security.Claims;

namespace DotNetAdmin.Modules.Profile;

[Route("admin/v1/profile")]
[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class ProfileWebController : Controller
{
    private readonly IProfileService _profileService;

    public ProfileWebController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("", Name = "admin.v1.profile.index")]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _profileService.GetAsync(userId);
        ViewBag.Title = "Profile";
        ViewBag.Timezones = TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToList();
        return View(user);
    }

    [HttpPut("update", Name = "admin.v1.profile.update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] ProfileUpdateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            await _profileService.UpdateAsync(userId, dto);
            HttpContext.Session.SetSuccess("Update Profile Success.");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
        }
        return RedirectToRoute("admin.v1.profile.index");
    }
}

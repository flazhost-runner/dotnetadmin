using System.Security.Claims;

namespace DotNetAdmin.Modules.Profile;

[ApiController]
[Authorize(AuthenticationSchemes = "JwtBearer")]
public class ProfileApiController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileApiController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("/api/v1/profile", Name = "api.v1.profile.index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _profileService.GetAsync(userId);
            return Ok(new { success = true, message = "OK", data = new {
                id       = user.Id,
                name     = user.Name,
                email    = user.Email,
                timezone = user.Timezone ?? "",
                picture  = user.Picture  ?? "",
                status   = user.Status   ?? "",
            }});
        }
        catch (Exception ex)
        {
            return NotFound(new { success = false, message = ex.Message, data = (object?)null });
        }
    }
}

namespace DotNetAdmin.Modules.Setting;

[ApiController]
[Authorize(AuthenticationSchemes = "JwtBearer")]
public class SettingApiController : ControllerBase
{
    private readonly ISettingService _settingService;

    public SettingApiController(ISettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet("/api/v1/setting", Name = "api.v1.setting.index")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var s = await _settingService.GetAsync();
            return Ok(new { success = true, message = "OK", data = new {
                id          = s.Id,
                name        = s.Name        ?? "",
                theme       = s.Theme       ?? "",
                fe_template = s.FeTemplate  ?? "",
            }});
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message, data = (object?)null });
        }
    }
}

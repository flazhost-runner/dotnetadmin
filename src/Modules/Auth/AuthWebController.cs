using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using DotNetAdmin.Core.Filters;
using DotNetAdmin.Core.Helpers;
using DotNetAdmin.Modules.Auth.Dtos;

namespace DotNetAdmin.Modules.Auth;

[Controller]
public class AuthWebController : Controller
{
    private readonly IAuthService _authService;
    private readonly ISettingCacheService _settingCache;

    public AuthWebController(IAuthService authService, ISettingCacheService settingCache)
    {
        _authService = authService;
        _settingCache = settingCache;
    }

    private async Task InjectAuthViewData()
    {
        var setting = await _settingCache.GetSettingAsync();
        var theme = ThemeConfig.GetTheme(setting?.Theme ?? "Blue");
        ViewBag.ThemePrimary = theme.Primary;
        ViewBag.ThemeSecondary = theme.Secondary;
        ViewBag.ThemeLight = theme.Light;
        ViewBag.ThemeDark = theme.Dark;
        ViewBag.SettingName = setting?.Name ?? "DotNetAdmin";
        ViewBag.SettingLogo = setting?.Logo;
        ViewBag.AppName = setting?.Name ?? "DotNetAdmin";

        // Carry flash from session
        var flashKey = HttpContext.Session.GetString("flash_key");
        var flashMsg = HttpContext.Session.GetString("flash_message");
        if (flashKey != null)
        {
            ViewBag.FlashKey = flashKey;
            ViewBag.FlashMessage = flashMsg;
            HttpContext.Session.Remove("flash_key");
            HttpContext.Session.Remove("flash_message");
        }
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [HttpGet("/auth/login", Name = "web.auth.login")]
    public async Task<IActionResult> LoginGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/admin/v1/dashboard");

        await InjectAuthViewData();
        ViewBag.Title = "Login";
        return View("Login");
    }

    [HttpPost("/auth/login", Name = "web.auth.login.post")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginPost([FromForm] LoginDto dto)
    {
        try
        {
            var user = await _authService.LoginAsync(dto.Email, dto.Password);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
            };
            foreach (var ur in user.UserRoles)
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));

            var identity = new ClaimsIdentity(claims, "WebCookie");
            var principal = new ClaimsPrincipal(identity);

            // "Keep me logged in" is UI-only — session TTL is always fixed
            var authProps = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(6)
            };

            await HttpContext.SignInAsync("WebCookie", principal, authProps);
            return Redirect("/admin/v1/dashboard");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
            ViewBag.OldEmail = dto.Email;
            await InjectAuthViewData();
            ViewBag.Title = "Login";
            return View("Login");
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [HttpPost("/auth/logout", Name = "web.auth.logout")]
    [ValidateAntiForgeryToken]
    [Authorize(AuthenticationSchemes = "WebCookie")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("WebCookie");
        return Redirect("/auth/login");
    }

    // ── Register ──────────────────────────────────────────────────────────────

    [HttpGet("/auth/register", Name = "web.auth.register")]
    public async Task<IActionResult> RegisterGet()
    {
        await InjectAuthViewData();
        ViewBag.Title = "Register";
        return View("Register");
    }

    [HttpPost("/auth/register", Name = "web.auth.register.post")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RegisterPost([FromForm] RegisterDto dto)
    {
        try
        {
            await _authService.RegisterAsync(dto);
            HttpContext.Session.SetSuccess("Register Success.");
            return Redirect("/auth/login");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
            await InjectAuthViewData();
            ViewBag.Title = "Register";
            ViewBag.OldName = dto.Name;
            ViewBag.OldCode = dto.Code;
            ViewBag.OldEmail = dto.Email;
            ViewBag.OldPhone = dto.Phone;
            return View("Register");
        }
    }

    // ── Reset OTP ─────────────────────────────────────────────────────────────

    [HttpGet("/admin/v1/auth/reset/req", Name = "admin.v1.auth.reset.req")]
    public async Task<IActionResult> ResetReqGet()
    {
        await InjectAuthViewData();
        ViewBag.Title = "Forgot Password";
        return View("ResetReq");
    }

    [HttpPost("/admin/v1/auth/reset/request", Name = "admin.v1.auth.reset.request")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetRequest([FromForm] ResetRequestDto dto)
    {
        try
        {
            await _authService.RequestOtpAsync(dto.Email);
            HttpContext.Session.SetSuccess("OTP Send Success.");
            return Redirect("/admin/v1/auth/reset/proc");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
            return Redirect("/admin/v1/auth/reset/req");
        }
    }

    [HttpGet("/admin/v1/auth/reset/proc", Name = "admin.v1.auth.reset.proc")]
    public async Task<IActionResult> ResetProcGet()
    {
        await InjectAuthViewData();
        ViewBag.Title = "Reset Password";
        ViewBag.OldOtp = HttpContext.Session.GetString("old_otp") ?? "";
        HttpContext.Session.Remove("old_otp");
        return View("ResetProc");
    }

    [HttpPost("/admin/v1/auth/reset/process", Name = "admin.v1.auth.reset.process")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> ResetProcess([FromForm] ResetProcessDto dto)
    {
        try
        {
            if (dto.Password != dto.PasswordConfirm)
                throw new ValidationAppException("Passwords do not match.");

            await _authService.ResetPasswordAsync(dto.Email, dto.Otp, dto.Password);
            HttpContext.Session.SetSuccess("Reset Password Success.");
            return Redirect("/auth/login");
        }
        catch (AppException ex)
        {
            HttpContext.Session.SetError(ex.Message);
            HttpContext.Session.SetString("old_otp", dto.Otp ?? "");
            return Redirect("/admin/v1/auth/reset/proc");
        }
    }
}

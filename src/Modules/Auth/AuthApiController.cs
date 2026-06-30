using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using DotNetAdmin.Modules.Auth.Dtos;

namespace DotNetAdmin.Modules.Auth;

[ApiController]
[Route("api/v1/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;

    public AuthApiController(IAuthService authService, IJwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("login", Name = "api.v1.auth.login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var user = await _authService.LoginAsync(dto.Email, dto.Password);
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var token = _jwtService.GenerateToken(user, roles);
            return Ok(new { success = true, message = "Login successful.", data = new { token, user = new { user.Id, user.Name, user.Email, user.Code } } });
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("logout", Name = "api.v1.auth.logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var principal = _jwtService.ValidateToken(token);
            if (principal != null)
            {
                var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
                if (long.TryParse(expClaim, out var exp))
                {
                    var ttl = DateTimeOffset.FromUnixTimeSeconds(exp) - DateTimeOffset.UtcNow;
                    if (ttl > TimeSpan.Zero)
                        await _jwtService.BlacklistTokenAsync(token, ttl);
                }
            }
        }
        return Ok(new { success = true, message = "Logged out." });
    }

    [HttpGet("me", Name = "api.v1.auth.me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        return Ok(new { success = true, data = new { id = userId, name, email } });
    }

    [HttpPost("register", Name = "api.v1.auth.register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var user = await _authService.RegisterAsync(dto);
            return Ok(new { success = true, message = "Registration successful.", data = new { user.Id, user.Name, user.Email } });
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, errors = ex.Errors });
        }
    }

    [HttpPost("reset/request", Name = "api.v1.auth.reset.request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetRequest([FromBody] ResetRequestDto dto)
    {
        try
        {
            await _authService.RequestOtpAsync(dto.Email);
            return Ok(new { success = true, message = "OTP sent to your email." });
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message });
        }
    }

    [HttpPost("reset/process", Name = "api.v1.auth.reset.process")]
    [EnableRateLimiting("otp")]
    public async Task<IActionResult> ResetProcess([FromBody] ResetProcessDto dto)
    {
        try
        {
            if (dto.Password != dto.PasswordConfirm)
                return BadRequest(new { success = false, message = "Passwords do not match." });

            await _authService.ResetPasswordAsync(dto.Email, dto.Otp, dto.Password);
            return Ok(new { success = true, message = "Password reset successful." });
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, message = ex.Message, errors = ex.Errors });
        }
    }
}

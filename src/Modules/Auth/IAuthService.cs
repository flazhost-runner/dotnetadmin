using DotNetAdmin.Modules.Auth.Dtos;

namespace DotNetAdmin.Modules.Auth;

public interface IAuthService
{
    Task<User> LoginAsync(string email, string password);
    Task<User> RegisterAsync(RegisterDto dto);
    Task<string> RequestOtpAsync(string email);
    Task ResetPasswordAsync(string email, string otp, string newPassword);
}

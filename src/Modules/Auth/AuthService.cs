using DotNetAdmin.Modules.Auth.Dtos;
using Microsoft.Extensions.Logging;

namespace DotNetAdmin.Modules.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly AppConfig _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IOptions<AppConfig> appConfig, ILogger<AuthService> logger)
    {
        _db = db;
        _config = appConfig.Value;
        _logger = logger;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            throw new UnauthorizedAppException("Wrong email or password.");

        if (user.Status != "Active")
            throw new UnauthorizedAppException("Account is inactive.");

        if (user.Blocked)
            throw new UnauthorizedAppException($"Account is blocked: {user.BlockedReason}");

        return user;
    }

    public async Task<User> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.PasswordConfirm)
            throw new ValidationAppException("Passwords do not match.", new() { ["password_confirm"] = "Passwords do not match." });

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new ConflictAppException("Email already exists.");

        if (await _db.Users.AnyAsync(u => u.Code == dto.Code))
            throw new ConflictAppException("Code already in use.");

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Code = dto.Code,
            Email = dto.Email,
            Phone = dto.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, _config.BcryptRounds),
            Status = "Active",
            CreatedBy = "self",
            UpdatedBy = "self"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<string> RequestOtpAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new NotFoundAppException("Email not found.");

        var otp = OtpHelper.GenerateOtp();
        user.PasswordOtp = OtpHelper.HashOtp(otp);
        user.PasswordOtpExpires = OtpHelper.OtpExpiresAt(_config.OtpExpiryMinutes);
        user.UpdatedBy = "system";
        await _db.SaveChangesAsync();

        _logger.LogWarning("OTP for {Email}: {Otp} (dev only — wire SMTP in production)", email, otp);

        return otp;
    }

    public async Task ResetPasswordAsync(string email, string otp, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new NotFoundAppException("Email not found.");

        if (string.IsNullOrEmpty(user.PasswordOtp) || !OtpHelper.VerifyOtp(otp, user.PasswordOtp))
            throw new ValidationAppException("OTP is invalid.", new() { ["otp"] = "OTP is invalid." });

        if (OtpHelper.IsOtpExpired(user.PasswordOtpExpires))
            throw new ValidationAppException("OTP has expired.", new() { ["otp"] = "OTP has expired." });

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword, _config.BcryptRounds);
        user.PasswordOtp = null;
        user.PasswordOtpExpires = null;
        user.UpdatedBy = "system";
        await _db.SaveChangesAsync();
    }
}

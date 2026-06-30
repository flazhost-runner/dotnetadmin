namespace DotNetAdmin.Core.Helpers;

public static class OtpHelper
{
    public static string GenerateOtp(int length = 6)
    {
        var bytes = RandomNumberGenerator.GetBytes(length * 4);
        var digits = string.Concat(bytes.Select(b => (b % 10).ToString()));
        return digits[..length];
    }

    public static string HashOtp(string otp) =>
        BCrypt.Net.BCrypt.HashPassword(otp);

    public static bool VerifyOtp(string otp, string hash) =>
        BCrypt.Net.BCrypt.Verify(otp, hash);

    public static long OtpExpiresAt(int minutes = 10) =>
        DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeMilliseconds();

    public static bool IsOtpExpired(long? expiresAt) =>
        !expiresAt.HasValue || DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() > expiresAt.Value;
}

namespace DotNetAdmin.Core.Auth;

public interface IJwtService
{
    string GenerateToken(User user, IList<string> roles);
    ClaimsPrincipal? ValidateToken(string token);
    Task BlacklistTokenAsync(string token, TimeSpan ttl);
    Task<bool> IsTokenBlacklistedAsync(string token);
}

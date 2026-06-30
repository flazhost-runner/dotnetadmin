using Microsoft.Extensions.Caching.Distributed;

namespace DotNetAdmin.Core.Auth;

public class JwtService(IOptions<AppConfig> appConfig, IDistributedCache cache) : IJwtService
{
    private readonly AppConfig _config = appConfig.Value;

    public string GenerateToken(User user, IList<string> roles)
    {
        var secret = _config.JwtSecret;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JwtSecret is not configured.");

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);  // pin HS256

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("name", user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.Add(ParseExpiresIn(_config.JwtExpiresIn)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var secret = _config.JwtSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return null;

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256]  // pin HS256
            }, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public async Task BlacklistTokenAsync(string token, TimeSpan ttl)
    {
        var key = $"blacklist:{token}";
        await cache.SetStringAsync(key, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        var key = $"blacklist:{token}";
        return await cache.GetStringAsync(key) is not null;
    }

    // Parse JWT_EXPIRES_IN string: '1h', '30m', '7d', '3600s'
    private static TimeSpan ParseExpiresIn(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return TimeSpan.FromHours(1);
        var lower = value.Trim().ToLowerInvariant();
        if (lower.EndsWith('h') && int.TryParse(lower[..^1], out var h)) return TimeSpan.FromHours(h);
        if (lower.EndsWith('m') && int.TryParse(lower[..^1], out var m)) return TimeSpan.FromMinutes(m);
        if (lower.EndsWith('d') && int.TryParse(lower[..^1], out var d)) return TimeSpan.FromDays(d);
        if (lower.EndsWith('s') && int.TryParse(lower[..^1], out var s)) return TimeSpan.FromSeconds(s);
        return TimeSpan.FromHours(1);
    }
}

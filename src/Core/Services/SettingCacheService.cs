namespace DotNetAdmin.Core.Services;

public interface ISettingCacheService
{
    Task<Setting?> GetSettingAsync();
    void InvalidateCache();
}

public class SettingCacheService : ISettingCacheService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Setting? _cached;
    private DateTime _cachedAt;
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(60);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SettingCacheService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<Setting?> GetSettingAsync()
    {
        if (_cached != null && DateTime.UtcNow - _cachedAt < _ttl)
            return _cached;

        await _lock.WaitAsync();
        try
        {
            if (_cached != null && DateTime.UtcNow - _cachedAt < _ttl)
                return _cached;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _cached = await db.Settings.FirstOrDefaultAsync();
            _cachedAt = DateTime.UtcNow;
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void InvalidateCache()
    {
        _cached = null;
    }
}

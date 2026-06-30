using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Services;
using Ganss.Xss;

namespace DotNetAdmin.Modules.Setting;

public class SettingService : ISettingService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ISettingCacheService _cache;
    private readonly IFeTemplateService _feTemplate;

    public SettingService(AppDbContext db, IWebHostEnvironment env, ISettingCacheService cache, IFeTemplateService feTemplate)
    {
        _db = db;
        _env = env;
        _cache = cache;
        _feTemplate = feTemplate;
    }

    public async Task<Core.Data.Entities.Setting> GetAsync()
    {
        var setting = await _db.Settings.FirstOrDefaultAsync();
        if (setting == null)
        {
            setting = new Core.Data.Entities.Setting { Name = "DotNetAdmin", Theme = "Blue" };
            _db.Settings.Add(setting);
            await _db.SaveChangesAsync();
        }
        return setting;
    }

    public async Task<Core.Data.Entities.Setting> UpdateAsync(SettingUpdateDto dto)
    {
        var setting = await GetAsync();

        if (dto.Initial != null) setting.Initial = dto.Initial;
        if (dto.Name != null) setting.Name = dto.Name;
        if (dto.Phone != null) setting.Phone = dto.Phone;
        if (dto.Address != null) setting.Address = dto.Address;
        if (dto.Email != null) setting.Email = dto.Email;
        if (dto.Copyright != null) setting.Copyright = dto.Copyright;
        if (!string.IsNullOrEmpty(dto.Theme)) setting.Theme = dto.Theme;
        if (!string.IsNullOrEmpty(dto.fe_template))
        {
            var oldTemplate = setting.FeTemplate;
            setting.FeTemplate = dto.fe_template;
            if (oldTemplate != dto.fe_template)
                _ = _feTemplate.EnsureAsync(dto.fe_template); // fire-and-forget cache warmup
        }

        if (dto.Description != null)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.UnionWith(["p", "br", "b", "i", "em", "strong", "a", "ul", "ol", "li", "h1", "h2", "h3", "h4", "h5", "h6", "img"]);
            setting.Description = sanitizer.Sanitize(dto.Description);
        }

        await SaveFileAsync(dto.icon, "setting", n => setting.Icon = n);
        await SaveFileAsync(dto.logo, "setting", n => setting.Logo = n);
        await SaveFileAsync(dto.favicon, "setting", n => setting.Favicon = n);
        await SaveFileAsync(dto.login_image, "setting", n => setting.LoginImage = n);

        await _db.SaveChangesAsync();
        _cache.InvalidateCache();
        return setting;
    }

    private async Task SaveFileAsync(IFormFile? file, string subdir, Action<string> setUrl)
    {
        if (file == null || file.Length == 0) return;
        var dir = Path.Combine(_env.WebRootPath, "media", subdir);
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(file.FileName).ToLower();
        var name = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(dir, name);
        await using var stream = File.Create(path);
        await file.CopyToAsync(stream);
        setUrl($"/media/{subdir}/{name}");
    }
}

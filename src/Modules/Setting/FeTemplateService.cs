namespace DotNetAdmin.Modules.Setting;

/// <summary>Kontrak FeTemplateService (template frontend / landing switcher).</summary>
public interface IFeTemplateService
{
    /// <summary>Apakah file template sudah ada di cache lokal.</summary>
    bool IsCached(string slug);

    /// <summary>Slug valid: 'default' (view lokal) atau cocok pola opentailwind.</summary>
    bool IsValidSlug(string? slug);

    /// <summary>Slug template aktif dari setting (fallback default).</summary>
    string GetActiveSlug(Core.Data.Entities.Setting? setting);

    /// <summary>True bila slug = 'default' → dirender via view Razor lokal (landing v6), bukan raw HTML.</summary>
    bool IsDefaultView(string slug);

    /// <summary>Pastikan file template tersedia lokal — download dari opentailwind bila perlu.</summary>
    Task EnsureAsync(string slug);

    /// <summary>
    /// HTML landing aktif (raw). Null bila template aktif = view 'default' lokal
    /// atau tak ada HTML yang bisa disajikan (offline & belum ter-cache) —
    /// pemanggil lalu merender landing fe/default (v6) sebagai fallback aman.
    /// </summary>
    Task<string?> GetActiveHtmlAsync(Core.Data.Entities.Setting? setting);
}

/// <summary>
/// Resolusi template frontend (landing) aktif + download/cache on-demand.
/// Mirror dari NodeAdmin src/modules/home/http/services/v1/FeTemplateService.ts
/// dengan semantik port (DjangoAdmin/LaravelAdmin): slug 'default' → view lokal,
/// slug opentailwind → raw HTML ter-cache di storage/fe/templates.
/// </summary>
public class FeTemplateService : IFeTemplateService
{
    /// <summary>Timeout download 1 file HTML template.</summary>
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(15);

    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;

    public FeTemplateService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory)
    {
        _env = env;
        _httpClientFactory = httpClientFactory;
    }

    private string Dir() => Path.Combine(_env.ContentRootPath, FeTemplates.Dir);

    private string FilePath(string slug) => Path.Combine(Dir(), slug + ".html");

    public bool IsCached(string slug) => File.Exists(FilePath(slug));

    public bool IsValidSlug(string? slug) => FeTemplates.IsValidSlug(slug);

    public string GetActiveSlug(Core.Data.Entities.Setting? setting)
    {
        var slug = setting?.FeTemplate?.Trim() ?? "";
        return IsValidSlug(slug) ? slug : FeTemplates.Default;
    }

    public bool IsDefaultView(string slug) => slug == FeTemplates.DefaultView;

    /// <summary>
    /// Pastikan template tersedia lokal. Bila belum → download HTML dari
    /// opentailwind (GitHub raw) lalu simpan ke folder cache. Hanya slug yang
    /// cocok pola opentailwind yang diizinkan (anti SSRF/arbitrary fetch).
    /// </summary>
    public async Task EnsureAsync(string slug)
    {
        if (!IsValidSlug(slug))
        {
            throw new AppException("Template tidak dikenali", 400);
        }
        if (IsDefaultView(slug) || IsCached(slug)) return;

        var url = $"{FeTemplates.BaseUrl}/{slug}.html";
        string html;
        try
        {
            var client = _httpClientFactory.CreateClient("github");
            using var cts = new CancellationTokenSource(FetchTimeout);
            using var res = await client.GetAsync(url, cts.Token);
            if (!res.IsSuccessStatusCode) throw new Exception($"HTTP {(int)res.StatusCode}");
            html = await res.Content.ReadAsStringAsync(cts.Token);
        }
        catch (Exception e)
        {
            var reason = e is OperationCanceledException ? "timeout" : e.Message;
            throw new AppException($"Gagal mengunduh template: {reason}", 502);
        }

        if (!html.Contains("</html>", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("Template terunduh tidak valid", 502);
        }

        Directory.CreateDirectory(Dir());
        await File.WriteAllTextAsync(FilePath(slug), html);
    }

    public async Task<string?> GetActiveHtmlAsync(Core.Data.Entities.Setting? setting)
    {
        var slug = GetActiveSlug(setting);
        if (IsDefaultView(slug)) return null;

        // Download on-demand (best-effort) — instalasi baru langsung menyajikan
        // template default opentailwind begitu jaringan memungkinkan.
        if (!IsCached(slug))
        {
            try
            {
                await EnsureAsync(slug);
            }
            catch (AppException)
            {
                // offline / gagal unduh → jatuh ke fallback di bawah
            }
        }

        var target = IsCached(slug) ? slug : FeTemplates.Default;
        var file = FilePath(target);
        if (!File.Exists(file)) return null;
        return await File.ReadAllTextAsync(file);
    }
}

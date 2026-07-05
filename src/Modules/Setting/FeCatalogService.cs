using System.Text.Json;

namespace DotNetAdmin.Modules.Setting;

/// <summary>
/// Katalog template frontend (640 landing opentailwind). Sumber kebenaran =
/// GitHub tree API, di-fetch SEKALI lalu di-cache (memori + file disk) agar tak
/// membebani server/GitHub. Pencarian & paginasi diproses server-side di sini.
/// Mirror dari NodeAdmin src/modules/home/http/services/v1/FeCatalogService.ts.
/// </summary>
public class FeCatalogService : IFeCatalogService
{
    /// <summary>TTL cache memori katalog. Disk dipakai sebagai persist lintas-restart.</summary>
    private static readonly TimeSpan CatalogTtl = TimeSpan.FromHours(6);

    /// <summary>Timeout fetch preview 1 file HTML — cukup ketat, file tunggal & ringan.</summary>
    private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Timeout fetch tree katalog — lebih longgar dari preview: respons tree
    /// recursive mencakup 640 entry & hanya dijalankan SEKALI lalu di-cache.
    /// </summary>
    private static readonly TimeSpan TreeFetchTimeout = TimeSpan.FromSeconds(20);

    private static List<FeTemplateItem>? _memo;
    private static DateTime _memoAt;
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FeCatalogService> _logger;

    public FeCatalogService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory, ILogger<FeCatalogService> logger)
    {
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private string CacheFile() => Path.Combine(_env.ContentRootPath, FeTemplates.CatalogFile);

    /// <summary>Path HTML template yang sudah ter-download lokal (fallback preview offline).</summary>
    private string LocalHtmlFile(string slug) =>
        Path.Combine(_env.ContentRootPath, FeTemplates.Dir, slug + ".html");

    /// <summary>Parse path tree → item landing (buang prefix `landings/` & `.html`).</summary>
    private static List<FeTemplateItem> ParseTree(JsonElement tree)
    {
        var items = new List<FeTemplateItem>();
        foreach (var node in tree.EnumerateArray())
        {
            var type = node.TryGetProperty("type", out var t) ? t.GetString() : null;
            var path = node.TryGetProperty("path", out var p) ? p.GetString() : null;
            if (type != "blob" || path == null) continue;
            if (!path.StartsWith("landings/") || !path.EndsWith(".html")) continue;
            var slug = path["landings/".Length..^".html".Length];
            items.Add(FeTemplates.Derive(slug));
        }
        // Urut stabil: kategori lalu nama.
        return [.. items
            .OrderBy(i => i.Category, StringComparer.Ordinal)
            .ThenBy(i => i.Name, StringComparer.Ordinal)];
    }

    private List<FeTemplateItem>? ReadDiskCache()
    {
        try
        {
            var raw = File.ReadAllText(CacheFile());
            var data = JsonSerializer.Deserialize<List<FeTemplateItem>>(raw);
            return data is { Count: > 0 } ? data : null;
        }
        catch
        {
            return null;
        }
    }

    private void WriteDiskCache(List<FeTemplateItem> data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFile())!);
            File.WriteAllText(CacheFile(), JsonSerializer.Serialize(data));
        }
        catch
        {
            // Cache disk best-effort — kegagalan tulis tak menggagalkan ListAsync().
        }
    }

    public async Task<List<FeTemplateItem>> ListAsync()
    {
        if (_memo != null && DateTime.UtcNow - _memoAt < CatalogTtl) return _memo;

        await Gate.WaitAsync();
        try
        {
            if (_memo != null && DateTime.UtcNow - _memoAt < CatalogTtl) return _memo;

            var disk = ReadDiskCache();
            if (disk != null)
            {
                _memo = disk;
                _memoAt = DateTime.UtcNow;
                return disk;
            }

            // Belum ada cache → fetch GitHub tree sekali.
            try
            {
                var client = _httpClientFactory.CreateClient("github");
                using var cts = new CancellationTokenSource(TreeFetchTimeout);
                using var req = new HttpRequestMessage(HttpMethod.Get, FeTemplates.TreeUrl);
                req.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
                using var res = await client.SendAsync(req, cts.Token);
                if (!res.IsSuccessStatusCode) throw new Exception($"HTTP {(int)res.StatusCode}");
                var body = await res.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(body);
                var data = doc.RootElement.TryGetProperty("tree", out var tree)
                    ? ParseTree(tree)
                    : [];
                if (data.Count == 0) throw new Exception("katalog kosong");
                _memo = data;
                _memoAt = DateTime.UtcNow;
                WriteDiskCache(data);
                return data;
            }
            catch (Exception e)
            {
                // Fallback ke katalog kurasi agar UI tetap berfungsi offline.
                _logger.LogError(e, "Fetch katalog opentailwind gagal, pakai fallback kurasi");
                var fallback = FeTemplates.Curated.ToList();
                _memo = fallback;
                _memoAt = DateTime.UtcNow;
                return fallback;
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var all = await ListAsync();
        return [.. all.Select(t => t.Category).Distinct().OrderBy(c => c, StringComparer.Ordinal)];
    }

    public async Task<PaginationResult<FeTemplateItem>> GetCatalogAsync(
        string? qName = null, string? qCategory = null, int page = 1, int pageSize = 12, string? pinSlug = null)
    {
        var all = await ListAsync();
        var name = (qName ?? "").Trim();
        var cat = (qCategory ?? "").Trim();

        var filtered = all.Where(t =>
            (name.Length == 0
                || t.Name.Contains(name, StringComparison.OrdinalIgnoreCase)
                || t.Slug.Contains(name, StringComparison.OrdinalIgnoreCase))
            && (cat.Length == 0 || t.Category == cat)).ToList();

        // Sematkan template aktif ke paling depan (bila lolos filter) agar tampil
        // di halaman pertama — memudahkan admin melihat pilihan saat ini.
        if (!string.IsNullOrEmpty(pinSlug))
        {
            var i = filtered.FindIndex(t => t.Slug == pinSlug);
            if (i > 0)
            {
                var pinned = filtered[i];
                filtered.RemoveAt(i);
                filtered.Insert(0, pinned);
            }
        }

        pageSize = pageSize > 0 ? pageSize : 12;
        page = page > 0 ? page : 1;
        var total = filtered.Count;
        var datas = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PaginationResult<FeTemplateItem>
        {
            Data = datas,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
        };
    }

    public async Task<bool> HasAsync(string slug)
    {
        var all = await ListAsync();
        return all.Any(t => t.Slug == slug);
    }

    /// <summary>Baca HTML template dari cache lokal bila ada & valid (fallback offline).</summary>
    private string? ReadLocalHtml(string slug)
    {
        try
        {
            var html = File.ReadAllText(LocalHtmlFile(slug));
            return html.Contains("</html>", StringComparison.OrdinalIgnoreCase) ? html : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GetPreviewHtmlAsync(string slug)
    {
        if (!await HasAsync(slug))
        {
            throw new AppException("Template tidak dikenali", 400);
        }

        // 1) Cache lokal lebih dulu — instan & tak bergantung jaringan/rate-limit.
        var local = ReadLocalHtml(slug);
        if (local != null) return local;

        // 2) Fetch upstream dengan timeout agar tak menggantung saat GitHub lambat.
        var url = $"{FeTemplates.BaseUrl}/{slug}.html";
        try
        {
            var client = _httpClientFactory.CreateClient("github");
            using var cts = new CancellationTokenSource(FetchTimeout);
            using var res = await client.GetAsync(url, cts.Token);
            if (!res.IsSuccessStatusCode) throw new Exception($"HTTP {(int)res.StatusCode}");
            var html = await res.Content.ReadAsStringAsync(cts.Token);
            if (!html.Contains("</html>", StringComparison.OrdinalIgnoreCase)) throw new Exception("HTML tidak valid");
            return html;
        }
        catch (Exception e)
        {
            // 3) Fallback terakhir: cache lokal (jika sempat ter-download sebagian).
            var fallback = ReadLocalHtml(slug);
            if (fallback != null) return fallback;
            var reason = e is OperationCanceledException ? "timeout" : e.Message;
            throw new AppException($"Gagal mengambil preview: {reason}", 502);
        }
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
using DotNetAdmin.Core.Helpers;

namespace DotNetAdmin.Modules.Setting;

public class FeCatalogService : IFeCatalogService
{
    private static List<FeCatalogItem>? _cache;
    private static DateTime _cacheTime;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromHours(6);
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FeCatalogService> _logger;

    private const string GitHubTreeUrl = "https://api.github.com/repos/lindoai/opentailwind/git/trees/main?recursive=1";
    private const string RawBaseUrl = "https://raw.githubusercontent.com/lindoai/opentailwind/main/";

    private static readonly Regex SlugPattern = new(@"^([a-z]+(?:-[a-z]+)*)-(\d{3})-([a-z0-9-]+)$", RegexOptions.Compiled);
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public FeCatalogService(IWebHostEnvironment env, IHttpClientFactory httpClientFactory, ILogger<FeCatalogService> logger)
    {
        _env = env;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PaginationResult<FeCatalogItem>> GetCatalogAsync(string? search = null, string? category = null, int page = 1, int pageSize = 12, string? pinSlug = null)
    {
        var catalog = await GetOrFetchCatalogAsync();
        var filtered = catalog.AsEnumerable();
        if (!string.IsNullOrEmpty(search))
            filtered = filtered.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || i.Slug.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(category))
            filtered = filtered.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();

        if (pinSlug != null && page == 1)
        {
            var pinned = list.FirstOrDefault(i => i.Slug == pinSlug);
            if (pinned != null) { list.Remove(pinned); list.Insert(0, pinned); }
        }

        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PaginationResult<FeCatalogItem>
        {
            Data = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    public async Task<string?> GetPreviewHtmlAsync(string slug)
    {
        if (!SlugPattern.IsMatch(slug)) return null;
        var catalog = await GetOrFetchCatalogAsync();
        if (!catalog.Any(i => i.Slug == slug)) return null;

        var cachePath = Path.Combine(_env.ContentRootPath, "public", "fe", "templates", slug + ".html");
        if (File.Exists(cachePath)) return await File.ReadAllTextAsync(cachePath);

        try
        {
            var client = _httpClientFactory.CreateClient("github");
            var html = await client.GetStringAsync(RawBaseUrl + slug + "/" + slug + ".html");
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            await File.WriteAllTextAsync(cachePath, html);
            return html;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch preview for {Slug}", slug);
            return null;
        }
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var catalog = await GetOrFetchCatalogAsync();
        return catalog.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();
    }

    private async Task<List<FeCatalogItem>> GetOrFetchCatalogAsync()
    {
        if (_cache != null && DateTime.UtcNow - _cacheTime < _cacheTtl) return _cache;

        await _lock.WaitAsync();
        try
        {
            if (_cache != null && DateTime.UtcNow - _cacheTime < _cacheTtl) return _cache;

            var diskCache = Path.Combine(_env.ContentRootPath, "_catalog.json");
            if (File.Exists(diskCache) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(diskCache)) < _cacheTtl)
            {
                var json = await File.ReadAllTextAsync(diskCache);
                _cache = JsonSerializer.Deserialize<List<FeCatalogItem>>(json) ?? FallbackCatalog();
                _cacheTime = File.GetLastWriteTimeUtc(diskCache);
                return _cache;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("github");
                using var response = await client.GetAsync(GitHubTreeUrl);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(body);
                    var tree = doc.RootElement.GetProperty("tree");
                    var items = new List<FeCatalogItem>();
                    foreach (var node in tree.EnumerateArray())
                    {
                        var path = node.GetProperty("path").GetString() ?? "";
                        var type = node.TryGetProperty("type", out var t) ? t.GetString() : "";
                        if (type == "tree")
                        {
                            var dirName = path.Contains('/') ? path.Split('/').Last() : path;
                            var m = SlugPattern.Match(dirName);
                            if (m.Success)
                            {
                                var slug = dirName;
                                var name = m.Groups[3].Value.Replace("-", " ");
                                var cat = m.Groups[1].Value;
                                items.Add(new FeCatalogItem(slug, name, cat, RawBaseUrl + path + "/" + slug + ".html"));
                            }
                        }
                    }
                    if (items.Count > 0)
                    {
                        _cache = items;
                        Directory.CreateDirectory(Path.GetDirectoryName(diskCache)!);
                        await File.WriteAllTextAsync(diskCache, JsonSerializer.Serialize(_cache));
                        _cacheTime = DateTime.UtcNow;
                        return _cache;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch GitHub catalog");
            }

            _cache ??= FallbackCatalog();
            _cacheTime = DateTime.UtcNow;
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static List<FeCatalogItem> FallbackCatalog() =>
    [
        new("agency-consulting-002-creative-agency", "creative agency", "agency-consulting", ""),
        new("ecommerce-001-shop", "shop", "ecommerce", ""),
        new("portfolio-001-minimal", "minimal", "portfolio", ""),
    ];
}

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using DotNetAdmin.Config;

namespace DotNetAdmin.Core.Storage;

/// <summary>
/// Driver <c>oss</c>: Alibaba Cloud OSS. Memakai skema tanda tangan OSS v1
/// (HMAC-SHA1). <see cref="Url"/> menghasilkan URL absolut ter-presign berlaku
/// <c>ttlSeconds</c>; tidak ada penyajian file lokal.
/// </summary>
public sealed class OssStorageService : IStorageService
{
    private readonly StorageConfig _cfg;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string _host;   // bucket.oss-region.aliyuncs.com
    private readonly string _scheme;

    public OssStorageService(StorageConfig cfg, IHttpClientFactory httpFactory)
    {
        _cfg = cfg;
        _httpFactory = httpFactory;
        _scheme = cfg.Ssl ? "https" : "http";
        var endpoint = StripScheme(cfg.Endpoint); // mis. oss-ap-southeast-5.aliyuncs.com
        _host = string.IsNullOrWhiteSpace(cfg.BaseUrl)
            ? $"{cfg.Bucket}.{endpoint}"
            : StripScheme(cfg.BaseUrl);
    }

    private static string StripScheme(string s) =>
        (s ?? string.Empty)
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

    private static string EncodeKey(string key) =>
        string.Join('/', key.Split('/').Select(Uri.EscapeDataString));

    // ── Presigned GET URL (render) ───────────────────────────────────────────
    public string Url(string? keyOrValue, int ttlSeconds = 21600)
    {
        if (string.IsNullOrWhiteSpace(keyOrValue)) return string.Empty;
        if (keyOrValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            keyOrValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return keyOrValue;

        var key = keyOrValue.Replace('\\', '/').TrimStart('/');
        var expires = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds).ToUnixTimeSeconds();
        var resource = $"/{_cfg.Bucket}/{key}";
        var stringToSign = $"GET\n\n\n{expires}\n{resource}";
        var signature = Sign(stringToSign);

        var qs = $"OSSAccessKeyId={Uri.EscapeDataString(_cfg.AccessKey)}" +
                 $"&Expires={expires}" +
                 $"&Signature={Uri.EscapeDataString(signature)}";
        return $"{_scheme}://{_host}/{EncodeKey(key)}?{qs}";
    }

    // ── Signed operations (put/list/delete) ──────────────────────────────────
    public async Task PutAsync(string key, Stream content, string? contentType = null, CancellationToken ct = default)
    {
        key = key.Replace('\\', '/').TrimStart('/');
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var body = ms.ToArray();
        contentType ??= "application/octet-stream";

        var req = BuildRequest(HttpMethod.Put, $"/{_cfg.Bucket}/{key}", $"/{EncodeKey(key)}", contentType);
        req.Content = new ByteArrayContent(body);
        req.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        await SendAsync(req, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        key = key.Replace('\\', '/').TrimStart('/');
        var req = BuildRequest(HttpMethod.Delete, $"/{_cfg.Bucket}/{key}", $"/{EncodeKey(key)}", null);
        await SendAsync(req, ct);
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, int maxKeys = 100, CancellationToken ct = default)
    {
        prefix = prefix.Replace('\\', '/').TrimStart('/');
        var pathAndQuery = $"/?prefix={Uri.EscapeDataString(prefix)}&max-keys={maxKeys}";
        // CanonicalizedResource untuk list = "/{bucket}/"
        var req = BuildRequest(HttpMethod.Get, $"/{_cfg.Bucket}/", pathAndQuery, null);
        var resp = await SendAsync(req, ct);
        var xml = await resp.Content.ReadAsStringAsync(ct);
        return ParseKeys(xml, prefix);
    }

    private static IReadOnlyList<string> ParseKeys(string xml, string prefix)
    {
        if (string.IsNullOrWhiteSpace(xml)) return [];
        var doc = XDocument.Parse(xml);
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        return doc.Descendants(ns + "Contents")
            .Select(c => c.Element(ns + "Key")?.Value)
            .Where(k => !string.IsNullOrEmpty(k) && k != prefix && !k!.EndsWith('/'))
            .Select(k => k!)
            .ToList();
    }

    private HttpRequestMessage BuildRequest(
        HttpMethod method, string canonicalizedResource, string pathAndQuery, string? contentType)
    {
        var date = DateTime.UtcNow.ToString("r", CultureInfo.InvariantCulture); // RFC1123 GMT
        var ctHeader = contentType ?? string.Empty;
        var stringToSign = $"{method.Method}\n\n{ctHeader}\n{date}\n{canonicalizedResource}";
        var signature = Sign(stringToSign);

        var req = new HttpRequestMessage(method, $"{_scheme}://{_host}{pathAndQuery}");
        req.Headers.TryAddWithoutValidation("Host", _host);
        req.Headers.TryAddWithoutValidation("Date", date);
        req.Headers.TryAddWithoutValidation("Authorization", $"OSS {_cfg.AccessKey}:{signature}");
        return req;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("storage");
        var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"OSS request failed ({(int)resp.StatusCode}): {body}");
        }
        return resp;
    }

    private string Sign(string stringToSign)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_cfg.SecretKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
    }
}

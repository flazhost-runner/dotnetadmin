using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using DotNetAdmin.Config;

namespace DotNetAdmin.Core.Storage;

/// <summary>
/// Driver <c>s3</c>: AWS S3 & kompatibel (MinIO, Cloudflare R2, Backblaze B2, ...).
/// Presign URL memakai AWS Signature V4 yang di-hand-roll (tanpa SDK), mengikuti
/// implementasi referensi NodeAdmin. <see cref="Url"/> menghasilkan URL absolut
/// ter-presign berlaku <c>ttlSeconds</c>; tidak ada penyajian file lokal.
/// </summary>
public sealed class S3StorageService : IStorageService
{
    private readonly StorageConfig _cfg;
    private readonly IHttpClientFactory _httpFactory;

    private readonly string _region;
    private readonly bool _pathStyle;
    private readonly string _host;
    private readonly string _scheme;

    public S3StorageService(StorageConfig cfg, IHttpClientFactory httpFactory)
    {
        _cfg = cfg;
        _httpFactory = httpFactory;
        _region = string.IsNullOrWhiteSpace(cfg.Region) ? "us-east-1" : cfg.Region;
        _pathStyle = !string.IsNullOrWhiteSpace(cfg.Endpoint);
        _scheme = cfg.Ssl ? "https" : "http";
        _host = _pathStyle
            ? StripScheme(cfg.Endpoint)
            : $"{cfg.Bucket}.s3.{_region}.amazonaws.com";
    }

    private static string StripScheme(string s) =>
        s.Replace("https://", "", StringComparison.OrdinalIgnoreCase)
         .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
         .TrimEnd('/');

    private string CanonicalUri(string key)
    {
        var encodedKey = string.Join('/', key.Split('/').Select(Uri.EscapeDataString));
        return _pathStyle ? $"/{_cfg.Bucket}/{encodedKey}" : $"/{encodedKey}";
    }

    // ── Presigned GET URL (render) ───────────────────────────────────────────
    public string Url(string? keyOrValue, int ttlSeconds = 21600)
    {
        if (string.IsNullOrWhiteSpace(keyOrValue)) return string.Empty;
        if (keyOrValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            keyOrValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return keyOrValue;

        var key = keyOrValue.Replace('\\', '/').TrimStart('/');
        var now = DateTime.UtcNow;
        var dateStr = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var credScope = $"{dateStr}/{_region}/s3/aws4_request";
        var canonicalUri = CanonicalUri(key);

        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = $"{_cfg.AccessKey}/{credScope}",
            ["X-Amz-Date"] = amzDate,
            ["X-Amz-Expires"] = ttlSeconds.ToString(),
            ["X-Amz-SignedHeaders"] = "host",
        };
        var canonicalQs = string.Join('&', query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var canonicalRequest = string.Join('\n',
            "GET", canonicalUri, canonicalQs, $"host:{_host}\n", "host", "UNSIGNED-PAYLOAD");
        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256", amzDate, credScope, Hex(Sha256(canonicalRequest)));
        var signature = Hex(HmacSha256(SigningKey(dateStr), stringToSign));

        return $"{_scheme}://{_host}{canonicalUri}?{canonicalQs}&X-Amz-Signature={signature}";
    }

    // ── Signed operations (put/list/delete) ──────────────────────────────────
    public async Task PutAsync(string key, Stream content, string? contentType = null, CancellationToken ct = default)
    {
        key = key.Replace('\\', '/').TrimStart('/');
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var body = ms.ToArray();
        using var req = SignedRequest(HttpMethod.Put, CanonicalUri(key), body, contentType, null);
        req.Content = new ByteArrayContent(body);
        if (!string.IsNullOrEmpty(contentType))
            req.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        await SendAsync(req, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        key = key.Replace('\\', '/').TrimStart('/');
        using var req = SignedRequest(HttpMethod.Delete, CanonicalUri(key), [], null, null);
        await SendAsync(req, ct);
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, int maxKeys = 100, CancellationToken ct = default)
    {
        prefix = prefix.Replace('\\', '/').TrimStart('/');
        var query = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["list-type"] = "2",
            ["max-keys"] = maxKeys.ToString(),
            ["prefix"] = prefix,
        };
        var uri = _pathStyle ? $"/{_cfg.Bucket}" : "/";
        using var req = SignedRequest(HttpMethod.Get, uri, [], null, query);
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

    // ── SigV4 header-signed request builder ──────────────────────────────────
    private HttpRequestMessage SignedRequest(
        HttpMethod method, string canonicalUri, byte[] payload,
        string? contentType, SortedDictionary<string, string>? query)
    {
        var now = DateTime.UtcNow;
        var dateStr = now.ToString("yyyyMMdd");
        var amzDate = now.ToString("yyyyMMddTHHmmssZ");
        var credScope = $"{dateStr}/{_region}/s3/aws4_request";
        var payloadHash = Hex(Sha256Bytes(payload));

        var canonicalQs = query is null ? "" : string.Join('&', query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        // Signed headers: host, x-amz-content-sha256, x-amz-date (alfabetis).
        var headers = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["host"] = _host,
            ["x-amz-content-sha256"] = payloadHash,
            ["x-amz-date"] = amzDate,
        };
        var canonicalHeaders = string.Concat(headers.Select(kv => $"{kv.Key}:{kv.Value}\n"));
        var signedHeaders = string.Join(';', headers.Keys);

        var canonicalRequest = string.Join('\n',
            method.Method, canonicalUri, canonicalQs, canonicalHeaders, signedHeaders, payloadHash);
        var stringToSign = string.Join('\n',
            "AWS4-HMAC-SHA256", amzDate, credScope, Hex(Sha256(canonicalRequest)));
        var signature = Hex(HmacSha256(SigningKey(dateStr), stringToSign));

        var authorization =
            $"AWS4-HMAC-SHA256 Credential={_cfg.AccessKey}/{credScope}, " +
            $"SignedHeaders={signedHeaders}, Signature={signature}";

        var url = $"{_scheme}://{_host}{canonicalUri}";
        if (!string.IsNullOrEmpty(canonicalQs)) url += $"?{canonicalQs}";

        var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("Host", _host);
        req.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        req.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        req.Headers.TryAddWithoutValidation("Authorization", authorization);
        return req;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("storage");
        var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"S3 request failed ({(int)resp.StatusCode}): {body}");
        }
        return resp;
    }

    private byte[] SigningKey(string dateStr)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{_cfg.SecretKey}"), dateStr);
        var kRegion = HmacSha256(kDate, _region);
        var kService = HmacSha256(kRegion, "s3");
        return HmacSha256(kService, "aws4_request");
    }

    private static byte[] Sha256(string s) => SHA256.HashData(Encoding.UTF8.GetBytes(s));
    private static byte[] Sha256Bytes(byte[] b) => SHA256.HashData(b);
    private static byte[] HmacSha256(byte[] key, string data) =>
        new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(data));
    private static string Hex(byte[] b) => Convert.ToHexStringLower(b);
}

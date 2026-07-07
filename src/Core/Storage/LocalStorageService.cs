namespace DotNetAdmin.Core.Storage;

/// <summary>
/// Driver <c>local</c>: objek disimpan di filesystem di bawah <c>STORAGE_BASE_PATH</c>
/// dan dirender lewat URL relatif stabil <c>/storage/&lt;key&gt;</c> yang di-serve
/// oleh static file middleware (di-mount di Program.cs saat driver=local).
/// </summary>
public sealed class LocalStorageService : IStorageService
{
    private readonly string _baseDir;

    public LocalStorageService(string baseDir)
    {
        _baseDir = baseDir;
    }

    private string PathFor(string key)
    {
        // Cegah path traversal: key dinormalisasi & wajib berada di dalam base dir.
        var normalized = key.Replace('\\', '/').TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(_baseDir, normalized));
        var baseFull = Path.GetFullPath(_baseDir);
        if (!full.StartsWith(baseFull, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid storage key");
        return full;
    }

    public async Task PutAsync(string key, Stream content, string? contentType = null, CancellationToken ct = default)
    {
        var dest = PathFor(key);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        await using var fs = File.Create(dest);
        await content.CopyToAsync(fs, ct);
    }

    public string Url(string? keyOrValue, int ttlSeconds = 21600)
    {
        if (string.IsNullOrWhiteSpace(keyOrValue)) return string.Empty;
        // Sudah URL/path absolut → kembalikan apa adanya (data lama & aset statis).
        if (keyOrValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            keyOrValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            keyOrValue.StartsWith('/'))
            return keyOrValue;
        var key = keyOrValue.Replace('\\', '/').TrimStart('/');
        return $"{LocalStorage.UrlPrefix}/{key}";
    }

    public Task<IReadOnlyList<string>> ListAsync(string prefix, int maxKeys = 100, CancellationToken ct = default)
    {
        var dir = PathFor(prefix);
        if (!Directory.Exists(dir))
            return Task.FromResult<IReadOnlyList<string>>([]);

        var normPrefix = prefix.Replace('\\', '/').Trim('/');
        var items = Directory.EnumerateFiles(dir)
            .Take(maxKeys)
            .Select(f => $"{normPrefix}/{Path.GetFileName(f)}")
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(items);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = PathFor(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }
}

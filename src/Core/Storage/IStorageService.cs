namespace DotNetAdmin.Core.Storage;

/// <summary>
/// Abstraksi penyimpanan objek — mengikuti desain NodeAdmin (driver local/oss/s3).
///
/// Kontrak kunci:
/// - DB menyimpan <b>key</b> objek (mis. <c>profile/abc.png</c>), <b>bukan</b> URL.
/// - <see cref="Url"/> membangun URL render <b>saat request</b> sesuai driver aktif,
///   sehingga berpindah backend (local ↔ oss/s3) cukup lewat konfigurasi — tanpa
///   mengubah kode/view maupun data yang tersimpan.
///   * local  → URL relatif stabil <c>/storage/&lt;key&gt;</c> (di-serve middleware static).
///   * oss/s3 → URL absolut ter-presign (TTL terbatas).
/// </summary>
public interface IStorageService
{
    /// <summary>Simpan objek pada <paramref name="key"/>.</summary>
    Task PutAsync(string key, Stream content, string? contentType = null, CancellationToken ct = default);

    /// <summary>
    /// Bangun URL render dari nilai tersimpan pada saat request.
    /// Menerima key (mis. <c>profile/x.png</c>) → URL sesuai driver.
    /// Nilai kosong → string kosong (pemanggil yang menyediakan fallback).
    /// Nilai yang sudah berupa URL absolut / path absolut (diawali <c>http</c> atau <c>/</c>)
    /// dikembalikan apa adanya (kompatibilitas mundur dengan data lama & aset statis).
    /// </summary>
    string Url(string? keyOrValue, int ttlSeconds = 21600);

    /// <summary>Daftar key di bawah <paramref name="prefix"/>.</summary>
    Task<IReadOnlyList<string>> ListAsync(string prefix, int maxKeys = 100, CancellationToken ct = default);

    /// <summary>Hapus objek pada <paramref name="key"/>.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// Konstanta & helper penyimpanan lokal. Prefix URL <b>dipisah</b> dari path
/// filesystem: <c>STORAGE_BASE_PATH</c> boleh absolut (mis. <c>/app/storage</c> di
/// container) namun URL yang dirender tetap <c>/storage/&lt;key&gt;</c> — bukan
/// <c>//app/storage/...</c>. Program.cs me-mount direktori ini di prefix ini via
/// static file middleware saat driver=local.
/// </summary>
public static class LocalStorage
{
    public const string UrlPrefix = "/storage";

    /// <summary>
    /// Resolusi direktori penyimpanan lokal absolut dari <c>Storage:BasePath</c>.
    /// Relatif → digabung ke <paramref name="contentRoot"/>; absolut → dipakai apa adanya.
    /// </summary>
    public static string ResolveBaseDir(string basePath, string contentRoot)
    {
        var bp = string.IsNullOrWhiteSpace(basePath) ? "storage/uploads" : basePath;
        return Path.IsPathRooted(bp) ? bp : Path.GetFullPath(Path.Combine(contentRoot, bp));
    }
}

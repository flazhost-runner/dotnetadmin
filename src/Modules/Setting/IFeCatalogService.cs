namespace DotNetAdmin.Modules.Setting;

/// <summary>Kontrak FeCatalogService — katalog 640 landing opentailwind (server-side).</summary>
public interface IFeCatalogService
{
    /// <summary>Seluruh katalog (fetch GitHub tree sekali, lalu cache memori + disk).</summary>
    Task<List<FeTemplateItem>> ListAsync();

    /// <summary>Daftar kategori unik (untuk dropdown filter).</summary>
    Task<List<string>> GetCategoriesAsync();

    /// <summary>Hasil paginasi + filter (q_name / q_category). pinSlug → disematkan ke halaman 1.</summary>
    Task<PaginationResult<FeTemplateItem>> GetCatalogAsync(
        string? qName = null, string? qCategory = null, int page = 1, int pageSize = 12, string? pinSlug = null);

    /// <summary>True bila slug ada di katalog (whitelist anti-SSRF).</summary>
    Task<bool> HasAsync(string slug);

    /// <summary>HTML mentah 1 template (on-demand, tanpa tulis disk). Lempar AppException bila gagal.</summary>
    Task<string> GetPreviewHtmlAsync(string slug);
}

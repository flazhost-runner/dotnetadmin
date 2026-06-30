using DotNetAdmin.Core.Helpers;

namespace DotNetAdmin.Modules.Setting;

public record FeCatalogItem(string Slug, string Name, string Category, string PreviewUrl);

public interface IFeCatalogService
{
    Task<PaginationResult<FeCatalogItem>> GetCatalogAsync(string? search = null, string? category = null, int page = 1, int pageSize = 12, string? pinSlug = null);
    Task<string?> GetPreviewHtmlAsync(string slug);
    Task<List<string>> GetCategoriesAsync();
}

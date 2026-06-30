namespace DotNetAdmin.Modules.Media;

public record MediaItem(string Name, string Url, string Key);

public interface IMediaService
{
    Task<List<MediaItem>> ListAsync();
    Task<MediaItem> UploadAsync(IFormFile file);
    Task DeleteAsync(string key);
}

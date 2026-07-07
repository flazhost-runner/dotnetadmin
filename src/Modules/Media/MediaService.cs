using DotNetAdmin.Core.Errors;
using DotNetAdmin.Core.Storage;

namespace DotNetAdmin.Modules.Media;

public class MediaService : IMediaService
{
    private readonly IStorageService _storage;

    private const string Prefix = "editor";
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { ".png",  [0x89, 0x50, 0x4E, 0x47] },
        { ".jpg",  [0xFF, 0xD8, 0xFF] },
        { ".jpeg", [0xFF, 0xD8, 0xFF] },
        { ".webp", [0x52, 0x49, 0x46, 0x46] },
    };

    public MediaService(IStorageService storage)
    {
        _storage = storage;
    }

    public async Task<List<MediaItem>> ListAsync()
    {
        var keys = await _storage.ListAsync(Prefix, 200);
        return keys
            .Where(k => AllowedExtensions.Contains(Path.GetExtension(k).ToLower()))
            .Select(k => new MediaItem(Path.GetFileName(k), _storage.Url(k), k))
            .OrderByDescending(i => i.Name)
            .ToList();
    }

    public async Task<MediaItem> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ValidationAppException("No file provided");

        if (file.Length > MaxFileSizeBytes)
            throw new ValidationAppException("File size exceeds 2MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!AllowedExtensions.Contains(ext))
            throw new ValidationAppException($"File type '{ext}' not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");

        // Validate magic bytes
        await using var stream = file.OpenReadStream();
        var magic = MagicBytes.GetValueOrDefault(ext);
        if (magic != null)
        {
            var header = new byte[magic.Length];
            var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
            if (read < magic.Length || !header.Take(magic.Length).SequenceEqual(magic))
                throw new ValidationAppException("File content does not match the declared extension");
            stream.Seek(0, SeekOrigin.Begin);
        }

        var key = $"{Prefix}/{Guid.NewGuid():N}{ext}";
        await _storage.PutAsync(key, stream, file.ContentType);

        return new MediaItem(Path.GetFileName(key), _storage.Url(key), key);
    }

    public async Task DeleteAsync(string key)
    {
        if (!key.StartsWith($"{Prefix}/") || key.Contains(".."))
            throw new ValidationAppException("Invalid file key");

        var fileName = Path.GetFileName(key);
        if (string.IsNullOrEmpty(fileName))
            throw new ValidationAppException("Invalid file key");

        await _storage.DeleteAsync(key);
    }
}

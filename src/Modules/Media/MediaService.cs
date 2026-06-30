using DotNetAdmin.Core.Errors;

namespace DotNetAdmin.Modules.Media;

public class MediaService : IMediaService
{
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2MB

    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        { ".png",  [0x89, 0x50, 0x4E, 0x47] },
        { ".jpg",  [0xFF, 0xD8, 0xFF] },
        { ".jpeg", [0xFF, 0xD8, 0xFF] },
        { ".webp", [0x52, 0x49, 0x46, 0x46] },
    };

    public MediaService(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string StorageDir => Path.Combine(_env.WebRootPath, "storage", "editor");

    public Task<List<MediaItem>> ListAsync()
    {
        Directory.CreateDirectory(StorageDir);
        var items = Directory.GetFiles(StorageDir)
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLower()))
            .Select(f => {
                var name = Path.GetFileName(f);
                return new MediaItem(name, $"/storage/editor/{name}", $"editor/{name}");
            })
            .OrderByDescending(i => i.Name)
            .ToList();
        return Task.FromResult(items);
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

        Directory.CreateDirectory(StorageDir);
        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(StorageDir, uniqueName);
        await using var dest = File.Create(path);
        await stream.CopyToAsync(dest);

        return new MediaItem(uniqueName, $"/storage/editor/{uniqueName}", $"editor/{uniqueName}");
    }

    public Task DeleteAsync(string key)
    {
        if (!key.StartsWith("editor/") || key.Contains(".."))
            throw new ValidationAppException("Invalid file key");

        var fileName = Path.GetFileName(key.Substring("editor/".Length));
        if (string.IsNullOrEmpty(fileName))
            throw new ValidationAppException("Invalid file key");

        var path = Path.Combine(StorageDir, fileName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}

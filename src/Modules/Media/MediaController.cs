using DotNetAdmin.Core.Errors;

namespace DotNetAdmin.Modules.Media;

[Route("admin/v1/media")]
[Authorize(AuthenticationSchemes = "WebCookie")]
public class MediaController : Controller
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet("list", Name = "admin.v1.media.list")]
    public async Task<IActionResult> List()
    {
        var items = await _mediaService.ListAsync();
        return Json(new { status = true, message = "OK", data = items });
    }

    [HttpPost("upload", Name = "admin.v1.media.upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        // Max 2MB, MIME: jpeg/jpg/png/webp only
        if (file.Length > 2 * 1024 * 1024)
            return StatusCode(422, new { status = false, message = "File size must not exceed 2MB." });

        var allowed = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowed.Contains(file.ContentType.ToLowerInvariant()))
            return StatusCode(422, new { status = false, message = "Only jpeg, jpg, png, webp files are allowed." });

        try
        {
            var item = await _mediaService.UploadAsync(file);
            return Json(new { status = true, message = "Upload success.", data = new { name = item.Name, url = item.Url } });
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { status = false, message = ex.Message });
        }
    }

    [HttpPost("delete", Name = "admin.v1.media.delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] string key)
    {
        try
        {
            await _mediaService.DeleteAsync(key);
            return Json(new { status = true, message = "Delete success.", data = (object?)null });
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { status = false, message = ex.Message });
        }
    }
}

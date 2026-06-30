namespace DotNetAdmin.Modules.Setting;

public interface IFeTemplateService
{
    Task EnsureAsync(string slug);
    Task<string> GetActiveHtmlAsync(Core.Data.Entities.Setting setting);
}

public class FeTemplateService : IFeTemplateService
{
    private readonly IWebHostEnvironment _env;
    private readonly IFeCatalogService _catalog;

    public FeTemplateService(IWebHostEnvironment env, IFeCatalogService catalog)
    {
        _env = env;
        _catalog = catalog;
    }

    public async Task EnsureAsync(string slug)
    {
        var dir = Path.Combine(_env.ContentRootPath, "public", "fe", "templates");
        var path = Path.Combine(dir, slug + ".html");
        if (File.Exists(path)) return;
        var html = await _catalog.GetPreviewHtmlAsync(slug);
        if (html != null)
        {
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(path, html);
        }
    }

    public async Task<string> GetActiveHtmlAsync(Core.Data.Entities.Setting setting)
    {
        var slug = setting.FeTemplate ?? "agency-consulting-002-creative-agency";
        if (slug == "agency-consulting-002-creative-agency")
            return "__native__";

        var path = Path.Combine(_env.ContentRootPath, "public", "fe", "templates", slug + ".html");
        if (File.Exists(path)) return await File.ReadAllTextAsync(path);

        await EnsureAsync(slug);
        if (File.Exists(path)) return await File.ReadAllTextAsync(path);

        return "__native__";
    }
}

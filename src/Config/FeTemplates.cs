using System.Text.RegularExpressions;

namespace DotNetAdmin.Config;

/// <summary>Satu item katalog template frontend (landing) opentailwind.</summary>
public record FeTemplateItem(string Slug, string Name, string Category);

/// <summary>
/// Katalog template frontend (landing) — kurasi dari opentailwind
/// (https://github.com/lindoai/opentailwind, MIT). Mirror dari NodeAdmin
/// src/config/feTemplates.ts. Tiap template self-contained (HTML + Tailwind v4
/// CDN) dan di-download on-demand saat admin memilihnya (lihat FeTemplateService).
///
/// Slug khusus 'default' merender view landing lokal (fe/default, landing v6)
/// alih-alih file HTML hasil unduhan.
/// </summary>
public static class FeTemplates
{
    /// <summary>Basis URL raw GitHub opentailwind untuk download on-demand.</summary>
    public const string BaseUrl = "https://raw.githubusercontent.com/lindoai/opentailwind/master/landings";

    /// <summary>GitHub API tree (recursive) untuk mendaftar seluruh 640 landing.</summary>
    public const string TreeUrl = "https://api.github.com/repos/lindoai/opentailwind/git/trees/master?recursive=1";

    /// <summary>
    /// Folder cache lokal (relatif ContentRoot). Berada di storage/ (folder
    /// runtime writable, sejajar dgn upload media) — BUKAN wwwroot, sehingga
    /// file cache tidak pernah tersaji langsung sebagai static file publik.
    /// </summary>
    public const string Dir = "storage/fe/templates";

    /// <summary>File cache katalog (daftar 640) hasil fetch tree, agar tak bebani GitHub.</summary>
    public const string CatalogFile = "storage/fe/templates/_catalog.json";

    /// <summary>
    /// Pola slug opentailwind: `{kategori}-{NNN}-{nama}` (kategori boleh
    /// ber-hyphen, mis. `agency-consulting`). Dipakai validator (anti-SSRF:
    /// charset a-z0-9- + struktur tetap) & derive metadata.
    /// </summary>
    public static readonly Regex SlugRe = new(@"^([a-z]+(?:-[a-z]+)*)-(\d{3})-([a-z0-9-]+)$", RegexOptions.Compiled);

    /// <summary>Slug khusus: render view landing lokal (fe/default, landing v6).</summary>
    public const string DefaultView = "default";

    /// <summary>Template default aktif (sama dgn NodeAdmin DEFAULT_FE_TEMPLATE).</summary>
    public const string Default = "agency-consulting-002-creative-agency";

    /// <summary>Title-case dari segmen hyphen: `digital-marketing` → `Digital Marketing`.</summary>
    public static string Titleize(string value) =>
        string.Join(' ', value.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

    /// <summary>
    /// Derive metadata tampil dari slug opentailwind. Bila slug tak cocok pola,
    /// pakai slug apa adanya sebagai name & kategori 'Other'.
    /// </summary>
    public static FeTemplateItem Derive(string slug)
    {
        var m = SlugRe.Match(slug);
        if (!m.Success) return new FeTemplateItem(slug, Titleize(slug), "Other");
        return new FeTemplateItem(slug, Titleize(m.Groups[3].Value), Titleize(m.Groups[1].Value));
    }

    /// <summary>Slug valid: 'default' (view lokal) atau cocok pola opentailwind.</summary>
    public static bool IsValidSlug(string? slug) =>
        !string.IsNullOrWhiteSpace(slug) && (slug == DefaultView || SlugRe.IsMatch(slug));

    /// <summary>Katalog kurasi (~15 dari 640 landing opentailwind) — fallback offline.</summary>
    public static readonly IReadOnlyList<FeTemplateItem> Curated =
    [
        new("agency-consulting-002-creative-agency", "Creative Agency", "Agency"),
        new("agency-consulting-001-digital-marketing-agency", "Digital Marketing Agency", "Agency"),
        new("technology-saas-001-hero-focused-conversion-page", "SaaS — Hero Focused", "Technology"),
        new("technology-saas-002-feature-rich-multi-section", "SaaS — Feature Rich", "Technology"),
        new("ecommerce-retail-001-fashion-boutique", "Fashion Boutique", "E-commerce"),
        new("ecommerce-retail-002-luxury-fashion-brand", "Luxury Fashion", "E-commerce"),
        new("portfolio-creative-001-creative-portfolio", "Creative Portfolio", "Portfolio"),
        new("portfolio-creative-002-minimal-portfolio", "Minimal Portfolio", "Portfolio"),
        new("professional-services-001-law-firm", "Law Firm", "Professional"),
        new("real-estate-property-001-real-estate-agency", "Real Estate Agency", "Real Estate"),
        new("food-hospitality-001-fine-dining-restaurant", "Fine Dining", "Food"),
        new("healthcare-wellness-001-family-doctor-clinic", "Family Clinic", "Healthcare"),
        new("education-training-001-private-school", "Private School", "Education"),
        new("fitness-sports-001-fitness-center", "Fitness Center", "Fitness"),
        new("travel-tourism-001-travel-agency", "Travel Agency", "Travel"),
    ];
}

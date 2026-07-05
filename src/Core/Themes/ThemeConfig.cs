namespace DotNetAdmin.Core.Themes;

public record ThemePalette(string Name, string Primary, string Secondary, string Light, string Dark);

/// <summary>
/// Peta palet tema untuk template switcher — mirror dari NodeAdmin
/// @flazhost-nodeadmin/core config/themes.ts (10 varian warna yang strukturnya
/// identik, hanya berbeda 4 nilai warna primary/secondary/light/dark).
///
/// Satu set view Tailwind (be/default) didorong oleh nilai-nilai ini via CSS
/// variable + tailwind.config inline di layout, sehingga ganti tema = ganti
/// palet saat render tanpa menduplikasi view per warna.
/// </summary>
public static class ThemeConfig
{
    public const string DefaultTheme = "Blue";

    public static readonly ThemePalette[] Themes =
    [
        new("Blue",   "#3B82F6", "#60A5FA", "#DBEAFE", "#1E40AF"),
        new("Black",  "#374151", "#4B5563", "#6B7280", "#1F2937"),
        new("Brown",  "#A16207", "#D97706", "#FEF3C7", "#78350F"),
        new("Green",  "#10B981", "#34D399", "#D1FAE5", "#047857"),
        new("Grey",   "#6B7280", "#9CA3AF", "#E5E7EB", "#374151"),
        new("Orange", "#F59E0B", "#FBBF24", "#FEF3C7", "#D97706"),
        new("Purple", "#8B5CF6", "#A78BFA", "#F3E8FF", "#6D28D9"),
        new("Red",    "#EF4444", "#F87171", "#FECACA", "#B91C1C"),
        new("Yellow", "#F59E0B", "#FCD34D", "#FEF3C7", "#D97706"),
    ];

    public static ThemePalette GetTheme(string? name) =>
        Themes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? Themes.First(t => t.Name == DefaultTheme);
}

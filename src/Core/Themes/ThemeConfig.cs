namespace DotNetAdmin.Core.Themes;

public record ThemePalette(string Name, string Primary, string Secondary, string Light, string Dark);

public static class ThemeConfig
{
    public static readonly ThemePalette[] Themes =
    [
        new("Blue",   "#3B82F6", "#60A5FA", "#EFF6FF", "#1E40AF"),
        new("Purple", "#8B5CF6", "#A78BFA", "#F5F3FF", "#5B21B6"),
        new("Green",  "#10B981", "#34D399", "#ECFDF5", "#065F46"),
        new("Orange", "#F59E0B", "#FCD34D", "#FFFBEB", "#92400E"),
        new("Red",    "#EF4444", "#F87171", "#FEF2F2", "#991B1B"),
    ];

    public static ThemePalette GetTheme(string? name) =>
        Themes.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? Themes[0];
}

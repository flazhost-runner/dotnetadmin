namespace DotNetAdmin.Core.Data.Entities;

public class Setting : BaseEntity
{
    public string? Initial { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Logo { get; set; }
    public string? Favicon { get; set; }
    public string? LoginImage { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Copyright { get; set; }
    public string Theme { get; set; } = "Blue";
    public string FeTemplate { get; set; } = "agency-consulting-002-creative-agency";
}

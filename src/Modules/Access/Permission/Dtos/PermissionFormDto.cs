namespace DotNetAdmin.Modules.Access.Permission.Dtos;

public class PermissionFormDto
{
    [FromForm(Name = "name")]      public string Name { get; set; } = string.Empty;
    [FromForm(Name = "guard_name")] public string GuardName { get; set; } = "web";
    [FromForm(Name = "method")]    public string? Method { get; set; }
    [FromForm(Name = "desc")]      public string? Desc { get; set; }
    [FromForm(Name = "status")]    public string Status { get; set; } = "Active";
}

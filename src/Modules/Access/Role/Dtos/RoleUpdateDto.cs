namespace DotNetAdmin.Modules.Access.Role.Dtos;

public class RoleUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? Desc { get; set; }
}

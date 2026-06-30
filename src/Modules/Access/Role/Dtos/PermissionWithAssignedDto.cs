namespace DotNetAdmin.Modules.Access.Role.Dtos;

public class PermissionWithAssignedDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Desc { get; set; }
    public bool IsAssigned { get; set; }
}

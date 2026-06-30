namespace DotNetAdmin.Core.Data.Entities;

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public string PermissionId { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}

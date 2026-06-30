namespace DotNetAdmin.Core.Data.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string GuardName { get; set; } = "web";
    public string Status { get; set; } = "Active";
    public string? Desc { get; set; }  // maps to "desc" column (SQL reserved word — pinned via Fluent API)

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

namespace DotNetAdmin.Core.Data.Entities;

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;  // NON-unique (indexed only)
    public string GuardName { get; set; } = "web";
    public string? Method { get; set; }
    public string Status { get; set; } = "Active";
    public string? Desc { get; set; }  // maps to "desc" column (SQL reserved word)

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

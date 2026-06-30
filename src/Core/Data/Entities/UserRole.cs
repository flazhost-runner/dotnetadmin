namespace DotNetAdmin.Core.Data.Entities;

public class UserRole
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

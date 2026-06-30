namespace DotNetAdmin.Core.Data.Entities;

public class User : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime? EmailVerifiedAt { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? PasswordOtp { get; set; }
    public long? PasswordOtpExpires { get; set; }
    public string Status { get; set; } = "Active";
    public string? Picture { get; set; }
    public bool Blocked { get; set; } = false;
    public string? BlockedReason { get; set; }
    public string Timezone { get; set; } = "UTC";

    public ICollection<UserRole> UserRoles { get; set; } = [];
}

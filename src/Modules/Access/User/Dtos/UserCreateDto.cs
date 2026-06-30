namespace DotNetAdmin.Modules.Access.User.Dtos;

public class UserCreateDto
{
    public string Code    { get; set; } = string.Empty;
    public string Name    { get; set; } = string.Empty;
    public string? Phone  { get; set; }
    public string Email   { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public string Password { get; set; } = string.Empty;
    public string password_confirmation { get; set; } = string.Empty;
    public string Status  { get; set; } = "Active";
    public bool   Blocked { get; set; } = false;
    public string? blocked_reason { get; set; }
    public string? Picture { get; set; }
    public List<string> Roles { get; set; } = [];
}

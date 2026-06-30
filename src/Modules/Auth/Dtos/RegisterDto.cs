namespace DotNetAdmin.Modules.Auth.Dtos;

public record RegisterDto(
    string Name,
    string Code,
    string Email,
    string Password,
    string PasswordConfirm,
    string? Phone = null);

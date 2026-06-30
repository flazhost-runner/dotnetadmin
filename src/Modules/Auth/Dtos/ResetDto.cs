namespace DotNetAdmin.Modules.Auth.Dtos;

public record ResetRequestDto(string Email);

public record ResetProcessDto(string Email, string Otp, string Password, string PasswordConfirm);

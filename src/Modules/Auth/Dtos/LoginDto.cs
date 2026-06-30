namespace DotNetAdmin.Modules.Auth.Dtos;

public record LoginDto(string Email, string Password, bool Remember = false);

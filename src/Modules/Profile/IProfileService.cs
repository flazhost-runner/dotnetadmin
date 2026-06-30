using DotNetAdmin.Core.Data.Entities;

namespace DotNetAdmin.Modules.Profile;

public class ProfileUpdateDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Timezone { get; set; }
    public string? Password { get; set; }
    public string? PasswordConfirmation { get; set; }
    public IFormFile? Picture { get; set; }
}

public interface IProfileService
{
    Task<User> GetAsync(string userId);
    Task<User> UpdateAsync(string userId, ProfileUpdateDto dto);
}

using DotNetAdmin.Core.Data.Entities;

namespace DotNetAdmin.Modules.Setting;

public class SettingUpdateDto
{
    public string? Initial { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Copyright { get; set; }
    public string? Theme { get; set; }
    public string? fe_template { get; set; }
    public IFormFile? icon { get; set; }
    public IFormFile? logo { get; set; }
    public IFormFile? favicon { get; set; }
    public IFormFile? login_image { get; set; }
}

public interface ISettingService
{
    Task<Core.Data.Entities.Setting> GetAsync();
    Task<Core.Data.Entities.Setting> UpdateAsync(SettingUpdateDto dto);
}

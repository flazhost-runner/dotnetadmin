namespace DotNetAdmin.Modules.__ModuleName__;

public interface I__ModuleName__Service
{
    Task<List<__ModuleName__Dto>> GetAllAsync();
    Task<__ModuleName__Dto?> GetByIdAsync(string id);
    Task<__ModuleName__Dto> CreateAsync(__ModuleName__Dto dto);
    Task<__ModuleName__Dto> UpdateAsync(string id, __ModuleName__Dto dto);
    Task DeleteAsync(string id);
}

using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Exceptions;

namespace DotNetAdmin.Modules.__ModuleName__;

public class __ModuleName__Service : I__ModuleName__Service
{
    private readonly AppDbContext _db;

    public __ModuleName__Service(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<__ModuleName__Dto>> GetAllAsync() => throw new NotImplementedException();
    public Task<__ModuleName__Dto?> GetByIdAsync(string id) => throw new NotImplementedException();
    public Task<__ModuleName__Dto> CreateAsync(__ModuleName__Dto dto) => throw new NotImplementedException();
    public Task<__ModuleName__Dto> UpdateAsync(string id, __ModuleName__Dto dto) => throw new NotImplementedException();
    public Task DeleteAsync(string id) => throw new NotImplementedException();
}

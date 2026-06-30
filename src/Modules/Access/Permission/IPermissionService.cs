using DotNetAdmin.Modules.Access.Permission.Dtos;
using PermissionEntity = DotNetAdmin.Core.Data.Entities.Permission;

namespace DotNetAdmin.Modules.Access.Permission;

public interface IPermissionService
{
    Task<PaginationResult<PermissionEntity>> GetAllAsync(PermissionFilterDto filter);
    Task<PermissionEntity> GetByIdAsync(string id);
    Task CreateAsync(PermissionFormDto dto);
    Task UpdateAsync(string id, PermissionFormDto dto);
    Task DeleteAsync(string id);
    Task DeleteSelectedAsync(IEnumerable<string> ids);
}

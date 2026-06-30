using DotNetAdmin.Modules.Access.Role.Dtos;
using RoleEntity = DotNetAdmin.Core.Data.Entities.Role;
using PermissionEntity = DotNetAdmin.Core.Data.Entities.Permission;

namespace DotNetAdmin.Modules.Access.Role;

public interface IRoleService
{
    Task<PaginationResult<RoleEntity>> GetAllAsync(RoleFilterDto filter);
    Task<RoleEntity> GetByIdAsync(string id);
    Task<RoleEntity> CreateAsync(RoleCreateDto dto, string createdBy);
    Task<RoleEntity> UpdateAsync(string id, RoleUpdateDto dto, string updatedBy);
    Task DeleteAsync(string id);
    Task DeleteSelectedAsync(IEnumerable<string> ids);
    Task<List<PermissionEntity>> GetPermissionsForRoleAsync(string id);
    Task SyncPermissionsAsync(string roleId, IEnumerable<string> permissionIds);
    Task<List<PermissionEntity>> GetAllPermissionsAsync();
    Task<PaginationResult<PermissionWithAssignedDto>> GetPermissionsPaginatedAsync(string roleId, RolePermissionFilterDto filter);
    Task AssignPermissionAsync(string roleId, string permId);
    Task UnassignPermissionAsync(string roleId, string permId);
    Task AssignSelectedAsync(string roleId, IEnumerable<string> permIds);
    Task UnassignSelectedAsync(string roleId, IEnumerable<string> permIds);
}

using DotNetAdmin.Modules.Access.User.Dtos;
using UserEntity = DotNetAdmin.Core.Data.Entities.User;
using RoleEntity = DotNetAdmin.Core.Data.Entities.Role;

namespace DotNetAdmin.Modules.Access.User;

public interface IUserService
{
    Task<PaginationResult<UserEntity>> GetAllAsync(UserFilterDto filter);
    Task<UserEntity> GetByIdAsync(string id);
    Task<UserEntity> CreateAsync(UserCreateDto dto, string createdBy);
    Task<UserEntity> UpdateAsync(string id, UserUpdateDto dto, string updatedBy);
    Task DeleteAsync(string id);
    Task DeleteSelectedAsync(IEnumerable<string> ids);
    Task<List<RoleEntity>> GetAllRolesAsync();
}

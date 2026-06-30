using DotNetAdmin.Modules.Access.Role.Dtos;
using RoleEntity = DotNetAdmin.Core.Data.Entities.Role;
using PermissionEntity = DotNetAdmin.Core.Data.Entities.Permission;

namespace DotNetAdmin.Modules.Access.Role;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;

    public RoleService(AppDbContext db) => _db = db;

    public async Task<PaginationResult<RoleEntity>> GetAllAsync(RoleFilterDto filter)
    {
        var query = _db.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.QName))
            query = query.WhereCiLike(r => r.Name, filter.QName);

        if (!string.IsNullOrWhiteSpace(filter.QStatus))
            query = query.Where(r => r.Status == filter.QStatus);

        if (!string.IsNullOrWhiteSpace(filter.QDesc))
            query = query.Where(r => r.Desc != null && r.Desc.Contains(filter.QDesc));

        query = query.OrderBy(r => r.Name);
        return await PaginationHelper.PaginateAsync(query, filter.Page, filter.PageSize);
    }

    public async Task<RoleEntity> GetByIdAsync(string id) =>
        await _db.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundAppException("Role not found.");

    public async Task<RoleEntity> CreateAsync(RoleCreateDto dto, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ValidationAppException("Name is required.");
        if (await _db.Roles.AnyAsync(r => r.Name == dto.Name.Trim()))
            throw new ConflictAppException("Role name already exists.");

        var role = new RoleEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            Status = dto.Status,
            Desc = dto.Desc?.Trim(),
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task<RoleEntity> UpdateAsync(string id, RoleUpdateDto dto, string updatedBy)
    {
        var role = await GetByIdAsync(id);
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ValidationAppException("Name is required.");
        if (await _db.Roles.AnyAsync(r => r.Name == dto.Name.Trim() && r.Id != id))
            throw new ConflictAppException("Role name already exists.");

        role.Name = dto.Name.Trim();
        role.Status = dto.Status;
        role.Desc = dto.Desc?.Trim();
        role.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync();
        return role;
    }

    public async Task DeleteAsync(string id)
    {
        var role = await GetByIdAsync(id);
        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSelectedAsync(IEnumerable<string> ids)
    {
        var roles = await _db.Roles.Where(r => ids.Contains(r.Id)).ToListAsync();
        _db.Roles.RemoveRange(roles);
        await _db.SaveChangesAsync();
    }

    public async Task<List<PermissionEntity>> GetPermissionsForRoleAsync(string id)
    {
        var rolePerms = await _db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == id)
            .Select(rp => rp.Permission)
            .ToListAsync();
        return rolePerms;
    }

    public async Task SyncPermissionsAsync(string roleId, IEnumerable<string> permissionIds)
    {
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);

        foreach (var permId in permissionIds.Where(p => !string.IsNullOrWhiteSpace(p)))
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });

        await _db.SaveChangesAsync();
    }

    public async Task<List<PermissionEntity>> GetAllPermissionsAsync() =>
        await _db.Permissions.OrderBy(p => p.Name).ThenBy(p => p.Method).ToListAsync();

    public async Task<PaginationResult<PermissionWithAssignedDto>> GetPermissionsPaginatedAsync(string roleId, RolePermissionFilterDto filter)
    {
        var assignedIds = (await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync()).ToHashSet();

        var query = _db.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.QName))
            query = query.WhereCiLike(p => p.Name, filter.QName);

        if (!string.IsNullOrWhiteSpace(filter.QStatus))
            query = query.Where(p => p.Status == filter.QStatus);

        if (!string.IsNullOrWhiteSpace(filter.QDesc))
            query = query.Where(p => p.Desc != null && p.Desc.Contains(filter.QDesc));

        query = query.OrderBy(p => p.Name);

        var rawResult = await PaginationHelper.PaginateAsync(query, filter.Page, filter.PageSize);

        return new PaginationResult<PermissionWithAssignedDto>
        {
            Data = rawResult.Data.Select(p => new PermissionWithAssignedDto
            {
                Id = p.Id,
                Name = p.Name,
                Desc = p.Desc,
                IsAssigned = assignedIds.Contains(p.Id)
            }).ToList(),
            TotalCount = rawResult.TotalCount,
            Page = rawResult.Page,
            PageSize = rawResult.PageSize,
            TotalPages = rawResult.TotalPages
        };
    }

    public async Task AssignPermissionAsync(string roleId, string permId)
    {
        if (!await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId))
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task UnassignPermissionAsync(string roleId, string permId)
    {
        var rp = await _db.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId);
        if (rp != null) { _db.RolePermissions.Remove(rp); await _db.SaveChangesAsync(); }
    }

    public async Task AssignSelectedAsync(string roleId, IEnumerable<string> permIds)
    {
        foreach (var permId in permIds.Where(p => !string.IsNullOrWhiteSpace(p)))
            if (!await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permId))
                _db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
        await _db.SaveChangesAsync();
    }

    public async Task UnassignSelectedAsync(string roleId, IEnumerable<string> permIds)
    {
        var idList = permIds.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        var existing = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId && idList.Contains(rp.PermissionId))
            .ToListAsync();
        _db.RolePermissions.RemoveRange(existing);
        await _db.SaveChangesAsync();
    }
}

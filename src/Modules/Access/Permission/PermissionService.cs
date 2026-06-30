using DotNetAdmin.Modules.Access.Permission.Dtos;
using PermissionEntity = DotNetAdmin.Core.Data.Entities.Permission;

namespace DotNetAdmin.Modules.Access.Permission;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;

    public PermissionService(AppDbContext db) => _db = db;

    public async Task<PaginationResult<PermissionEntity>> GetAllAsync(PermissionFilterDto filter)
    {
        var query = _db.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.QName))
            query = query.WhereCiLike(p => p.Name, filter.QName);

        if (!string.IsNullOrWhiteSpace(filter.QGuard))
            query = query.Where(p => p.GuardName == filter.QGuard);

        if (!string.IsNullOrWhiteSpace(filter.QMethod))
            query = query.Where(p => p.Method == filter.QMethod.ToUpper());

        if (!string.IsNullOrWhiteSpace(filter.QStatus))
            query = query.Where(p => p.Status == filter.QStatus);

        if (!string.IsNullOrWhiteSpace(filter.QDesc))
            query = query.WhereCiLike(p => p.Desc, filter.QDesc);

        query = query.OrderBy(p => p.Name).ThenBy(p => p.Method);
        return await PaginationHelper.PaginateAsync(query, filter.Page, filter.PageSize);
    }

    public async Task<PermissionEntity> GetByIdAsync(string id) =>
        await _db.Permissions.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundAppException("Permission not found.");

    public async Task CreateAsync(PermissionFormDto dto)
    {
        var perm = new PermissionEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name.Trim(),
            GuardName = dto.GuardName,
            Method = string.IsNullOrWhiteSpace(dto.Method) ? null : dto.Method.Trim().ToUpper(),
            Desc = dto.Desc,
            Status = dto.Status,
        };
        _db.Permissions.Add(perm);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(string id, PermissionFormDto dto)
    {
        var perm = await GetByIdAsync(id);
        perm.Name = dto.Name.Trim();
        perm.GuardName = dto.GuardName;
        perm.Method = string.IsNullOrWhiteSpace(dto.Method) ? null : dto.Method.Trim().ToUpper();
        perm.Desc = dto.Desc;
        perm.Status = dto.Status;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var perm = await GetByIdAsync(id);
        _db.Permissions.Remove(perm);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSelectedAsync(IEnumerable<string> ids)
    {
        var perms = await _db.Permissions.Where(p => ids.Contains(p.Id)).ToListAsync();
        _db.Permissions.RemoveRange(perms);
        await _db.SaveChangesAsync();
    }
}

using DotNetAdmin.Modules.Access.User.Dtos;
using Microsoft.Extensions.Logging;
using UserEntity = DotNetAdmin.Core.Data.Entities.User;
using RoleEntity = DotNetAdmin.Core.Data.Entities.Role;

namespace DotNetAdmin.Modules.Access.User;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UserService> _logger;

    private readonly AppConfig _config;

    public UserService(AppDbContext db, IWebHostEnvironment env, IOptions<AppConfig> appConfig, ILogger<UserService> logger)
    {
        _db = db;
        _env = env;
        _config = appConfig.Value;
        _logger = logger;
    }

    public async Task<PaginationResult<UserEntity>> GetAllAsync(UserFilterDto filter)
    {
        var query = _db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        // Individual field filters (NodeAdmin standard: q_name, q_email, q_code, q_role)
        if (!string.IsNullOrWhiteSpace(filter.q_name))
            query = query.WhereCiLike(u => u.Name, filter.q_name);
        if (!string.IsNullOrWhiteSpace(filter.q_email))
            query = query.WhereCiLike(u => u.Email, filter.q_email);
        if (!string.IsNullOrWhiteSpace(filter.q_code))
            query = query.WhereCiLike(u => u.Code, filter.q_code);
        if (!string.IsNullOrWhiteSpace(filter.q_status))
            query = query.Where(u => u.Status == filter.q_status);
        if (!string.IsNullOrWhiteSpace(filter.q_role))
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == filter.q_role));

        // Legacy search box fallback
        if (!string.IsNullOrWhiteSpace(filter.Search) &&
            string.IsNullOrWhiteSpace(filter.q_name) && string.IsNullOrWhiteSpace(filter.q_email))
        {
            var s = filter.Search;
            query = _db.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.Name.Contains(s) || u.Email.Contains(s) || u.Code.Contains(s));
        }

        // Legacy status filter
        if (!string.IsNullOrWhiteSpace(filter.Status) && string.IsNullOrWhiteSpace(filter.q_status))
            query = query.Where(u => u.Status == filter.Status);

        query = query.OrderByDescending(u => u.CreatedAt);

        return await PaginationHelper.PaginateAsync(query, filter.Page, filter.PageSize);
    }

    public async Task<UserEntity> GetByIdAsync(string id)
    {
        return await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundAppException("User not found.");
    }

    public async Task<UserEntity> CreateAsync(UserCreateDto dto, string createdBy)
    {
        Validate(dto.Code, dto.Name, dto.Email, dto.Password, dto.password_confirmation);

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            throw new ConflictAppException("Email already in use.");
        if (await _db.Users.AnyAsync(u => u.Code == dto.Code))
            throw new ConflictAppException("Code already in use.");

        var picturePath = dto.Picture;

        var user = new UserEntity
        {
            Id = Guid.NewGuid().ToString(),
            Code = dto.Code.Trim(),
            Name = dto.Name.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, _config.BcryptRounds),
            Status = dto.Status,
            Blocked = dto.Blocked,
            BlockedReason = dto.Blocked ? dto.blocked_reason?.Trim() : null,
            Timezone = string.IsNullOrWhiteSpace(dto.Timezone) ? "UTC" : dto.Timezone,
            Picture = picturePath,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await SyncRolesAsync(user.Id, dto.Roles);
        return user;
    }

    public async Task<UserEntity> UpdateAsync(string id, UserUpdateDto dto, string updatedBy)
    {
        var user = await GetByIdAsync(id);

        if (string.IsNullOrWhiteSpace(dto.Code)) throw new ValidationAppException("Code is required.");
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ValidationAppException("Name is required.");
        if (string.IsNullOrWhiteSpace(dto.Email)) throw new ValidationAppException("Email is required.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant() && u.Id != id))
            throw new ConflictAppException("Email already in use.");
        if (await _db.Users.AnyAsync(u => u.Code == dto.Code.Trim() && u.Id != id))
            throw new ConflictAppException("Code already in use.");

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password != dto.password_confirmation)
                throw new ValidationAppException("Passwords do not match.");
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, _config.BcryptRounds);
        }

        if (dto.Picture != null)
        {
            DeletePicture(user.Picture);
            user.Picture = dto.Picture;
        }

        user.Code = dto.Code.Trim();
        user.Name = dto.Name.Trim();
        user.Phone = dto.Phone?.Trim();
        user.Email = dto.Email.Trim().ToLowerInvariant();
        user.Status = dto.Status;
        user.Blocked = dto.Blocked;
        user.BlockedReason = dto.Blocked ? dto.blocked_reason?.Trim() : null;
        user.Timezone = string.IsNullOrWhiteSpace(dto.Timezone) ? "UTC" : dto.Timezone;
        user.UpdatedBy = updatedBy;

        await _db.SaveChangesAsync();
        await SyncRolesAsync(id, dto.Roles);
        return user;
    }

    public async Task DeleteAsync(string id)
    {
        var user = await GetByIdAsync(id);
        DeletePicture(user.Picture);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSelectedAsync(IEnumerable<string> ids)
    {
        var users = await _db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        foreach (var u in users)
            DeletePicture(u.Picture);
        _db.Users.RemoveRange(users);
        await _db.SaveChangesAsync();
    }

    public async Task<List<RoleEntity>> GetAllRolesAsync() =>
        await _db.Roles.OrderBy(r => r.Name).ToListAsync();

    private static void Validate(string code, string name, string email, string password, string passwordConfirm)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ValidationAppException("Code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationAppException("Name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new ValidationAppException("Email is required.");
        if (string.IsNullOrWhiteSpace(password)) throw new ValidationAppException("Password is required.");
        if (password != passwordConfirm) throw new ValidationAppException("Passwords do not match.");
    }

    private async Task<string?> SavePictureAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        if (!allowed.Contains(ext)) throw new ValidationAppException("Invalid image type.");

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "users");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/users/{fileName}";
    }

    private void DeletePicture(string? picturePath)
    {
        if (string.IsNullOrWhiteSpace(picturePath)) return;
        var fullPath = Path.Combine(_env.WebRootPath, picturePath.TrimStart('/'));
        if (File.Exists(fullPath)) File.Delete(fullPath);
    }

    private async Task SyncRolesAsync(string userId, List<string> roleIds)
    {
        var existing = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        _db.UserRoles.RemoveRange(existing);

        foreach (var roleId in roleIds.Where(r => !string.IsNullOrWhiteSpace(r)))
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });

        await _db.SaveChangesAsync();
    }
}

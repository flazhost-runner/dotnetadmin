using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Data.Entities;
using DotNetAdmin.Core.Errors;

namespace DotNetAdmin.Modules.Profile;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProfileService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<User> GetAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundAppException("User not found");
        return user;
    }

    public async Task<User> UpdateAsync(string userId, ProfileUpdateDto dto)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundAppException("User not found");

        if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
        if (!string.IsNullOrWhiteSpace(dto.Timezone)) user.Timezone = dto.Timezone;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Length < 6)
                throw new ValidationAppException("Password must be at least 6 characters");
            if (dto.Password != dto.PasswordConfirmation)
                throw new ValidationAppException("Passwords do not match");
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        if (dto.Picture != null && dto.Picture.Length > 0)
        {
            var dir = Path.Combine(_env.WebRootPath, "storage", "profile");
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(dto.Picture.FileName).ToLower();
            var fileName = $"{userId}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(dir, fileName);
            await using var stream = File.Create(fullPath);
            await dto.Picture.CopyToAsync(stream);
            user.Picture = $"/storage/profile/{fileName}";
        }

        await _db.SaveChangesAsync();
        return user;
    }
}

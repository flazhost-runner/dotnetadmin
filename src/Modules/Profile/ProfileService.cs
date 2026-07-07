using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Data.Entities;
using DotNetAdmin.Core.Errors;
using DotNetAdmin.Core.Storage;

namespace DotNetAdmin.Modules.Profile;

public class ProfileService : IProfileService
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public ProfileService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
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
            var ext = Path.GetExtension(dto.Picture.FileName).ToLower();
            // DB simpan KEY (bukan URL); URL dibangun saat render oleh IStorageService.
            var key = $"profile/{userId}_{Guid.NewGuid():N}{ext}";
            await using var stream = dto.Picture.OpenReadStream();
            await _storage.PutAsync(key, stream, dto.Picture.ContentType);
            user.Picture = key;
        }

        await _db.SaveChangesAsync();
        return user;
    }
}

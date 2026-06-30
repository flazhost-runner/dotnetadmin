using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Data.Entities;
using DotNetAdmin.Core.Helpers;
using DotNetAdmin.Modules.Access.Role;
using DotNetAdmin.Modules.Access.Role.Dtos;
using DotNetAdmin.Modules.Access.User;
using DotNetAdmin.Modules.Access.User.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetAdmin.Tests.Integration;

public class RbacTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RbacTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Administrator_role_exists_and_is_seeded()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var admin = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Name == "Administrator");

        Assert.NotNull(admin);
        Assert.Equal("Administrator", admin.Name);
    }

    [Fact]
    public async Task Permission_name_is_not_unique_allows_same_name_different_guard()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var p1 = new Permission { Id = Guid.NewGuid().ToString(), Name = "duplicate.route.test", Method = "GET", GuardName = "web", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var p2 = new Permission { Id = Guid.NewGuid().ToString(), Name = "duplicate.route.test", Method = "GET", GuardName = "api", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Permissions.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var count = await db.Permissions.CountAsync(p => p.Name == "duplicate.route.test");
        Assert.Equal(2, count);

        db.Permissions.RemoveRange(p1, p2);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task UserService_GetAllAsync_returns_at_least_one_user()
    {
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var result = await userService.GetAllAsync(new UserFilterDto { Page = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    [Fact]
    public async Task RoleService_GetAllAsync_returns_at_least_one_role()
    {
        using var scope = _factory.Services.CreateScope();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

        var result = await roleService.GetAllAsync(new RoleFilterDto { Page = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }
}

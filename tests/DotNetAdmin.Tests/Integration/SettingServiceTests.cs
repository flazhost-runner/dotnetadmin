using DotNetAdmin.Modules.Setting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetAdmin.Tests.Integration;

public class SettingServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SettingServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAsync_returns_setting_or_creates_default()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISettingService>();

        var setting = await service.GetAsync();

        Assert.NotNull(setting);
        Assert.NotNull(setting.Id);
    }

    [Fact]
    public async Task GetAsync_returns_same_row_on_repeated_calls()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISettingService>();

        var s1 = await service.GetAsync();
        var s2 = await service.GetAsync();

        Assert.Equal(s1.Id, s2.Id);
    }
}

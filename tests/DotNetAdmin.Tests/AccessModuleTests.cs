using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DotNetAdmin.Tests;

public class AccessModuleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private string? _token;

    public AccessModuleTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        if (_token != null) return _token;

        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@admin.com",
            password = "12345678"
        });
        resp.EnsureSuccessStatusCode();

        // Response shape: { success, message, data: { token, user } }
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        _token = body.GetProperty("data").GetProperty("token").GetString()!;
        return _token;
    }

    [Fact]
    public async Task GetRoles_Authenticated_Returns200()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.GetAsync("/api/v1/access/role");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetPermissions_Authenticated_Returns200()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.GetAsync("/api/v1/access/permission");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task CreateRole_Authenticated_Returns201()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.PostAsJsonAsync("/api/v1/access/role", new
        {
            name = $"TestRole_{Guid.NewGuid():N}",
            guardName = "web",
            status = "Active"
        });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }
}

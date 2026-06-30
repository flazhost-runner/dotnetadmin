using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace DotNetAdmin.Tests;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ApiLogin_ValidCredentials_Returns200WithToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@admin.com",
            password = "12345678"
        });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
        // token is nested under data
        Assert.True(body.GetProperty("data").GetProperty("token").GetString()?.Length > 0);
    }

    [Fact]
    public async Task ApiLogin_InvalidCredentials_Returns401()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "admin@admin.com",
            password = "wrongpassword"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ApiLogin_MissingEmail_Returns4xx()
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "",
            password = "12345678"
        });

        Assert.True((int)resp.StatusCode >= 400);
    }

    [Fact]
    public async Task GetUsers_Unauthenticated_Returns401()
    {
        var resp = await _client.GetAsync("/api/v1/access/user");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetPermissions_Unauthenticated_Returns401()
    {
        var resp = await _client.GetAsync("/api/v1/access/permission");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task WebLogin_Page_Returns200()
    {
        var resp = await _client.GetAsync("/auth/login");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Unauthenticated_RedirectsToLogin()
    {
        var client = new CustomWebApplicationFactory().CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var resp = await client.GetAsync("/admin/v1/dashboard");
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.Contains("/auth/login", resp.Headers.Location?.ToString() ?? "");
    }
}

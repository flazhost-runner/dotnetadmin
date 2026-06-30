using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Reqnroll;
using Xunit;

namespace DotNetAdmin.Tests.StepDefinitions;

[Binding]
public class AuthSteps
{
    // Each scenario gets a fresh factory+client pair via BoDi constructor injection
    private readonly HttpClient _client;
    private readonly HttpClient _noRedirectClient;
    private HttpResponseMessage? _response;
    private string? _token;

    public AuthSteps()
    {
        var factory = new CustomWebApplicationFactory();
        _client = factory.CreateClient();
        _noRedirectClient = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Given(@"I am on the login page")]
    public void GivenIAmOnTheLoginPage() { }

    [Given(@"I am not authenticated")]
    public void GivenIAmNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        _noRedirectClient.DefaultRequestHeaders.Authorization = null;
    }

    [Given(@"I am logged in as ""(.*)"" with password ""(.*)""")]
    public async Task GivenIAmLoggedIn(string email, string password)
    {
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        _token = body.GetProperty("data").GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token!);
    }

    [When(@"I submit email ""(.*)"" and password ""(.*)""")]
    public async Task WhenISubmitCredentials(string email, string password)
    {
        _response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
    }

    [When(@"I GET ""(.*)""")]
    public async Task WhenIGet(string path)
    {
        _response = await _client.GetAsync(path);
    }

    [When(@"I GET ""(.*)"" with no redirect")]
    public async Task WhenIGetNoRedirect(string path)
    {
        _response = await _noRedirectClient.GetAsync(path);
    }

    [Then(@"I should receive a JWT token")]
    public async Task ThenIShouldReceiveAJwtToken()
    {
        Assert.Equal(HttpStatusCode.OK, _response?.StatusCode);
        var body = await _response!.Content.ReadFromJsonAsync<JsonElement>();
        _token = body.GetProperty("data").GetProperty("token").GetString();
        Assert.NotNull(_token);
    }

    [Then(@"the token should have 3 parts")]
    public void ThenTheTokenShouldHave3Parts()
    {
        Assert.NotNull(_token);
        Assert.Equal(3, _token!.Split('.').Length);
    }

    [Then(@"the response status should be (\d+)")]
    public void ThenTheResponseStatusShouldBe(int code)
    {
        Assert.Equal(code, (int?)_response?.StatusCode);
    }
}

# Testing Guide

## Running Tests

```bash
dotnet test
```

Run specific project:
```bash
dotnet test tests/DotNetAdmin.Tests/DotNetAdmin.Tests.csproj
```

## Test Structure

```
tests/DotNetAdmin.Tests/
├── CustomWebApplicationFactory.cs   # WebApplicationFactory: SQLite in-memory + test JWT/session config
├── reqnroll.json                    # BDD configuration
├── AuthApiTests.cs                  # HTTP API auth endpoint tests
├── AccessModuleTests.cs             # HTTP API access module tests
├── Integration/
│   ├── RbacTests.cs                 # RBAC DB + service layer tests
│   ├── SettingServiceTests.cs       # SettingService integration tests
│   └── AuthApiEndpointTests.cs      # Auth API endpoint integration tests
├── Features/                        # Reqnroll BDD feature files
│   ├── Auth.feature
│   └── Rbac.feature
└── StepDefinitions/                 # Reqnroll step bindings
    └── AuthSteps.cs
```

## CustomWebApplicationFactory

Uses an in-memory SQLite connection (kept open for the factory lifetime to persist data across DI scopes).
Injects test secrets for JWT + session so auth works without production config.

```csharp
public class MyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MyTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
}
```

## Writing Integration Tests

```csharp
[Fact]
public async Task GetAsync_returns_expected_result()
{
    using var scope = _factory.Services.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IMyService>();
    var result = await service.GetAsync();
    Assert.NotNull(result);
}
```

## BDD with Reqnroll

Add feature files under `Features/` and step definitions under `StepDefinitions/`:

```gherkin
Feature: My Feature
Scenario: Happy path
    Given some precondition
    When I do something
    Then I expect this outcome
```

```csharp
[Binding]
public class MySteps
{
    [Given(@"some precondition")]
    public void GivenSomePrecondition() { ... }
}
```

## Convention Check

Run the full suite including build + convention checks:

```bash
./scripts/check-conventions.sh
```

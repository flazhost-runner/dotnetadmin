# Module Guide

## Creating a New Module

### Option A: dotnet new template

```bash
dotnet new install ./templates/module   # once
dotnet new dotnetadmin-module --ModuleName Blog
```

Generates `IBlogService.cs`, `BlogService.cs`, `BlogController.cs`, `BlogDto.cs` in the current directory.
Move to `src/Modules/Blog/`, then follow steps 3–6 below.

### Option B: Manual

1. Create `src/Modules/{YourModule}/`
2. Add files:
   - `I{YourModule}Service.cs` — interface with method signatures
   - `{YourModule}Service.cs` — implements `I{YourModule}Service`, injects `AppDbContext`
   - `{YourModule}Controller.cs` — see below
3. Register in `src/Core/Extensions/ServiceCollectionExtensions.cs`:
   ```csharp
   services.AddScoped<I{YourModule}Service, {YourModule}Service>();
   ```
4. Create view at `Views/{YourModule}/Index.cshtml` using `@{ Layout = "_AdminLayout"; }`
5. Add at least one test in `tests/DotNetAdmin.Tests/`

### Controller template

```csharp
[Route("admin/v1/yourmodule")]
[Authorize(AuthenticationSchemes = "WebCookie")]
[ServiceFilter(typeof(AdminViewDataFilter))]
[ServiceFilter(typeof(AccessFilterAttribute))]
public class YourModuleController : Controller
{
    private readonly IYourModuleService _service;
    public YourModuleController(IYourModuleService service) => _service = service;

    [HttpGet("", Name = "admin.v1.yourmodule.index")]
    public async Task<IActionResult> Index() { ... }
}
```

## RBAC

Named routes are auto-synced to `permissions` table at startup via `IPermissionSyncService`.
Route name = permission name; HTTP method = permission method; guard = `web` or `api` (prefix).

`AccessFilterAttribute` checks if current user's roles contain a matching permission.
`Administrator` role bypasses all permission checks (no DB rows needed).

## Error Handling

Services must **throw** `AppException`, never return error objects:

```csharp
throw new AppException("Record not found", 404);  // maps to HTTP 404
throw new AppException("Validation failed", 422);
```

`IExceptionHandler` (ErrorHandlingMiddleware) catches these and returns JSON or Razor error views.

## DI Rules

| Lifetime   | Usage                                   |
|------------|-----------------------------------------|
| `AddScoped`    | Services that touch DB per request  |
| `AddSingleton` | Caches (SettingCacheService, etc.)  |
| `AddTransient` | Rarely needed; avoid stateful use   |

Never instantiate services with `new` inside other services — always inject via constructor.

## Path Resolution

Always use `IWebHostEnvironment` for file paths:

```csharp
var path = Path.Combine(_env.WebRootPath, "storage", "myfile.png");
var configPath = Path.Combine(_env.ContentRootPath, "data.json");
```

Never use `Directory.GetCurrentDirectory()` inside modules.

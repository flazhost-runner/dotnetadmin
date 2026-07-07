using DotNetAdmin.Core.Data;
using DotNetAdmin.Core.Extensions;
using DotNetAdmin.Core.Middleware;
using DotNetAdmin.Core.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddDotNetAdminCore(builder.Configuration);

// MVC + Razor Views
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Exception handler (IExceptionHandler implementation)
builder.Services.AddExceptionHandler<ErrorHandlingMiddleware>();
builder.Services.AddProblemDetails();

// Authentication: Cookie (web) + JWT (API) dual-scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "WebCookie";
    options.DefaultChallengeScheme = "WebCookie";
})
.AddCookie("WebCookie", options =>
{
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.Cookie.Name = ".DotNetAdmin.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = ctx =>
    {
        // API paths → 401 JSON; web paths → redirect to login
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsJsonAsync(new { status = false, message = "Unauthorized" });
        }
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.ContentType = "application/json";
            return ctx.Response.WriteAsJsonAsync(new { status = false, message = "Forbidden" });
        }
        ctx.Response.Redirect("/admin/v1/dashboard");
        return Task.CompletedTask;
    };
})
.AddJwtBearer("JwtBearer");  // TokenValidationParameters configured below via IOptions<AppConfig>

builder.Services.AddAuthorization();

// JWT validation parameters resolved lazily from IOptions<AppConfig> so factory config overrides take effect
builder.Services.AddOptions<JwtBearerOptions>("JwtBearer")
    .Configure<IOptions<AppConfig>>((jwtOpts, appConfig) =>
    {
        var secret = appConfig.Value.JwtSecret;
        if (string.IsNullOrWhiteSpace(secret))
            secret = "placeholder-dev-secret-change-in-production-min32";
        jwtOpts.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]  // pin HS256
        };
    });

// Rate limiting (built-in .NET 7+) for auth endpoints — loopback bypassed
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy<string>("auth", ctx =>
    {
        if (ctx.Connection.RemoteIpAddress is { } rip && System.Net.IPAddress.IsLoopback(rip))
            return RateLimitPartition.GetNoLimiter("loopback");
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(15),
            PermitLimit = 10,
            QueueLimit = 0,
        });
    });
    o.AddPolicy<string>("otp", ctx =>
    {
        if (ctx.Connection.RemoteIpAddress is { } rip && System.Net.IPAddress.IsLoopback(rip))
            return RateLimitPartition.GetNoLimiter("loopback");
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            Window = TimeSpan.FromMinutes(15),
            PermitLimit = 5,
            QueueLimit = 0,
        });
    });
    o.RejectionStatusCode = 429;
});

// ── Build app ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline (ORDER MATTERS) ────────────────────────────────────────

// 0. Forwarded headers — MUST run before anything that reads the request scheme
//    (session/antiforgery/auth cookies with SameAsRequest, HTTPS redirects, URL gen).
//    Behind a TLS-terminating reverse proxy (CapRover: browser→HTTPS→proxy→HTTP→app:80),
//    the proxy forwards X-Forwarded-Proto: https. Without this, Request.IsHttps stays
//    false → Secure cookies never get set → web login fails. KnownNetworks/KnownProxies
//    are cleared because the proxy IP is unknown/dynamic in the container network.
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

// 1. Exception handler (must be first to catch all errors)
app.UseExceptionHandler();

// 2. Security headers
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

// 3. Method override BEFORE UseRouting (translates POST+?_method=PUT|DELETE)
app.UseMiddleware<MethodOverrideMiddleware>();

// 3a. CSRF query-param promotion (?_csrf= → X-CSRF-TOKEN header, NodeAdmin standard)
app.UseMiddleware<CsrfQueryMiddleware>();

// 4. Response compression
app.UseResponseCompression();

// 5. Static files (wwwroot) — serve first, no auth needed
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (!app.Environment.IsDevelopment())
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
    }
});

// 5a. Local storage (driver=local): serve uploaded objects at stable prefix /storage/<key>.
//     URL dipisah dari path filesystem — STORAGE_BASE_PATH boleh absolut (mis. /app/storage
//     di container) namun URL render tetap /storage/<key>. Untuk oss/s3 tak ada penyajian lokal
//     (URL absolut ter-presign). Berpindah backend cukup via config, tanpa ubah kode/view.
{
    var storageCfg = app.Services.GetRequiredService<IOptions<StorageConfig>>().Value;
    if (string.Equals(storageCfg.Driver, "local", StringComparison.OrdinalIgnoreCase))
    {
        var baseDir = LocalStorage.ResolveBaseDir(storageCfg.BasePath, app.Environment.ContentRootPath);
        Directory.CreateDirectory(baseDir);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(baseDir),
            RequestPath = LocalStorage.UrlPrefix,
            OnPrepareResponse = ctx =>
            {
                if (!app.Environment.IsDevelopment())
                    ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=604800";
            }
        });
    }
}

// 6. Routing
app.UseRouting();

// 7. Rate limiter
app.UseRateLimiter();

// 8. Session (must be before Auth to restore session cookie)
app.UseSession();

// 9. Authentication + Authorization
app.UseAuthentication();
app.UseAuthorization();

// 10. Map MVC controllers
app.MapControllers();

// ── Dev: auto-migrate + seed ───────────────────────────────────────────────────
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Test")
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
}

// ── Fail-fast on missing production secrets ────────────────────────────────────
if (!app.Environment.IsDevelopment() && app.Environment.EnvironmentName != "Test")
{
    var prodJwtSecret = app.Configuration["App:JwtSecret"];
    var prodSessionSecret = app.Configuration["App:SessionSecret"];
    if (string.IsNullOrWhiteSpace(prodJwtSecret) || string.IsNullOrWhiteSpace(prodSessionSecret))
        throw new InvalidOperationException(
            "App:JwtSecret and App:SessionSecret must be set in production. " +
            "Use 'dotnet user-secrets set' or environment variables.");
}

app.Run();

// Expose Program for WebApplicationFactory in tests
public partial class Program { }

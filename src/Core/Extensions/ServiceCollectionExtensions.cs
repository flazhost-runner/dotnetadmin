using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using DotNetAdmin.Core.Middleware;
using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Auth;
using DotNetAdmin.Modules.Access.User;
using DotNetAdmin.Modules.Access.Role;
using DotNetAdmin.Modules.Access.Permission;
using DotNetAdmin.Modules.Profile;
using DotNetAdmin.Modules.Setting;
using DotNetAdmin.Modules.Media;

namespace DotNetAdmin.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetAdminCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Config options ────────────────────────────────────────────
        services.Configure<AppConfig>(configuration.GetSection("App"));
        services.Configure<DatabaseConfig>(configuration.GetSection("Database"));
        services.Configure<RedisConfig>(configuration.GetSection("Redis"));
        services.Configure<StorageConfig>(configuration.GetSection("Storage"));
        services.Configure<EmailConfig>(configuration.GetSection("Email"));

        // ── Database (multi-dialect) ──────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            DesignTimeDbContextFactory.ConfigureDb(options, configuration));

        // ── Distributed cache (session store) ────────────────────────
        // SESSION_DRIVER: redis | database | memory  (default: redis)
        // - redis    : simpan sesi di Redis; cepat, hilang saat Redis restart.
        // - database : simpan sesi di tabel DB via EF Core (persist lintas restart).
        //              Jalankan migration dulu agar tabel sesi tersedia.
        // - memory   : in-process memory; hanya untuk dev/single-node.
        var sessionDriver = configuration["Session:Driver"] ?? "database";
        if (sessionDriver == "database")
        {
            services.AddDistributedMemoryCache(); // EF Core session via IDistributedCache wrapper
            // TODO: ganti dengan AddDistributedSqlServerCache / custom EF store bila perlu persist penuh
        }
        else if (sessionDriver == "redis")
        {
            var redisUrl = configuration["Redis:Url"] ?? "";
            var redisConnStr = redisUrl.StartsWith("redis://")
                ? redisUrl["redis://".Length..]
                : redisUrl;
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnStr);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // ── Session ───────────────────────────────────────────────────
        var sessionTtlHours = int.TryParse(configuration["App:SessionTtlHours"], out var h) ? h : 6;
        services.AddSession(o =>
        {
            o.Cookie.Name = ".DotNetAdmin.Session";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            o.IdleTimeout = TimeSpan.FromHours(sessionTtlHours);
        });

        // ── Antiforgery: header or query (for DELETE form compat) ─────
        services.AddAntiforgery(o =>
        {
            o.HeaderName = "X-CSRF-TOKEN";
            o.FormFieldName = "_csrf";
        });

        // ── Core services ─────────────────────────────────────────────
        services.AddSingleton<ISettingCacheService, SettingCacheService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionSyncService, PermissionSyncService>();

        // ── Access module services ────────────────────────────────────
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // ── Profile / Setting / Media services ────────────────────────
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddSingleton<IFeCatalogService, FeCatalogService>();
        services.AddScoped<IFeTemplateService, FeTemplateService>();
        services.AddScoped<IMediaService, MediaService>();

        // ── HttpClient for GitHub catalog API ─────────────────────────
        services.AddHttpClient("github", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "DotNetAdmin/1.0");
            c.Timeout = TimeSpan.FromSeconds(15);
        });

        // ── MVC filters ───────────────────────────────────────────────
        services.AddScoped<AdminViewDataFilter>();
        services.AddScoped<AccessFilterAttribute>();

        // ── Compression ───────────────────────────────────────────────
        services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<GzipCompressionProvider>();
            o.Providers.Add<BrotliCompressionProvider>();
        });

        return services;
    }
}

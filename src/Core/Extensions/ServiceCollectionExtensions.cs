using System.Net;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using DotNetAdmin.Core.Middleware;
using DotNetAdmin.Core.Filters;
using DotNetAdmin.Modules.Auth;
using DotNetAdmin.Modules.Access.User;
using DotNetAdmin.Modules.Access.Role;
using DotNetAdmin.Modules.Access.Permission;
using DotNetAdmin.Modules.Profile;
using DotNetAdmin.Modules.Setting;
using DotNetAdmin.Modules.Media;
using DotNetAdmin.Core.Storage;

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
            var redisOptions = BuildRedisOptions(redisUrl);
            services.AddStackExchangeRedisCache(o => o.ConfigurationOptions = redisOptions);
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

        // ── Storage adapter (driver: local | oss | s3) ────────────────
        // Berpindah backend cukup via config STORAGE_DRIVER — tanpa ubah kode/view.
        // DB menyimpan KEY objek; URL render dibangun saat request oleh IStorageService.
        //   local  → file di STORAGE_BASE_PATH, dirender /storage/<key> (static middleware).
        //   oss/s3 → URL absolut ter-presign (TTL).
        services.AddHttpClient("storage");
        services.AddSingleton<IStorageService>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<StorageConfig>>().Value;
            var driver = (cfg.Driver ?? "local").Trim().ToLowerInvariant();
            if (driver == "local")
            {
                var env = sp.GetRequiredService<IWebHostEnvironment>();
                var baseDir = LocalStorage.ResolveBaseDir(cfg.BasePath, env.ContentRootPath);
                return new LocalStorageService(baseDir);
            }

            if (string.IsNullOrWhiteSpace(cfg.AccessKey) || string.IsNullOrWhiteSpace(cfg.SecretKey))
                throw new InvalidOperationException(
                    "Storage belum dikonfigurasi (STORAGE_ACCESS_KEY/STORAGE_SECRET_KEY kosong).");

            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            return driver == "s3"
                ? new S3StorageService(cfg, httpFactory)
                : new OssStorageService(cfg, httpFactory);
        });

        // ── HttpClient for GitHub catalog API ─────────────────────────
        services.AddHttpClient("github", c =>
        {
            c.DefaultRequestHeaders.Add("User-Agent", "DotNetAdmin/1.0");
            // Batas atas; timeout efektif per-panggilan diatur via CancellationToken
            // (preview 8s, download template 15s, tree katalog 20s).
            c.Timeout = TimeSpan.FromSeconds(30);
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

    /// <summary>
    /// Bangun <see cref="ConfigurationOptions"/> dari REDIS_URL (redis:// atau rediss://).
    /// StackExchange.Redis tidak mem-parse skema URL/userinfo secara native, jadi kita
    /// urai manual host:port + password, dan untuk rediss:// (TLS) set Ssl=true beserta
    /// SslHost = host — WAJIB agar TLS SNI terkirim (HAProxy flazhost me-route by SNI;
    /// tanpa SNI koneksi ditutup → crash-loop → 504).
    /// AbortOnConnectFail=false: Redis yang belum siap tidak memblokir startup HTTP.
    /// </summary>
    internal static ConfigurationOptions BuildRedisOptions(string redisUrl)
    {
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false, // jangan crash saat Redis sempat tak tersedia
        };

        if (string.IsNullOrWhiteSpace(redisUrl))
        {
            options.EndPoints.Add("127.0.0.1", 6379);
            return options;
        }

        var isTls = redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);

        // Coba parse sebagai URI (menangani skema, userinfo, host, port).
        if (Uri.TryCreate(redisUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals("redis", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("rediss", StringComparison.OrdinalIgnoreCase)))
        {
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : (isTls ? 6380 : 6379);
            options.EndPoints.Add(host, port);

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':', 2);
                // format: user:password — StackExchange pakai User + Password
                if (parts.Length == 2)
                {
                    if (!string.IsNullOrEmpty(parts[0]) && parts[0] != "default")
                        options.User = parts[0];
                    options.Password = Uri.UnescapeDataString(parts[1]);
                }
                else if (parts.Length == 1 && !string.IsNullOrEmpty(parts[0]))
                {
                    options.Password = Uri.UnescapeDataString(parts[0]);
                }
            }

            if (isTls)
            {
                options.Ssl = true;
                options.SslHost = host; // TLS SNI = host REDIS_URL
            }
        }
        else
        {
            // Bukan URL — perlakukan sebagai connection string StackExchange biasa.
            var raw = redisUrl.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
                ? redisUrl["redis://".Length..]
                : redisUrl;
            var parsed = ConfigurationOptions.Parse(raw);
            parsed.AbortOnConnectFail = false;
            if (parsed.Ssl && string.IsNullOrEmpty(parsed.SslHost))
            {
                // pastikan SNI terkirim bila TLS diaktifkan lewat connection string
                parsed.SslHost = parsed.EndPoints
                    .OfType<DnsEndPoint>()
                    .FirstOrDefault()?.Host;
            }
            return parsed;
        }

        return options;
    }
}

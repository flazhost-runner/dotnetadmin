using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace DotNetAdmin.Core.Services;

/// <summary>
/// Upsert permission dari seluruh route ter-registrasi (idempoten).
///
/// Sumber kebenaran = EndpointDataSource (composite berisi semua endpoint MVC)
/// + nama route dari atribut [HttpGet/Post/...](Name = "...") — nama yang SAMA
/// dipakai AccessFilterAttribute (RouteNameMetadata) saat cek RBAC, sehingga
/// permission yang ter-upsert konsisten dengan mekanisme cek akses.
///
/// guard_name: nama route 'api.*' → 'api', selain itu 'web'
/// (paritas NodeAdmin getAllRegisteredRoute / NestAdmin syncFromRouteRegistry).
/// </summary>
public class PermissionSyncService : IPermissionSyncService
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly AppDbContext _db;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(
        EndpointDataSource endpointDataSource,
        AppDbContext db,
        ILogger<PermissionSyncService> logger)
    {
        _endpointDataSource = endpointDataSource;
        _db = db;
        _logger = logger;
    }

    public async Task SyncAsync()
    {
        var endpoints = _endpointDataSource.Endpoints.OfType<RouteEndpoint>();

        int added = 0;
        foreach (var endpoint in endpoints)
        {
            var routeName = endpoint.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
            if (string.IsNullOrEmpty(routeName)) continue; // hanya route bernama yang dipersist

            var httpMeta = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            if (httpMeta == null || httpMeta.HttpMethods.Count == 0) continue;

            // Jalur auth dari nama route: 'api.*' → guard api, selain itu web.
            var guardName = routeName.StartsWith("api.", StringComparison.OrdinalIgnoreCase) ? "api" : "web";

            foreach (var method in httpMeta.HttpMethods)
            {
                var exists = await _db.Permissions
                    .AnyAsync(p => p.Name == routeName && p.Method == method && p.GuardName == guardName);

                if (!exists)
                {
                    _db.Permissions.Add(new Permission
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = routeName,
                        GuardName = guardName,
                        Method = method,
                        Status = "Active",
                        CreatedBy = "system",
                        UpdatedBy = "system"
                    });
                    added++;
                }
            }
        }

        if (added > 0)
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("Permission sync: {Count} permissions added.", added);
        }
    }
}

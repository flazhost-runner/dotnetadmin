using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace DotNetAdmin.Core.Services;

public class PermissionSyncService : IPermissionSyncService
{
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(ILogger<PermissionSyncService> logger)
    {
        _logger = logger;
    }

    public async Task SyncAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sources = app.Services.GetServices<EndpointDataSource>();
        var endpoints = sources.SelectMany(s => s.Endpoints).OfType<RouteEndpoint>();

        int added = 0;
        foreach (var endpoint in endpoints)
        {
            var routeName = endpoint.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
            if (string.IsNullOrEmpty(routeName)) continue;

            var httpMeta = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            if (httpMeta == null || httpMeta.HttpMethods.Count == 0) continue;

            var guardName = routeName.StartsWith("api.", StringComparison.OrdinalIgnoreCase) ? "api" : "web";

            foreach (var method in httpMeta.HttpMethods)
            {
                var exists = await db.Permissions
                    .AnyAsync(p => p.Name == routeName && p.Method == method);

                if (!exists)
                {
                    db.Permissions.Add(new Permission
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
            await db.SaveChangesAsync();
            _logger.LogInformation("Permission sync: {Count} permissions added.", added);
        }
    }
}

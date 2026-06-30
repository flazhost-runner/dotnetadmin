namespace DotNetAdmin.Core.Services;

public interface IPermissionSyncService
{
    Task SyncAsync(WebApplication app);
}

namespace DotNetAdmin.Core.Services;

/// <summary>
/// Auto-discover permission dari route ter-registrasi (idempoten) — pola
/// NodeAdmin PermissionService.getAllRegisteredRoute / NestAdmin
/// syncFromRouteRegistry. Dipanggil saat halaman Permission dibuka.
/// </summary>
public interface IPermissionSyncService
{
    Task SyncAsync();
}

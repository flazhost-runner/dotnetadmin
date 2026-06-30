using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DotNetAdmin.Core.Data;

namespace DotNetAdmin.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep the SQLite connection open for the lifetime of the factory
    // so the in-memory database persists across multiple DI scopes.
    private readonly SqliteConnection _connection =
        new SqliteConnection("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment("Test");

        // Inject test secrets so JWT works without production config
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:JwtSecret"] = "test-jwt-secret-at-least-32-chars-long-xxxxx",
                ["App:SessionSecret"] = "test-session-secret"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove real DB registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // All contexts share the same persistent in-memory connection
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

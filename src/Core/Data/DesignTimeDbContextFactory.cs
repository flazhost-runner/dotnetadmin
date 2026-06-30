using Microsoft.EntityFrameworkCore.Design;

namespace DotNetAdmin.Core.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        ConfigureDb(optionsBuilder, config);

        return new AppDbContext(optionsBuilder.Options);
    }

    internal static void ConfigureDb(DbContextOptionsBuilder options, IConfiguration config)
    {
        var dbType = config["Database:Type"] ?? "sqlite";
        switch (dbType.ToLowerInvariant())
        {
            case "mysql":
                var mysqlConn = config["Database:MySql"]!;
                options.UseMySql(mysqlConn, ServerVersion.AutoDetect(mysqlConn));
                break;
            case "postgres":
                options.UseNpgsql(config["Database:Postgres"]!);
                break;
            default:
                options.UseSqlite(config["Database:ConnectionString"] ?? "Data Source=dotnetadmin.db");
                break;
        }
    }
}

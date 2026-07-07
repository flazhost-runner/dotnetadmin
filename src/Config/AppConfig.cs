namespace DotNetAdmin.Config;

public class AppConfig
{
    public string Name { get; set; } = "DotNetAdmin";
    public string Mode { get; set; } = "full";
    public string Url { get; set; } = "http://localhost:5000";
    public string SessionSecret { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
    public string JwtExpiresIn { get; set; } = "1h";
    public int BcryptRounds { get; set; } = 10;
    public int OtpExpiryMinutes { get; set; } = 10;
    public int DefaultPageSize { get; set; } = 10;
    public int SessionTtlHours { get; set; } = 6;
    public string[] AllowedOrigins { get; set; } = [];
}

public class DatabaseConfig
{
    public string Type { get; set; } = "sqlite";
    public string ConnectionString { get; set; } = "Data Source=dotnetadmin.db";
    public string MySql { get; set; } = string.Empty;
    public string Postgres { get; set; } = string.Empty;
    public DatabasePoolConfig Pool { get; set; } = new();
}

public class DatabasePoolConfig
{
    public int Min { get; set; } = 2;
    public int Max { get; set; } = 10;
}

public class RedisConfig
{
    public string Url { get; set; } = "redis://127.0.0.1:6379";
}

public class StorageConfig
{
    public string Driver { get; set; } = "local";
    public string BasePath { get; set; } = "storage/uploads";
    public string EditorFolder { get; set; } = "editor";
    public string Endpoint { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool Ssl { get; set; } = false;
}

public class EmailConfig
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "noreply@dotnetadmin.com";
    public bool Secure { get; set; } = false;
    public string FromName { get; set; } = "NodeAdmin";
}

using System;
using System.Net.Http;
using DotNetAdmin.Config;
using DotNetAdmin.Core.Storage;
using Xunit;

namespace DotNetAdmin.Tests;

/// <summary>
/// Mengunci kontrak render-URL adapter storage: DB simpan key, URL dibangun saat
/// request per driver. Local → /storage/&lt;key&gt;; oss/s3 → URL absolut ter-presign.
/// Berpindah backend cukup ganti driver — tanpa ubah kode/view.
/// </summary>
public class StorageServiceTests
{
    // IHttpClientFactory minimal — Url() tidak memakainya, hanya untuk konstruktor.
    private sealed class StubHttpFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    [Fact]
    public void Local_Url_BuildsStablePrefixFromKey()
    {
        var svc = new LocalStorageService("/var/data/storage");
        Assert.Equal("/storage/editor/a.png", svc.Url("editor/a.png"));
        // Absolut base path tetap menghasilkan URL valid (dipisah dari path fs).
        Assert.Equal("/storage/profile/x.png", svc.Url("profile/x.png"));
    }

    [Fact]
    public void Local_Url_PassesThroughAbsoluteValues()
    {
        var svc = new LocalStorageService("/var/data/storage");
        Assert.Equal("/be/default/avatar.png", svc.Url("/be/default/avatar.png"));
        Assert.Equal("https://cdn.example.com/x.png", svc.Url("https://cdn.example.com/x.png"));
        Assert.Equal("", svc.Url(null));
    }

    [Fact]
    public void S3_Url_IsAbsolutePresignedWithTtl()
    {
        var cfg = new StorageConfig
        {
            Driver = "s3", Bucket = "mybucket", Region = "ap-southeast-1",
            AccessKey = "AKIAEXAMPLE", SecretKey = "secretkey", Ssl = true,
        };
        var url = new S3StorageService(cfg, new StubHttpFactory()).Url("editor/a.png", 3600);

        Assert.StartsWith("https://mybucket.s3.ap-southeast-1.amazonaws.com/editor/a.png?", url);
        Assert.Contains("X-Amz-Algorithm=AWS4-HMAC-SHA256", url);
        Assert.Contains("X-Amz-Expires=3600", url);
        Assert.Contains("X-Amz-Signature=", url);
        Assert.Contains("X-Amz-Credential=AKIAEXAMPLE", url);
    }

    [Fact]
    public void S3_Url_UsesPathStyleWhenEndpointSet()
    {
        var cfg = new StorageConfig
        {
            Driver = "s3", Bucket = "mybucket", Region = "us-east-1",
            Endpoint = "https://minio.local:9000", AccessKey = "k", SecretKey = "s", Ssl = false,
        };
        var url = new S3StorageService(cfg, new StubHttpFactory()).Url("editor/a.png");
        Assert.StartsWith("http://minio.local:9000/mybucket/editor/a.png?", url);
    }

    [Fact]
    public void Oss_Url_IsAbsolutePresigned()
    {
        var cfg = new StorageConfig
        {
            Driver = "oss", Bucket = "mybucket", Endpoint = "oss-ap-southeast-5.aliyuncs.com",
            AccessKey = "LTAIEXAMPLE", SecretKey = "secretkey", Ssl = true,
        };
        var url = new OssStorageService(cfg, new StubHttpFactory()).Url("profile/a.png", 3600);

        Assert.StartsWith("https://mybucket.oss-ap-southeast-5.aliyuncs.com/profile/a.png?", url);
        Assert.Contains("OSSAccessKeyId=LTAIEXAMPLE", url);
        Assert.Contains("Expires=", url);
        Assert.Contains("Signature=", url);
    }

    [Fact]
    public void ObjectDrivers_PassThroughAbsoluteUrls()
    {
        var cfg = new StorageConfig { Bucket = "b", AccessKey = "k", SecretKey = "s" };
        var s3 = new S3StorageService(cfg, new StubHttpFactory());
        var oss = new OssStorageService(cfg, new StubHttpFactory());
        Assert.Equal("https://cdn/x.png", s3.Url("https://cdn/x.png"));
        Assert.Equal("https://cdn/x.png", oss.Url("https://cdn/x.png"));
        Assert.Equal("", s3.Url(null));
    }
}

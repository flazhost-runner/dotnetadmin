# DotNetAdmin

ASP.NET Core 10 LTS admin panel — a 1:1 concept port of NodeAdmin (Express/TypeScript/TypeORM) using native .NET idioms.

## Architecture

Vertical Slice / Feature Folder (`src/Modules/{Module}/`)

- **EF Core 10** — SQLite (dev/test) / MySQL via Pomelo / Postgres via Npgsql
- **Dual Auth** — Cookie (`WebCookie` scheme, web sessions) + JWT Bearer (API)
- **RBAC** — Route-driven permissions auto-synced from endpoint metadata at startup
- **Razor Views** + Tailwind CDN (1:1 NodeAdmin admin chrome)
- **DI** — all services `AddScoped`/`AddSingleton`, never `new`

## Quick Start

```bash
cd /home/mulyawan/Project/Admin/DotNetAdmin
dotnet run
```

Pertama kali jalan: migrasi + seed otomatis. Buka browser:

| URL | Halaman |
|-----|---------|
| `http://localhost:5000` | Landing page |
| `http://localhost:5000/auth/login` | Login admin |
| `http://localhost:5000/admin/v1/dashboard` | Dashboard |

**Kredensial default:**
```
Email    : admin@admin.com
Password : Admin1234!
```

**Hot-reload (development):**
```bash
dotnet watch run
```

**Ganti port:**
```bash
dotnet run --urls "http://localhost:5001"
```

**Reset database:**
```bash
rm dotnetadmin.db
dotnet run   # migrate + seed ulang otomatis
```

## Testing

```bash
dotnet test
```

## API Collection (Postman)

Import `docs/postman/DotNetAdmin.postman_collection.json` into Postman to exercise the REST API.

- `base_url` collection variable defaults to `http://localhost:5000` (the app's default HTTP URL — see Quick Start).
- Set the `access_token` variable after logging in via `POST /api/v1/auth/login` to authorize the protected requests.

## Convention Check

```bash
./scripts/check-conventions.sh
```

Verifies: build 0 errors, tests green, interface naming, no raw path access in modules.

## Scaffold a New Module

```bash
# Install the local template (once)
dotnet new install ./templates/module

# Generate a new module
dotnet new dotnetadmin-module --ModuleName Blog
# → creates IBlogService.cs, BlogService.cs, BlogController.cs, BlogDto.cs
# → register in ServiceCollectionExtensions.cs + create Views/Blog/Index.cshtml
```

## Modules

| Module       | Web Routes                      | API Routes                     |
|--------------|---------------------------------|--------------------------------|
| Auth         | `/auth/login`, `/auth/register` | `/api/v1/auth/*`               |
| Dashboard    | `/admin/v1/dashboard`           | —                              |
| Users        | `/admin/v1/access/user/*`       | `/api/v1/access/user/*`        |
| Roles        | `/admin/v1/access/role/*`       | `/api/v1/access/role/*`        |
| Permissions  | `/admin/v1/access/permission/*` | `/api/v1/access/permission/*`  |
| Profile      | `/admin/v1/profile`             | —                              |
| Setting      | `/admin/v1/setting`             | —                              |
| Components   | `/admin/v1/components`          | —                              |
| Media        | `/admin/v1/media/*`             | —                              |
| Home/Landing | `/`, `/home`                    | —                              |

## DB Schema (canonical)

Tables: `users`, `roles`, `permissions`, `settings`, `users_roles`, `roles_permissions`

- `id` = `varchar(36)` UUID
- `description` column = `desc` via Fluent API (SQL reserved word pinned)
- `permissions.name` is NOT unique (same route name can exist for `web` and `api` guard)
- `status` = `varchar(20)` not ENUM (portable across dialects)

## Storage & switching backends

File uploads (profile pictures, media/editor images) go through a pluggable storage
adapter (`IStorageService`) that mirrors NodeAdmin. **The database stores the object
_key_ (e.g. `profile/ab12.png`) — never a URL.** The render URL is built _at request
time_ by the active driver, so switching backends is a **config-only change (+ restart)**
— no code or view edits.

| Driver  | `Storage:Driver` | Where files live                          | Rendered URL                         |
|---------|------------------|-------------------------------------------|--------------------------------------|
| `local` | `local`          | `Storage:BasePath` on the local disk      | `/storage/<key>` (stable, relative)  |
| `oss`   | `oss`            | Alibaba Cloud OSS bucket                   | absolute **presigned** URL (TTL)     |
| `s3`    | `s3`             | AWS S3 / MinIO / R2 / B2 bucket            | absolute **presigned** URL (TTL)     |

For `local`, a static-file middleware serves `Storage:BasePath` at the stable prefix
`/storage` (registered only when the driver is `local`, in `Program.cs`). The URL prefix
is **decoupled** from the filesystem path, so an absolute `Storage:BasePath` (e.g.
`/app/storage` in a container) still renders as `/storage/<key>` — not `//app/...`.
For `oss`/`s3` there is no local serving; `IStorageService.Url(key)` returns an absolute
presigned URL valid for a limited TTL.

### Switch the backend

Edit config (env var `Storage__Driver` or `appsettings.json` `Storage:Driver`) and restart:

```bash
# Local (default)
Storage__Driver=local
Storage__BasePath=storage/uploads          # relative to ContentRoot, or absolute

# S3 / S3-compatible (AWS, MinIO, Cloudflare R2, Backblaze B2)
Storage__Driver=s3
Storage__Bucket=your-bucket
Storage__Region=us-east-1
Storage__AccessKey=...  Storage__SecretKey=...  Storage__Ssl=true
# Storage__Endpoint=minio.local:9000        # set for non-AWS (path-style); leave empty for AWS

# Alibaba Cloud OSS
Storage__Driver=oss
Storage__Bucket=your-bucket
Storage__Endpoint=oss-ap-southeast-5.aliyuncs.com
Storage__AccessKey=...  Storage__SecretKey=...  Storage__Ssl=true
```

See `.env.example` for the full annotated list. Because the DB stores keys, existing
records keep working after a switch **as long as the same keys exist in the new backend**.

### Migrating existing files when you switch

Copy the objects under `Storage:BasePath` into the target bucket, preserving key paths:

```bash
# → S3 (or S3-compatible)
aws s3 sync ./storage/uploads/ s3://your-bucket/

# → Alibaba OSS
ossutil cp -r ./storage/uploads/ oss://your-bucket/
```

### Deployment caveats

- **Uploads are git-ignored** — `storage/uploads/` contents are excluded (`.gitignore`),
  only `storage/uploads/.gitkeep` is committed to preserve the directory.
- **`local` in production is ephemeral** — on containers/PaaS the local disk is wiped on
  every redeploy/restart. For a persistent `local` backend, mount a **persistent volume**
  at `Storage:BasePath`; otherwise use `oss`/`s3`.

## Config

```json
{
  "Database": { "Type": "sqlite", "Connection": "Data Source=dotnetadmin_dev.db" },
  "App": { "JwtSecret": "...", "SessionSecret": "..." }
}
```

For production set via environment variables or `dotnet user-secrets`.

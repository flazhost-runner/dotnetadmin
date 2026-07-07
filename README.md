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

## Config

```json
{
  "Database": { "Type": "sqlite", "Connection": "Data Source=dotnetadmin_dev.db" },
  "App": { "JwtSecret": "...", "SessionSecret": "..." }
}
```

For production set via environment variables or `dotnet user-secrets`.

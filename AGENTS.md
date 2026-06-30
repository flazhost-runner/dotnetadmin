# AGENTS.md — Aturan Pengembangan DotNetAdmin (untuk AI & developer)

> **Sumber kebenaran tunggal** untuk DotNetAdmin (ASP.NET Core .NET 10 LTS — port konsep NodeAdmin).
> Setiap AI (Claude Code, Cursor, Copilot) dan developer WAJIB mengikuti dokumen ini.
> Acuan NodeAdmin: `/home/mulyawan/Project/Admin/NodeAdmin/AGENTS.md` + `docs/PORTING_GUIDE.md`.

---

## Alur Wajib (Request Lifecycle)

```
Route (named via [HttpGet("...")] + attr routing)
  → Middleware: AuthenticationMiddleware → AuthorizationMiddleware → CSRF (IAntiforgery)
  → Controller (@Authorize → AccessFilter) — tipis: parsing req, panggil service, return result
  → Service (IXService → XService) — logika bisnis; throw AppException bila gagal
  → EF Core Repository (AppDbContext) — query via LINQ + CiLikeHelper
  → DB (SQLite dev / MySQL / Postgres)
  ↘ error AppException → ErrorHandlingMiddleware (terpusat)
```

## Prinsip Wajib

1. **SOLID / DI.** Service & controller di-inject via constructor dari `IServiceCollection`.
   Service `implements IXService`, didaftarkan `AddScoped<IXService, XService>()` di `XModule.RegisterServices()`.
   **DILARANG** `new XService()` di controller/route.

2. **DRY.** Pakai helper yang ADA:
   - `PaginationHelper.PaginateAsync<T>(query, page, pageSize)` — semua list paginated
   - `CiLikeHelper.WhereCiLike<T>(query, x => x.Field, term)` — search case-insensitive lintas DB
   - `OtpHelper.GenerateOtp/HashOtp/VerifyOtp/OtpExpiresAt` — OTP aman
   - `ResponseHelper.Success/Error` — response API seragam
   - `ThemeConfig.GetTheme(name)` — palet tema
   - `ISettingCacheService.GetSettingAsync()` — setting singleton (TTL 60s)
   Jangan tulis ulang.

3. **Error handling.** Service **`throw AppException`** (atau subclass):
   `NotFoundAppException`, `ConflictAppException`, `ValidationAppException`, `UnauthorizedAppException`, `ForbiddenAppException`.
   Controller TIDAK menangkap error manual — `ErrorHandlingMiddleware` yang menangani.
   **DILARANG** `return error` / `catch` tanpa re-throw di controller.

4. **Separation of Concerns.** Controller ≠ Service ≠ DbContext ≠ View.
   Logika bisnis HANYA di service. Controller hanya: parse request → panggil service → return View/Json.

5. **Config terpusat.** Akses config HANYA via `IOptions<AppConfig>`, `IOptions<DatabaseConfig>`, dll.
   **DILARANG** `Environment.GetEnvironmentVariable()` langsung di modul.

6. **Portabilitas DB.** Entity gunakan tipe kolom abstrak (`varchar`, `text`, `bigint`, `boolean`, `timestamp`).
   **DILARANG** tipe vendor (`longtext`, `mediumtext`, `datetime` MySQL-only).
   **DILARANG** raw SQL (`db.Database.ExecuteSqlRaw()`) di modul — gunakan LINQ + EF Core.
   Semua nama tabel/kolom/join di-**PIN eksplisit** via Fluent API (`ToTable`, `HasColumnName`) — jangan andalkan konvensi EF.

7. **Method override.** Form web hanya POST/GET → gunakan `?_method=PUT|DELETE`.
   `MethodOverrideMiddleware` sudah terpasang SEBELUM `UseRouting()`.
   Handler: `[HttpPut]` / `[HttpDelete]` apa adanya.

8. **CSRF.** Antiforgery token dibaca dari header `X-CSRF-TOKEN` ATAU query `?_csrf`.
   Form web: inject via `@Html.AntiForgeryToken()` atau baca dari `IAntiforgery`.
   DELETE form: token di query karena form-urlencoded body tak terbaca setelah method override.

9. **Path aset/file.** SELALU gunakan `IWebHostEnvironment.ContentRootPath`/`WebRootPath` + `Path.Combine`.
   **DILARANG** `Directory.GetCurrentDirectory()` mentah — panic saat dijalankan dari folder lain.

10. **Auth redirect.**
    - Web route belum login → redirect `/auth/login` (sudah dikonfigurasi di `OnRedirectToLogin`).
    - API `/api/**` belum login → **401 JSON** (bukan redirect).
    - 403 web → redirect `/admin/v1/dashboard`.

---

## Skema DB Kanonik (WAJIB identik dengan NodeAdmin)

Tabel: `users`, `roles`, `permissions`, `settings`, `users_roles`, `roles_permissions`.
- `id` = `varchar(36)` UUID string (bukan auto-increment, bukan uuid native).
- `desc` di `roles`/`permissions` = reserved word → `HasColumnName("desc")` (sudah di AppDbContext).
- `permissions.name` = NON-unique (hanya diindex).
- Join: `users_roles(user_id, role_id)`, `roles_permissions(role_id, permission_id)` — komposit PK.
- Semua tabel punya: `created_by`, `updated_by`, `created_at`, `updated_at`.

Lihat `src/Core/Data/AppDbContext.cs` untuk konfigurasi Fluent API lengkap.

---

## RBAC Route-Driven

Permission = `(name, method, guard_name)`. Nama permission = nama named-route ASP.NET Core.
- `guard_name`: nama route diawali `api.` → `"api"`, lainnya → `"web"`.
- Permission **di-scan otomatis dari route registry** (`IActionDescriptorCollectionProvider`) saat halaman Permission dibuka atau aplikasi start — **TIDAK hardcoded**.
- `AccessFilter` (IAsyncAuthorizationFilter) dipasang **tanpa argumen** — menurunkan `(routeName, method)` dari `ActionDescriptor.AttributeRouteInfo.Name` → cocokkan permission `name`+`method` dari role user.
- Role `Administrator` bypass semua pengecekan.
- Urutan: `[Authorize]` (autentikasi) → `AccessFilter` (RBAC) — jangan dibalik.

Named route format: `admin.v1.access.user.index`, `api.v1.access.role.store`, dll.
Selalu set `Name = "..."` di attribute routing:
```csharp
[HttpGet("/admin/v1/access/user", Name = "admin.v1.access.user.index")]
```

---

## Checklist Membuat Modul Baru

Ikuti `docs/MODULE_GUIDE.md`. Urutan & file wajib:

1. **Entity** `src/Core/Data/Entities/X.cs` — tipe portabel, Fluent API di `AppDbContext.OnModelCreating`.
2. **Migration** `dotnet ef migrations add CreateXTable` — setelah update `AppDbContext`.
3. **Interface** `src/Modules/X/Services/IXService.cs`.
4. **Service** `src/Modules/X/Services/XService.cs` — `IXService` impl, constructor injection `AppDbContext`.
5. **Controller** `src/Modules/X/Controllers/XController.cs` — `[Authorize]` + `AccessFilter`, inject `IXService`.
6. **Validator** `src/Modules/X/Validators/XValidator.cs` — FluentValidation `AbstractValidator<XDto>`.
7. **Routes** — attribute routing di controller dengan `Name = "admin.v1.x.{aksi}"`.
8. **Views** `Views/X/{Index,Create,Edit}.cshtml` — Razor, ikuti pola chrome admin (`_AdminLayout`).
9. **Service registration** — tambah `services.AddScoped<IXService, XService>()` di `Program.cs` atau `ServiceCollectionExtensions`.
10. **Test** `tests/DotNetAdmin.Tests/Modules/X/` — xUnit + WebApplicationFactory.
11. **Docs** — update `README.md`, `docs/API.md` bila ada API.

---

## DO NOT (akan ditolak checker / CI)

- ❌ `new XService()` / `new XController()` di controller → inject via constructor.
- ❌ `return error` / `throw new Exception()` biasa → pakai `throw new AppException(...)`.
- ❌ `Directory.GetCurrentDirectory()` untuk path file → `IWebHostEnvironment.ContentRootPath`.
- ❌ Raw SQL di modul (`ExecuteSqlRaw`, `FromSqlRaw`) → EF Core LINQ.
- ❌ `Environment.GetEnvironmentVariable()` di modul → `IOptions<AppConfig>`.
- ❌ Tipe kolom vendor (`longtext`, `datetime`, `mediumtext`) di entity.
- ❌ ORM auto-naming kolom/tabel — selalu pin via Fluent API.
- ❌ Modul baru tanpa test & tanpa update docs.
- ❌ Hardcode secret/kredensial.
- ❌ `[Authorize(Roles = "...")]` hardcoded — gunakan `AccessFilter` dinamis.

---

## Definition of Done (modul/fitur)

- [ ] Mengikuti checklist & pola di atas.
- [ ] `dotnet build` → 0 error.
- [ ] `dotnet test` → hijau (+ test baru untuk fitur).
- [ ] Convention checker lolos (saat tersedia).
- [ ] Security checklist terpenuhi (auth, CSRF, rate-limit sesuai).
- [ ] `README.md` + `docs/API.md` diperbarui.

---

## Perintah Penting

```bash
dotnet build                                    # build (wajib 0 error)
dotnet run                                      # jalankan dev (auto-migrate + seed)
dotnet test                                     # semua xUnit test
dotnet ef migrations add <NamaMigration>        # buat migration baru
dotnet ef database update                       # terapkan migration manual
# DOTNET_ROOT perlu di-set bila dotnet dari snap:
export DOTNET_ROOT=/var/snap/dotnet/common/dotnet
export PATH="$PATH:/home/mulyawan/.dotnet/tools"
```

---

## Struktur Direktori

```
DotNetAdmin/
├── Program.cs                    # minimal-hosting entry point
├── GlobalUsings.cs               # global using statements
├── DotNetAdmin.csproj
├── AGENTS.md                     # ← ini
├── appsettings.json
├── appsettings.Development.json
├── .editorconfig
├── .gitignore
├── src/
│   ├── Config/
│   │   └── AppConfig.cs          # IOptions<T> config classes
│   └── Core/
│       ├── Auth/                 # IJwtService + JwtService
│       ├── Data/
│       │   ├── Entities/         # BaseEntity, User, Role, Permission, Setting, UserRole, RolePermission
│       │   ├── Migrations/       # EF Core migrations
│       │   ├── AppDbContext.cs   # Fluent API config (semua nama tabel/kolom di-PIN)
│       │   ├── DbSeeder.cs       # idempotent seed
│       │   └── DesignTimeDbContextFactory.cs
│       ├── Errors/               # AppException hierarchy
│       ├── Extensions/           # ServiceCollectionExtensions
│       ├── Filters/              # AccessFilter (RBAC)
│       ├── Helpers/              # PaginationHelper, CiLikeHelper, ResponseHelper, OtpHelper
│       ├── Middleware/           # MethodOverrideMiddleware, ErrorHandlingMiddleware
│       ├── Services/             # ISettingCacheService + SettingCacheService
│       └── Themes/               # ThemeConfig (9 palet)
│   └── Modules/
│       ├── Auth/                 # login, register, logout, OTP reset
│       ├── Access/
│       │   ├── User/             # CRUD user
│       │   ├── Role/             # CRUD role + assign permission
│       │   └── Permission/       # CRUD permission (auto-scan dari route)
│       ├── Dashboard/
│       ├── Setting/              # theme switcher + FE template switcher
│       ├── Profile/
│       ├── Components/           # showcase UI
│       ├── Media/                # file manager (Trumbowyg)
│       └── Home/                 # landing publik
├── Views/
│   ├── Shared/
│   │   ├── _AdminLayout.cshtml   # chrome admin (sidebar + topbar + foot)
│   │   ├── _AdminHead.cshtml
│   │   ├── _AdminSidebar.cshtml
│   │   ├── _AdminTopbar.cshtml
│   │   ├── _AdminFoot.cshtml
│   │   └── _AuthLayout.cshtml    # layout auth (full-width, tanpa sidebar)
│   └── {Module}/
│       └── {Action}.cshtml
├── wwwroot/
│   ├── be/default/               # admin assets (CSS, JS, vendor)
│   └── fe/default/               # landing assets
└── tests/
    ├── DotNetAdmin.Tests/        # xUnit + WebApplicationFactory
    └── DotNetAdmin.BDD/          # Reqnroll (BDD)
```

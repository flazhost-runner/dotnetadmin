namespace DotNetAdmin.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── User ─────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("varchar(36)");
            e.Property(x => x.Code).HasColumnName("code").HasColumnType("varchar(20)").IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(50)").IsRequired();
            e.Property(x => x.Phone).HasColumnName("phone").HasColumnType("varchar(15)");
            e.Property(x => x.Email).HasColumnName("email").HasColumnType("varchar(255)").IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at");
            e.Property(x => x.Password).HasColumnName("password").HasColumnType("varchar(255)").IsRequired();
            e.Property(x => x.PasswordOtp).HasColumnName("password_otp").HasColumnType("varchar(255)");
            e.Property(x => x.PasswordOtpExpires).HasColumnName("password_otp_expires");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").HasDefaultValue("Active");
            e.Property(x => x.Picture).HasColumnName("picture").HasColumnType("varchar(255)");
            e.Property(x => x.Blocked).HasColumnName("blocked").HasDefaultValue(false);
            e.Property(x => x.BlockedReason).HasColumnName("blocked_reason").HasColumnType("varchar(255)");
            e.Property(x => x.Timezone).HasColumnName("timezone").HasColumnType("varchar(255)").HasDefaultValue("UTC");
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(36)");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("varchar(36)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Role ──────────────────────────────────────────────────────
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("varchar(36)");
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(255)").IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.GuardName).HasColumnName("guard_name").HasColumnType("varchar(20)").HasDefaultValue("web");
            e.HasIndex(x => x.GuardName);
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").HasDefaultValue("Active");
            e.Property(x => x.Desc).HasColumnName("desc").HasColumnType("varchar(255)");  // reserved word — explicit pin
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(36)");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("varchar(36)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Permission ────────────────────────────────────────────────
        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("varchar(36)");
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(255)").IsRequired();
            e.HasIndex(x => x.Name);  // indexed but NON-unique
            e.Property(x => x.GuardName).HasColumnName("guard_name").HasColumnType("varchar(20)").HasDefaultValue("web");
            e.HasIndex(x => x.GuardName);
            e.Property(x => x.Method).HasColumnName("method").HasColumnType("varchar(255)");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(20)").HasDefaultValue("Active");
            e.Property(x => x.Desc).HasColumnName("desc").HasColumnType("varchar(255)");  // reserved word
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(36)");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("varchar(36)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── Setting ───────────────────────────────────────────────────
        modelBuilder.Entity<Setting>(e =>
        {
            e.ToTable("settings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("varchar(36)");
            e.Property(x => x.Initial).HasColumnName("initial").HasColumnType("varchar(255)");
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(255)");
            e.Property(x => x.Description).HasColumnName("description").HasColumnType("text");
            e.Property(x => x.Icon).HasColumnName("icon").HasColumnType("varchar(255)");
            e.Property(x => x.Logo).HasColumnName("logo").HasColumnType("varchar(255)");
            e.Property(x => x.Favicon).HasColumnName("favicon").HasColumnType("varchar(255)");
            e.Property(x => x.LoginImage).HasColumnName("login_image").HasColumnType("varchar(255)");
            e.Property(x => x.Phone).HasColumnName("phone").HasColumnType("varchar(255)");
            e.Property(x => x.Address).HasColumnName("address").HasColumnType("varchar(255)");
            e.Property(x => x.Email).HasColumnName("email").HasColumnType("varchar(255)");
            e.Property(x => x.Copyright).HasColumnName("copyright").HasColumnType("varchar(255)");
            e.Property(x => x.Theme).HasColumnName("theme").HasColumnType("varchar(20)").HasDefaultValue("Blue");
            e.Property(x => x.FeTemplate).HasColumnName("fe_template").HasColumnType("varchar(80)")
                .HasDefaultValue("agency-consulting-002-creative-agency");
            e.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("varchar(36)");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("varchar(36)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        // ── users_roles join table ────────────────────────────────────
        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("users_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("varchar(36)");
            e.Property(x => x.RoleId).HasColumnName("role_id").HasColumnType("varchar(36)");
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });

        // ── roles_permissions join table ──────────────────────────────
        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("roles_permissions");
            e.HasKey(x => new { x.RoleId, x.PermissionId });
            e.Property(x => x.RoleId).HasColumnName("role_id").HasColumnType("varchar(36)");
            e.Property(x => x.PermissionId).HasColumnName("permission_id").HasColumnType("varchar(36)");
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);
        });
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = entry.Entity.UpdatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}

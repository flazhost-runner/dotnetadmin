using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetAdmin.src.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    guard_name = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "web"),
                    method = table.Column<string>(type: "varchar(255)", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Active"),
                    desc = table.Column<string>(type: "varchar(255)", nullable: true),
                    created_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", nullable: false),
                    name = table.Column<string>(type: "varchar(255)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Active"),
                    desc = table.Column<string>(type: "varchar(255)", nullable: true),
                    created_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", nullable: false),
                    initial = table.Column<string>(type: "varchar(255)", nullable: true),
                    name = table.Column<string>(type: "varchar(255)", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon = table.Column<string>(type: "varchar(255)", nullable: true),
                    logo = table.Column<string>(type: "varchar(255)", nullable: true),
                    login_image = table.Column<string>(type: "varchar(255)", nullable: true),
                    phone = table.Column<string>(type: "varchar(255)", nullable: true),
                    address = table.Column<string>(type: "varchar(255)", nullable: true),
                    email = table.Column<string>(type: "varchar(255)", nullable: true),
                    copyright = table.Column<string>(type: "varchar(255)", nullable: true),
                    theme = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Blue"),
                    fe_template = table.Column<string>(type: "varchar(80)", nullable: false, defaultValue: "agency-consulting-002-creative-agency"),
                    created_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", nullable: false),
                    code = table.Column<string>(type: "varchar(20)", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", nullable: false),
                    phone = table.Column<string>(type: "varchar(15)", nullable: true),
                    email = table.Column<string>(type: "varchar(255)", nullable: false),
                    email_verified_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    password = table.Column<string>(type: "varchar(255)", nullable: false),
                    password_otp = table.Column<string>(type: "varchar(255)", nullable: true),
                    password_otp_expires = table.Column<long>(type: "INTEGER", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Active"),
                    picture = table.Column<string>(type: "varchar(255)", nullable: true),
                    blocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    blocked_reason = table.Column<string>(type: "varchar(255)", nullable: true),
                    timezone = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: "UTC"),
                    created_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(36)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles_permissions",
                columns: table => new
                {
                    role_id = table.Column<string>(type: "varchar(36)", nullable: false),
                    permission_id = table.Column<string>(type: "varchar(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_roles_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_roles_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users_roles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "varchar(36)", nullable: false),
                    role_id = table.Column<string>(type: "varchar(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_users_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_users_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_guard_name",
                table: "permissions",
                column: "guard_name");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_name",
                table: "permissions",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_permissions_permission_id",
                table: "roles_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_code",
                table: "users",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_roles_role_id",
                table: "users_roles",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "roles_permissions");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "users_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

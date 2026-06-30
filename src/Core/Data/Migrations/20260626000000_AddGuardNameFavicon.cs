using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetAdmin.src.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuardNameFavicon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "guard_name",
                table: "roles",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "web");

            migrationBuilder.CreateIndex(
                name: "IX_roles_guard_name",
                table: "roles",
                column: "guard_name");

            migrationBuilder.AddColumn<string>(
                name: "favicon",
                table: "settings",
                type: "varchar(255)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "guard_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_guard_name",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "favicon",
                table: "settings");
        }
    }
}

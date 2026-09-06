using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class RolePermissionsAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanAccessDesktop",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageHalls",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageMovies",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageNews",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageProjections",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageReferenceData",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageRoles",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageUsers",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanUseChatBot",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewAnalytics",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Roles",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#64748B");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CanAccessDesktop", "CanManageHalls", "CanManageMovies", "CanManageNews", "CanManageProjections", "CanManageReferenceData", "CanManageRoles", "CanManageUsers", "CanUseChatBot", "CanViewAnalytics", "Color" },
                values: new object[] { true, true, true, true, true, true, true, true, true, true, "#7C3AED" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CanAccessDesktop", "CanManageHalls", "CanManageMovies", "CanManageNews", "CanManageProjections", "CanManageReferenceData", "CanManageRoles", "CanManageUsers", "CanUseChatBot", "CanViewAnalytics", "Color" },
                values: new object[] { false, false, false, false, false, false, false, false, false, false, "#16A34A" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CanAccessDesktop", "CanManageHalls", "CanManageMovies", "CanManageNews", "CanManageProjections", "CanManageReferenceData", "CanManageRoles", "CanManageUsers", "CanUseChatBot", "CanViewAnalytics", "Color" },
                values: new object[] { true, true, true, true, true, true, false, false, true, true, "#2563EB" });

            // Existing JWTs have no Permissions claim; force a fresh login after this schema change.
            migrationBuilder.Sql("UPDATE Users SET TokenVersion = TokenVersion + 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanAccessDesktop",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageHalls",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageMovies",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageNews",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageProjections",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageReferenceData",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageRoles",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanManageUsers",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanUseChatBot",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CanViewAnalytics",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Roles");
        }
    }
}

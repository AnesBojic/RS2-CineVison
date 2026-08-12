using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProjectionEndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Projections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Projections",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1,
                column: "EndTime",
                value: new DateTime(2026, 7, 5, 19, 53, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 2,
                column: "EndTime",
                value: new DateTime(2026, 7, 5, 22, 53, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 3,
                column: "EndTime",
                value: new DateTime(2026, 7, 6, 19, 8, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 4,
                column: "EndTime",
                value: new DateTime(2026, 7, 6, 21, 45, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 5,
                column: "EndTime",
                value: new DateTime(2026, 7, 7, 20, 58, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 7,
                column: "EndTime",
                value: new DateTime(2026, 8, 10, 19, 53, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 8,
                column: "EndTime",
                value: new DateTime(2026, 8, 10, 22, 53, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 9,
                column: "EndTime",
                value: new DateTime(2026, 8, 12, 18, 38, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 10,
                column: "EndTime",
                value: new DateTime(2026, 8, 15, 20, 45, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 11,
                column: "EndTime",
                value: new DateTime(2026, 8, 20, 21, 58, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 12,
                column: "EndTime",
                value: new DateTime(2026, 9, 5, 20, 37, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 13,
                column: "EndTime",
                value: new DateTime(2026, 9, 12, 20, 53, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 14,
                column: "EndTime",
                value: new DateTime(2026, 9, 18, 17, 38, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 15,
                column: "EndTime",
                value: new DateTime(2026, 10, 3, 21, 58, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 16,
                column: "EndTime",
                value: new DateTime(2026, 10, 10, 19, 45, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 17,
                column: "EndTime",
                value: new DateTime(2026, 10, 22, 21, 37, 0, 0, DateTimeKind.Utc));
        }
    }
}

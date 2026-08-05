using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class SeedUpcomingScreenings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Screenings",
                columns: new[] { "Id", "BasePrice", "CreatedAt", "EndTime", "HallId", "IsActive", "LanguageId", "MovieId", "StartTime", "UpdatedAt" },
                values: new object[,]
                {
                    { 7, 8.50m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 10, 19, 53, 0, 0, DateTimeKind.Utc), 1, true, 1, 1, new DateTime(2026, 8, 10, 18, 0, 0, 0, DateTimeKind.Utc), null },
                    { 8, 9.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 10, 22, 53, 0, 0, DateTimeKind.Utc), 1, true, 1, 1, new DateTime(2026, 8, 10, 21, 0, 0, 0, DateTimeKind.Utc), null },
                    { 9, 7.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 12, 18, 38, 0, 0, DateTimeKind.Utc), 2, true, 1, 2, new DateTime(2026, 8, 12, 17, 0, 0, 0, DateTimeKind.Utc), null },
                    { 10, 9.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 15, 20, 45, 0, 0, DateTimeKind.Utc), 1, true, 2, 3, new DateTime(2026, 8, 15, 19, 0, 0, 0, DateTimeKind.Utc), null },
                    { 11, 10.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 20, 21, 58, 0, 0, DateTimeKind.Utc), 2, true, 1, 5, new DateTime(2026, 8, 20, 20, 0, 0, 0, DateTimeKind.Utc), null },
                    { 12, 8.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 5, 20, 37, 0, 0, DateTimeKind.Utc), 1, true, 1, 4, new DateTime(2026, 9, 5, 18, 30, 0, 0, DateTimeKind.Utc), null },
                    { 13, 8.50m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 12, 20, 53, 0, 0, DateTimeKind.Utc), 2, true, 1, 1, new DateTime(2026, 9, 12, 19, 0, 0, 0, DateTimeKind.Utc), null },
                    { 14, 7.50m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 18, 17, 38, 0, 0, DateTimeKind.Utc), 1, true, 1, 2, new DateTime(2026, 9, 18, 16, 0, 0, 0, DateTimeKind.Utc), null },
                    { 15, 10.50m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 3, 21, 58, 0, 0, DateTimeKind.Utc), 1, true, 1, 5, new DateTime(2026, 10, 3, 20, 0, 0, 0, DateTimeKind.Utc), null },
                    { 16, 9.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 10, 19, 45, 0, 0, DateTimeKind.Utc), 2, true, 1, 3, new DateTime(2026, 10, 10, 18, 0, 0, 0, DateTimeKind.Utc), null },
                    { 17, 8.00m, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 22, 21, 37, 0, 0, DateTimeKind.Utc), 2, true, 2, 4, new DateTime(2026, 10, 22, 19, 30, 0, 0, DateTimeKind.Utc), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 17);
        }
    }
}

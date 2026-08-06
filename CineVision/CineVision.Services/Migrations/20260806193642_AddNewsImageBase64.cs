using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsImageBase64 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageBase64",
                table: "News",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "News",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageBase64",
                value: null);

            migrationBuilder.UpdateData(
                table: "News",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageBase64",
                value: null);

            migrationBuilder.InsertData(
                table: "News",
                columns: new[] { "Id", "Content", "CreatedAt", "ImageBase64", "IsActive", "PublishedAt", "Title", "UpdatedAt" },
                values: new object[] { 3, "This weekend Hall A hosts an all-day sci-fi marathon. Combo snacks included with every ticket.", new DateTime(2026, 7, 5, 15, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 7, 5, 15, 0, 0, 0, DateTimeKind.Utc), "IMAX weekend marathon", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "News",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "ImageBase64",
                table: "News");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class Phase4SchemaAuditNewsSearchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Reservations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Query = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: true),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchHistories_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SearchHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "News",
                columns: new[] { "Id", "Content", "CreatedAt", "ImageBase64", "IsActive", "PublishedAt", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Join us every Friday for premiere nights with discounted snacks and late shows.", new DateTime(2026, 6, 10, 12, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 6, 10, 12, 0, 0, 0, DateTimeKind.Utc), "Summer premiere nights", null },
                    { 2, "Show your student ID Monday–Thursday for 20% off base ticket prices.", new DateTime(2026, 6, 20, 9, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 6, 20, 9, 0, 0, 0, DateTimeKind.Utc), "Student discount weekdays", null }
                });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt" },
                values: new object[] { null, null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CancelledByUserId",
                table: "Reservations",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PaymentTransactionId",
                table: "Reservations",
                column: "PaymentTransactionId",
                unique: true,
                filter: "[PaymentTransactionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_GenreId",
                table: "SearchHistories",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_UserId_SearchedAt",
                table: "SearchHistories",
                columns: new[] { "UserId", "SearchedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_CancelledByUserId",
                table: "Reservations",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_CancelledByUserId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "SearchHistories");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CancelledByUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PaymentTransactionId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Reservations");
        }
    }
}

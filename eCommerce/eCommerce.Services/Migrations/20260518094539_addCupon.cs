using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class addCupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CuponId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscountAmount = table.Column<double>(type: "float", nullable: false),
                    DiscountType = table.Column<int>(type: "int", nullable: false),
                    Uses = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupons", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Cupons",
                columns: new[] { "Id", "Code", "CreatedAt", "DiscountAmount", "DiscountType", "ExpiresAt", "IsActive", "UpdatedAt", "Uses" },
                values: new object[,]
                {
                    { 1, "WELCOME10", new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Utc), 10.0, 0, new DateTime(2026, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), true, null, 0 },
                    { 2, "SPRING15", new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Utc), 15.0, 0, new DateTime(2026, 6, 30, 23, 59, 59, 0, DateTimeKind.Utc), true, null, 0 },
                    { 3, "SAVE20", new DateTime(2026, 5, 16, 0, 0, 0, 0, DateTimeKind.Utc), 20.0, 1, new DateTime(2026, 11, 30, 23, 59, 59, 0, DateTimeKind.Utc), true, null, 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CuponId",
                table: "Orders",
                column: "CuponId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Cupons_CuponId",
                table: "Orders",
                column: "CuponId",
                principalTable: "Cupons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Cupons_CuponId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Cupons");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CuponId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CuponId",
                table: "Orders");
        }
    }
}

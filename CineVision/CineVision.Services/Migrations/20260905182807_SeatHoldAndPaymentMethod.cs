using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class SeatHoldAndPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            // Default 1 = Online so existing rows land on a valid enum value.
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Bookings that never carried a Stripe payment were box-office style sales.
            migrationBuilder.Sql(
                "UPDATE [Reservations] SET [PaymentMethod] = 2 WHERE [PaymentTransactionId] IS NULL;");

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 1 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "HoldExpiresAt", "PaymentMethod" },
                values: new object[] { null, 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Reservations");
        }
    }
}

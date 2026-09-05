using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class CountRevenueFromPaymentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing box-office sales collected cash but stored PaymentStatus.None because
            // Confirmed was treated as "not Stripe". Revenue now reads PaymentStatus, so mark
            // those rows as actually paid without touching checkout holds (Pending).
            migrationBuilder.Sql(
                """
                UPDATE [Reservations]
                SET [PaymentStatus] = 1,
                    [PaymentDate] = ISNULL([PaymentDate], [ReservationDate])
                WHERE [PaymentMethod] = 2
                  AND [PaymentStatus] = 0
                  AND [Status] <> 0;
                """);

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "PaymentDate", "PaymentStatus" },
                values: new object[] { new DateTime(2026, 6, 30, 9, 40, 0, 0, DateTimeKind.Utc), 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "PaymentDate", "PaymentStatus" },
                values: new object[] { null, 0 });
        }
    }
}

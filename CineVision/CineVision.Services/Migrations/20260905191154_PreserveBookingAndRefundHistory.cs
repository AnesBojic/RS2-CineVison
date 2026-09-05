using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class PreserveBookingAndRefundHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservationSeats_ProjectionId_SeatId",
                table: "ReservationSeats");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReleasedAt",
                table: "ReservationSeats",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentStatus",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RefundError",
                table: "Reservations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundId",
                table: "Reservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundStatus",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            // Bookings that already collected money carry a payment fact that predates this
            // column; PaymentDate is only stamped on the transition to Paid, so it identifies
            // them even after they were cancelled.
            migrationBuilder.Sql(
                "UPDATE [Reservations] SET [PaymentStatus] = 1 WHERE [PaymentDate] IS NOT NULL;");

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 2,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 3,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 4,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 5,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 6,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 7,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 8,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 9,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 10,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 11,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 12,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 13,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 14,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 15,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 16,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 17,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 18,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 19,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 20,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 21,
                column: "ReleasedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 1, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 1, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 1, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 1, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 1, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 1, null, null, 0, null });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "PaymentStatus", "RefundError", "RefundId", "RefundStatus", "RefundedAt" },
                values: new object[] { 0, null, null, 0, null });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSeats_ProjectionId_SeatId",
                table: "ReservationSeats",
                columns: new[] { "ProjectionId", "SeatId" },
                unique: true,
                filter: "[ReleasedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservationSeats_ProjectionId_SeatId",
                table: "ReservationSeats");

            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "ReservationSeats");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RefundError",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RefundId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationSeats_ProjectionId_SeatId",
                table: "ReservationSeats",
                columns: new[] { "ProjectionId", "SeatId" },
                unique: true);
        }
    }
}

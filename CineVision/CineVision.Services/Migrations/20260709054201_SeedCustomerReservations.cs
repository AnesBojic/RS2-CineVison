using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class SeedCustomerReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Reservations",
                columns: new[] { "Id", "CustomerEmail", "CustomerName", "PaymentDate", "PaymentTransactionId", "ReservationDate", "ReservationNumber", "ScreeningId", "Status", "TotalAmount", "UserId" },
                values: new object[,]
                {
                    { 1, "customer1@gmail.com", "Dave Customer", new DateTime(2026, 6, 15, 14, 31, 0, 0, DateTimeKind.Utc), "pi_seed_001", new DateTime(2026, 6, 15, 14, 30, 0, 0, DateTimeKind.Utc), "R-SEED-001", 1, 2, 25.50m, 4 },
                    { 2, "customer2@gmail.com", "Eve Customer", new DateTime(2026, 6, 20, 10, 16, 0, 0, DateTimeKind.Utc), "pi_seed_002", new DateTime(2026, 6, 20, 10, 15, 0, 0, DateTimeKind.Utc), "R-SEED-002", 1, 2, 17.00m, 5 },
                    { 3, "customer1@gmail.com", "Dave Customer", new DateTime(2026, 6, 22, 18, 46, 0, 0, DateTimeKind.Utc), "pi_seed_003", new DateTime(2026, 6, 22, 18, 45, 0, 0, DateTimeKind.Utc), "R-SEED-003", 3, 2, 21.00m, 4 },
                    { 4, "customer2@gmail.com", "Eve Customer", new DateTime(2026, 7, 1, 11, 1, 0, 0, DateTimeKind.Utc), "pi_seed_004", new DateTime(2026, 7, 1, 11, 0, 0, 0, DateTimeKind.Utc), "R-SEED-004", 4, 2, 36.00m, 5 },
                    { 5, "customer1@gmail.com", "Dave Customer", new DateTime(2026, 7, 3, 16, 21, 0, 0, DateTimeKind.Utc), "pi_seed_005", new DateTime(2026, 7, 3, 16, 20, 0, 0, DateTimeKind.Utc), "R-SEED-005", 5, 2, 40.00m, 4 },
                    { 6, "customer2@gmail.com", "Eve Customer", new DateTime(2026, 6, 28, 20, 6, 0, 0, DateTimeKind.Utc), "pi_seed_006", new DateTime(2026, 6, 28, 20, 5, 0, 0, DateTimeKind.Utc), "R-SEED-006", 2, 2, 34.00m, 5 },
                    { 7, "customer1@gmail.com", "Dave Customer", null, null, new DateTime(2026, 6, 30, 9, 40, 0, 0, DateTimeKind.Utc), "R-SEED-007", 2, 1, 8.50m, 4 }
                });

            migrationBuilder.InsertData(
                table: "ReservationSeats",
                columns: new[] { "Id", "Price", "ReservationId", "ScreeningId", "SeatId" },
                values: new object[,]
                {
                    { 1, 8.50m, 1, 1, 1 },
                    { 2, 8.50m, 1, 1, 2 },
                    { 3, 8.50m, 1, 1, 3 },
                    { 4, 8.50m, 2, 1, 4 },
                    { 5, 8.50m, 2, 1, 5 },
                    { 6, 7.00m, 3, 3, 20 },
                    { 7, 7.00m, 3, 3, 21 },
                    { 8, 7.00m, 3, 3, 22 },
                    { 9, 9.00m, 4, 4, 41 },
                    { 10, 9.00m, 4, 4, 42 },
                    { 11, 9.00m, 4, 4, 43 },
                    { 12, 9.00m, 4, 4, 44 },
                    { 13, 10.00m, 5, 5, 47 },
                    { 14, 10.00m, 5, 5, 48 },
                    { 15, 10.00m, 5, 5, 49 },
                    { 16, 10.00m, 5, 5, 50 },
                    { 17, 8.50m, 6, 2, 9 },
                    { 18, 8.50m, 6, 2, 10 },
                    { 19, 8.50m, 6, 2, 11 },
                    { 20, 8.50m, 6, 2, 12 },
                    { 21, 8.50m, 7, 2, 13 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}

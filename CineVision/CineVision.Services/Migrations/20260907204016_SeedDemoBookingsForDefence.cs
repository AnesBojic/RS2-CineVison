using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoBookingsForDefence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Live DBs may have deleted seed movie 6 (Quantum Drift). Restore it before
            // inserting demo projections that reference it. Fresh DBs already have it.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Movies] WHERE [Id] = 6)
                BEGIN
                    SET IDENTITY_INSERT [Movies] ON;
                    INSERT INTO [Movies] ([Id], [AgeRatingId], [CreatedAt], [Description], [DurationMinutes], [GenreId], [LanguageId], [PosterImageBase64], [ReleaseDate], [Title], [UpdatedAt], [ViewCount])
                    VALUES (6, 3, '2026-06-01T00:00:00.0000000Z', N'A physicist discovers a way to travel between parallel worlds.', 134, 4, 1, NULL, '2026-06-12T00:00:00.0000000Z', N'Quantum Drift', NULL, 60);
                    SET IDENTITY_INSERT [Movies] OFF;
                END
                """);

            migrationBuilder.InsertData(
                table: "Projections",
                columns: new[] { "Id", "BasePrice", "CancellationReason", "CancelledAt", "CreatedAt", "HallId", "LanguageId", "MovieId", "StartTime", "UpdatedAt" },
                values: new object[,]
                {
                    { 1001, 9.00m, "Projector fault — show cancelled by staff.", new DateTime(2026, 9, 6, 10, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 6, new DateTime(2026, 9, 25, 18, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 6, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { 1002, 9.00m, "Low attendance — screening withdrawn.", new DateTime(2026, 9, 6, 11, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 2, 6, new DateTime(2026, 9, 26, 20, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 6, 11, 0, 0, 0, DateTimeKind.Utc) },
                    { 1003, 8.50m, null, null, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 1, new DateTime(2026, 9, 14, 18, 0, 0, 0, DateTimeKind.Utc), null },
                    { 1004, 7.50m, null, null, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 2, new DateTime(2026, 9, 20, 17, 0, 0, 0, DateTimeKind.Utc), null },
                    { 1005, 10.50m, null, null, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 5, new DateTime(2026, 10, 5, 19, 0, 0, 0, DateTimeKind.Utc), null },
                    { 1006, 9.00m, null, null, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, 1, 3, new DateTime(2026, 10, 12, 19, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 5, 20, 0, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 5, 20, 0, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 6, 19, 30, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 6, 22, 0, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 7, 21, 0, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 5, 23, 0, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { new DateTime(2026, 7, 5, 23, 0, 0, 0, DateTimeKind.Utc), 4 });

            migrationBuilder.InsertData(
                table: "Reservations",
                columns: new[] { "Id", "CancellationReason", "CancelledAt", "CancelledByUserId", "CompletedAt", "CustomerEmail", "CustomerName", "HoldExpiresAt", "PaymentDate", "PaymentMethod", "PaymentStatus", "PaymentTransactionId", "ProjectionId", "RefundError", "RefundId", "RefundStatus", "RefundedAt", "ReservationDate", "ReservationNumber", "Status", "TotalAmount", "UserId" },
                values: new object[,]
                {
                    { 1001, null, null, null, null, "customer1@gmail.com", "Dave Customer", null, new DateTime(2026, 9, 1, 12, 1, 0, 0, DateTimeKind.Utc), 1, 1, "pi_seed_008", 1003, null, null, 0, null, new DateTime(2026, 9, 1, 12, 0, 0, 0, DateTimeKind.Utc), "R-SEED-008", 2, 17.00m, 4 },
                    { 1003, null, null, null, null, "customer1@gmail.com", "Dave Customer", null, new DateTime(2026, 9, 7, 11, 15, 0, 0, DateTimeKind.Utc), 2, 1, null, 1005, null, null, 0, null, new DateTime(2026, 9, 7, 11, 15, 0, 0, DateTimeKind.Utc), "R-SEED-010", 1, 10.50m, 4 },
                    { 1004, "Projector fault — show cancelled by staff.", new DateTime(2026, 9, 6, 10, 0, 0, 0, DateTimeKind.Utc), 1, null, "customer2@gmail.com", "Eve Customer", null, new DateTime(2026, 9, 2, 16, 1, 0, 0, DateTimeKind.Utc), 1, 1, "pi_seed_011", 1001, null, "re_seed_011", 2, new DateTime(2026, 9, 6, 10, 5, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 2, 16, 0, 0, 0, DateTimeKind.Utc), "R-SEED-011", 3, 18.00m, 5 },
                    { 1005, "Low attendance — screening withdrawn.", new DateTime(2026, 9, 6, 11, 0, 0, 0, DateTimeKind.Utc), 1, null, "customer1@gmail.com", "Dave Customer", null, new DateTime(2026, 9, 3, 14, 21, 0, 0, DateTimeKind.Utc), 1, 1, "pi_seed_012", 1002, "Stripe refund failed: the charge could not be refunded (seeded demo for retry).", null, 3, null, new DateTime(2026, 9, 3, 14, 20, 0, 0, DateTimeKind.Utc), "R-SEED-012", 3, 18.00m, 4 }
                });

            migrationBuilder.InsertData(
                table: "ReservationSeats",
                columns: new[] { "Id", "Price", "ProjectionId", "ReleasedAt", "ReservationId", "SeatId" },
                values: new object[,]
                {
                    { 1001, 8.50m, 1003, null, 1001, 33 },
                    { 1002, 8.50m, 1003, null, 1001, 34 },
                    { 1004, 10.50m, 1005, null, 1003, 36 },
                    { 1005, 9.00m, 1001, new DateTime(2026, 9, 6, 10, 0, 0, 0, DateTimeKind.Utc), 1004, 1 },
                    { 1006, 9.00m, 1001, new DateTime(2026, 9, 6, 10, 0, 0, 0, DateTimeKind.Utc), 1004, 2 },
                    { 1007, 9.00m, 1002, new DateTime(2026, 9, 6, 11, 0, 0, 0, DateTimeKind.Utc), 1005, 3 },
                    { 1008, 9.00m, 1002, new DateTime(2026, 9, 6, 11, 0, 0, 0, DateTimeKind.Utc), 1005, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1006);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1007);

            migrationBuilder.DeleteData(
                table: "ReservationSeats",
                keyColumn: "Id",
                keyValue: 1008);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1006);

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 2 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 2 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 2 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 2 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 2 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 2 });

            migrationBuilder.UpdateData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CompletedAt", "Status" },
                values: new object[] { null, 1 });
        }
    }
}

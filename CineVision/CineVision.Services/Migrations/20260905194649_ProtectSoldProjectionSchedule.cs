using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class ProtectSoldProjectionSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Projections",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Projections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Projections",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "CancellationReason", "CancelledAt" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Projections");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Projections");
        }
    }
}

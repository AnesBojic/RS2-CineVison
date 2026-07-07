using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class addMoredProductsInSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "Price", "ProductState", "ProductTypeId", "SKU", "StockQuantity", "UnitOfMeasureId", "UpdatedAt", "Weight" },
                values: new object[,]
                {
                    { 8, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Portable 1TB external SSD with high-speed USB-C connectivity", true, "External SSD 1TB", 129.99m, "DraftProductState", null, "SSD-800", 60, null, null, 80m },
                    { 9, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Full HD webcam with built-in microphone and privacy shutter", true, "Webcam Pro 1080p", 59.99m, "DraftProductState", null, "CAM-900", 90, null, null, 140m },
                    { 10, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Compact Bluetooth speaker with waterproof design and deep bass", true, "Bluetooth Speaker", 49.99m, "DraftProductState", null, "SPK-1000", 110, null, null, 620m },
                    { 11, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Fitness-focused smartwatch with heart-rate tracking and GPS", true, "Smartwatch Active", 149.99m, "DraftProductState", null, "WCH-1100", 55, null, null, 50m },
                    { 12, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Adjustable aluminum laptop stand for improved desk ergonomics", true, "Laptop Stand Aluminum", 39.99m, "DraftProductState", null, "STD-1200", 85, null, null, 750m },
                    { 13, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Dual-band Wi-Fi 6 router with extended coverage for home networks", true, "Wi-Fi 6 Router", 119.99m, "DraftProductState", null, "RTR-1300", 45, null, null, 680m },
                    { 14, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Digital drawing tablet with pressure-sensitive stylus", true, "Graphics Tablet", 79.99m, "DraftProductState", null, "TAB-1400", 35, null, null, 420m },
                    { 15, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "20000mAh portable power bank with dual USB output", true, "Portable Power Bank", 34.99m, "DraftProductState", null, "PWR-1500", 130, null, null, 410m },
                    { 16, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Cat 6 Ethernet cable for reliable high-speed wired networking", true, "Ethernet Cable 10m", 12.99m, "DraftProductState", null, "NET-1600", 300, null, null, 260m },
                    { 17, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "USB-C docking station with HDMI, Ethernet, USB-A, and card reader ports", true, "Docking Station", 99.99m, "DraftProductState", null, "DOC-1700", 50, null, null, 520m },
                    { 18, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Adjustable LED desk lamp with touch controls and multiple brightness levels", true, "Smart LED Desk Lamp", 44.99m, "DraftProductState", null, "LMP-1800", 70, null, null, 850m },
                    { 19, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Compact 4K action camera with waterproof casing and image stabilization", true, "Action Camera", 179.99m, "DraftProductState", null, "ACT-1900", 28, null, null, 160m },
                    { 20, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Standalone virtual reality headset with motion controllers", true, "VR Headset", 299.99m, "DraftProductState", null, "VRH-2000", 20, null, null, 620m },
                    { 21, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Central smart home hub for connecting lights, sensors, and voice assistants", true, "Smart Home Hub", 84.99m, "DraftProductState", null, "HUB-2100", 42, null, null, 300m },
                    { 22, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Lightweight fitness tracker with step counting, sleep monitoring, and notifications", true, "Fitness Tracker Band", 69.99m, "DraftProductState", null, "FIT-2200", 95, null, null, 35m },
                    { 23, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Slim wireless charging pad compatible with Qi-enabled smartphones and earbuds", true, "Wireless Charging Pad", 24.99m, "DraftProductState", null, "WCP-2300", 160, null, null, 110m },
                    { 24, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Two-bay network attached storage enclosure for backups and media sharing", true, "NAS Storage Enclosure", 229.99m, "DraftProductState", null, "NAS-2400", 18, null, null, 1300m },
                    { 25, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Portable digital voice recorder with noise reduction and long recording time", true, "Digital Voice Recorder", 54.99m, "DraftProductState", null, "REC-2500", 65, null, null, 90m },
                    { 26, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Portable mini projector with HDMI input and built-in speaker", true, "Mini Projector", 159.99m, "DraftProductState", null, "PRJ-2600", 32, null, null, 950m },
                    { 27, new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Utc), "Wi-Fi smart doorbell camera with motion detection and two-way audio", true, "Smart Doorbell Camera", 139.99m, "DraftProductState", null, "DRB-2700", 38, null, null, 250m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 27);
        }
    }
}

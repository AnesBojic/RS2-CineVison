using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class ReferenceDataLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Halls",
                newName: "StatusId");

            migrationBuilder.RenameColumn(
                name: "ScreenType",
                table: "Halls",
                newName: "ScreenTypeId");

            // The renamed columns still hold the old zero-based enum values while the new
            // reference rows are keyed from 1, so every hall is shifted by one.
            migrationBuilder.Sql("UPDATE [Halls] SET [ScreenTypeId] = [ScreenTypeId] + 1, [StatusId] = [StatusId] + 1;");

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "Screenings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgeRatingId",
                table: "Movies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LanguageId",
                table: "Movies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgeRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MinimumAge = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HallStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AllowsScreenings = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScreenTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScreenTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AgeRatings",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "MinimumAge", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "General audiences — all ages admitted", true, 0, "G", null },
                    { 2, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Parental guidance suggested", true, 8, "PG", null },
                    { 3, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Some material may be inappropriate for children under 13", true, 13, "PG-13", null },
                    { 4, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Restricted — under 17 requires an accompanying adult", true, 17, "R", null },
                    { 5, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No one 17 and under admitted", true, 18, "NC-17", null }
                });

            migrationBuilder.InsertData(
                table: "HallStatuses",
                columns: new[] { "Id", "AllowsScreenings", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hall is open and can host projections", true, "Active", null },
                    { 2, false, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Temporarily closed for maintenance", true, "Maintenance", null },
                    { 3, false, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Permanently out of use", true, "Inactive", null }
                });

            migrationBuilder.UpdateData(
                table: "Halls",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ScreenTypeId", "StatusId" },
                values: new object[] { 2, 1 });

            migrationBuilder.UpdateData(
                table: "Halls",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ScreenTypeId", "StatusId" },
                values: new object[] { 1, 1 });

            migrationBuilder.InsertData(
                table: "Languages",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "en", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "English audio", true, "English", null },
                    { 2, "bs", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bosnian audio", true, "Bosnian", null },
                    { 3, "de", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "German audio", true, "German", null },
                    { 4, "es", new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Spanish audio", true, "Spanish", null }
                });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AgeRatingId", "LanguageId" },
                values: new object[] { 3, 1 });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AgeRatingId", "LanguageId" },
                values: new object[] { 2, 1 });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AgeRatingId", "LanguageId" },
                values: new object[] { 4, 1 });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AgeRatingId", "LanguageId" },
                values: new object[] { 3, 1 });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AgeRatingId", "LanguageId" },
                values: new object[] { 3, 1 });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AgeRatingId", "LanguageId" },
                values: new object[] { 3, 1 });

            migrationBuilder.InsertData(
                table: "ScreenTypes",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard 2D digital projection", true, "Standard", null },
                    { 2, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Large-format IMAX screen", true, "IMAX", null },
                    { 3, new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Stereoscopic 3D projection", true, "3D", null }
                });

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LanguageId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 2,
                column: "LanguageId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 3,
                column: "LanguageId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 4,
                column: "LanguageId",
                value: 1);

            // Carry the free-text values over to the new reference rows by matching on name,
            // then retire the old string columns.
            migrationBuilder.Sql(
                "UPDATE m SET m.[AgeRatingId] = a.[Id] FROM [Movies] m INNER JOIN [AgeRatings] a ON a.[Name] = m.[AgeRating];");
            migrationBuilder.Sql(
                "UPDATE m SET m.[LanguageId] = l.[Id] FROM [Movies] m INNER JOIN [Languages] l ON l.[Name] = m.[Language];");
            migrationBuilder.Sql(
                "UPDATE s SET s.[LanguageId] = l.[Id] FROM [Screenings] s INNER JOIN [Languages] l ON l.[Name] = s.[Language];");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Screenings");

            migrationBuilder.DropColumn(
                name: "AgeRating",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Movies");

            migrationBuilder.CreateIndex(
                name: "IX_Screenings_LanguageId",
                table: "Screenings",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_AgeRatingId",
                table: "Movies",
                column: "AgeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_LanguageId",
                table: "Movies",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Halls_ScreenTypeId",
                table: "Halls",
                column: "ScreenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Halls_StatusId",
                table: "Halls",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgeRatings_Name",
                table: "AgeRatings",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HallStatuses_Name",
                table: "HallStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Name",
                table: "Languages",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScreenTypes_Name",
                table: "ScreenTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Halls_HallStatuses_StatusId",
                table: "Halls",
                column: "StatusId",
                principalTable: "HallStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Halls_ScreenTypes_ScreenTypeId",
                table: "Halls",
                column: "ScreenTypeId",
                principalTable: "ScreenTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_AgeRatings_AgeRatingId",
                table: "Movies",
                column: "AgeRatingId",
                principalTable: "AgeRatings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Movies_Languages_LanguageId",
                table: "Movies",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Languages_LanguageId",
                table: "Screenings",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Halls_HallStatuses_StatusId",
                table: "Halls");

            migrationBuilder.DropForeignKey(
                name: "FK_Halls_ScreenTypes_ScreenTypeId",
                table: "Halls");

            migrationBuilder.DropForeignKey(
                name: "FK_Movies_AgeRatings_AgeRatingId",
                table: "Movies");

            migrationBuilder.DropForeignKey(
                name: "FK_Movies_Languages_LanguageId",
                table: "Movies");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Languages_LanguageId",
                table: "Screenings");

            migrationBuilder.DropTable(
                name: "AgeRatings");

            migrationBuilder.DropTable(
                name: "HallStatuses");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "ScreenTypes");

            migrationBuilder.DropIndex(
                name: "IX_Screenings_LanguageId",
                table: "Screenings");

            migrationBuilder.DropIndex(
                name: "IX_Movies_AgeRatingId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_LanguageId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Halls_ScreenTypeId",
                table: "Halls");

            migrationBuilder.DropIndex(
                name: "IX_Halls_StatusId",
                table: "Halls");

            migrationBuilder.DropIndex(
                name: "IX_Genres_Name",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Screenings");

            migrationBuilder.DropColumn(
                name: "AgeRatingId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Movies");

            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "Halls",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ScreenTypeId",
                table: "Halls",
                newName: "ScreenType");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Screenings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgeRating",
                table: "Movies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Movies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Halls",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ScreenType", "Status" },
                values: new object[] { 1, 0 });

            migrationBuilder.UpdateData(
                table: "Halls",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ScreenType", "Status" },
                values: new object[] { 0, 0 });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AgeRating", "Language" },
                values: new object[] { "PG-13", "English" });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AgeRating", "Language" },
                values: new object[] { "PG", "English" });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AgeRating", "Language" },
                values: new object[] { "R", "English" });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AgeRating", "Language" },
                values: new object[] { "PG-13", "English" });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AgeRating", "Language" },
                values: new object[] { "PG-13", "English" });

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AgeRating", "Language" },
                values: new object[] { "PG-13", "English" });

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 1,
                column: "Language",
                value: "English");

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 2,
                column: "Language",
                value: "English");

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 3,
                column: "Language",
                value: "English");

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 4,
                column: "Language",
                value: "English");

            migrationBuilder.UpdateData(
                table: "Screenings",
                keyColumn: "Id",
                keyValue: 5,
                column: "Language",
                value: "English");
        }
    }
}

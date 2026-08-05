using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineVision.Services.Migrations
{
    /// <inheritdoc />
    public partial class RenameScreeningsToProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Screenings_ScreeningId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationSeats_Screenings_ScreeningId",
                table: "ReservationSeats");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Halls_HallId",
                table: "Screenings");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Languages_LanguageId",
                table: "Screenings");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_Movies_MovieId",
                table: "Screenings");

            migrationBuilder.RenameTable(
                name: "Screenings",
                newName: "Projections");

            migrationBuilder.RenameColumn(
                name: "ScreeningId",
                table: "ReservationSeats",
                newName: "ProjectionId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationSeats_ScreeningId_SeatId",
                table: "ReservationSeats",
                newName: "IX_ReservationSeats_ProjectionId_SeatId");

            migrationBuilder.RenameColumn(
                name: "ScreeningId",
                table: "Reservations",
                newName: "ProjectionId");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_ScreeningId",
                table: "Reservations",
                newName: "IX_Reservations_ProjectionId");

            migrationBuilder.RenameColumn(
                name: "AllowsScreenings",
                table: "HallStatuses",
                newName: "AllowsProjections");

            migrationBuilder.RenameIndex(
                name: "IX_Screenings_HallId",
                table: "Projections",
                newName: "IX_Projections_HallId");

            migrationBuilder.RenameIndex(
                name: "IX_Screenings_LanguageId",
                table: "Projections",
                newName: "IX_Projections_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_Screenings_MovieId",
                table: "Projections",
                newName: "IX_Projections_MovieId");

            migrationBuilder.Sql("EXEC sp_rename N'[PK_Screenings]', N'PK_Projections';");

            migrationBuilder.AddForeignKey(
                name: "FK_Projections_Halls_HallId",
                table: "Projections",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projections_Languages_LanguageId",
                table: "Projections",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projections_Movies_MovieId",
                table: "Projections",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Projections_ProjectionId",
                table: "Reservations",
                column: "ProjectionId",
                principalTable: "Projections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationSeats_Projections_ProjectionId",
                table: "ReservationSeats",
                column: "ProjectionId",
                principalTable: "Projections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Projections_ProjectionId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationSeats_Projections_ProjectionId",
                table: "ReservationSeats");

            migrationBuilder.DropForeignKey(
                name: "FK_Projections_Halls_HallId",
                table: "Projections");

            migrationBuilder.DropForeignKey(
                name: "FK_Projections_Languages_LanguageId",
                table: "Projections");

            migrationBuilder.DropForeignKey(
                name: "FK_Projections_Movies_MovieId",
                table: "Projections");

            migrationBuilder.RenameTable(
                name: "Projections",
                newName: "Screenings");

            migrationBuilder.RenameColumn(
                name: "ProjectionId",
                table: "ReservationSeats",
                newName: "ScreeningId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationSeats_ProjectionId_SeatId",
                table: "ReservationSeats",
                newName: "IX_ReservationSeats_ScreeningId_SeatId");

            migrationBuilder.RenameColumn(
                name: "ProjectionId",
                table: "Reservations",
                newName: "ScreeningId");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_ProjectionId",
                table: "Reservations",
                newName: "IX_Reservations_ScreeningId");

            migrationBuilder.RenameColumn(
                name: "AllowsProjections",
                table: "HallStatuses",
                newName: "AllowsScreenings");

            migrationBuilder.RenameIndex(
                name: "IX_Projections_HallId",
                table: "Screenings",
                newName: "IX_Screenings_HallId");

            migrationBuilder.RenameIndex(
                name: "IX_Projections_LanguageId",
                table: "Screenings",
                newName: "IX_Screenings_LanguageId");

            migrationBuilder.RenameIndex(
                name: "IX_Projections_MovieId",
                table: "Screenings",
                newName: "IX_Screenings_MovieId");

            migrationBuilder.Sql("EXEC sp_rename N'[PK_Projections]', N'PK_Screenings';");

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Halls_HallId",
                table: "Screenings",
                column: "HallId",
                principalTable: "Halls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Languages_LanguageId",
                table: "Screenings",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_Movies_MovieId",
                table: "Screenings",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Screenings_ScreeningId",
                table: "Reservations",
                column: "ScreeningId",
                principalTable: "Screenings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationSeats_Screenings_ScreeningId",
                table: "ReservationSeats",
                column: "ScreeningId",
                principalTable: "Screenings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

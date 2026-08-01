using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eCommerce.Services.Migrations
{
    /// <inheritdoc />
    public partial class MovieStateAndNotificationTypeEnums : Migration
    {
        // SQL Server cannot implicitly convert the old text values to int, so each column is
        // swapped for a new one through an explicit CASE mapping instead of AlterColumn.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [UserNotifications] ADD [TypeEnum] int NULL;");
            migrationBuilder.Sql(@"
                UPDATE [UserNotifications] SET [TypeEnum] = CASE [Type]
                    WHEN 'Email' THEN 0
                    WHEN 'Message' THEN 1
                    WHEN 'Reservation' THEN 2
                    WHEN 'Payment' THEN 3
                    WHEN 'Cancellation' THEN 4
                    WHEN 'Status' THEN 5
                    ELSE 0
                END;");
            migrationBuilder.Sql("ALTER TABLE [UserNotifications] ALTER COLUMN [TypeEnum] int NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [UserNotifications] DROP COLUMN [Type];");
            migrationBuilder.Sql("EXEC sp_rename N'[UserNotifications].[TypeEnum]', N'Type', N'COLUMN';");

            migrationBuilder.Sql("ALTER TABLE [Movies] ADD [MovieStateEnum] int NULL;");
            migrationBuilder.Sql(@"
                UPDATE [Movies] SET [MovieStateEnum] =
                    CASE WHEN [MovieState] LIKE 'Active%' THEN 1 ELSE 0 END;");
            migrationBuilder.Sql("ALTER TABLE [Movies] ALTER COLUMN [MovieStateEnum] int NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [Movies] DROP COLUMN [MovieState];");
            migrationBuilder.Sql("EXEC sp_rename N'[Movies].[MovieStateEnum]', N'MovieState', N'COLUMN';");

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 1,
                column: "MovieState",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 2,
                column: "MovieState",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 3,
                column: "MovieState",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 4,
                column: "MovieState",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 5,
                column: "MovieState",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Movies",
                keyColumn: "Id",
                keyValue: 6,
                column: "MovieState",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [Movies] ADD [MovieStateText] nvarchar(1000) NULL;");
            migrationBuilder.Sql(@"
                UPDATE [Movies] SET [MovieStateText] =
                    CASE WHEN [MovieState] = 1 THEN 'ActiveMovieState' ELSE 'DraftMovieState' END;");
            migrationBuilder.Sql("ALTER TABLE [Movies] ALTER COLUMN [MovieStateText] nvarchar(1000) NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [Movies] DROP COLUMN [MovieState];");
            migrationBuilder.Sql("EXEC sp_rename N'[Movies].[MovieStateText]', N'MovieState', N'COLUMN';");

            migrationBuilder.Sql("ALTER TABLE [UserNotifications] ADD [TypeText] nvarchar(50) NULL;");
            migrationBuilder.Sql(@"
                UPDATE [UserNotifications] SET [TypeText] = CASE [Type]
                    WHEN 0 THEN 'Email'
                    WHEN 1 THEN 'Message'
                    WHEN 2 THEN 'Reservation'
                    WHEN 3 THEN 'Payment'
                    WHEN 4 THEN 'Cancellation'
                    WHEN 5 THEN 'Status'
                    ELSE 'Email'
                END;");
            migrationBuilder.Sql("ALTER TABLE [UserNotifications] ALTER COLUMN [TypeText] nvarchar(50) NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [UserNotifications] DROP COLUMN [Type];");
            migrationBuilder.Sql("EXEC sp_rename N'[UserNotifications].[TypeText]', N'Type', N'COLUMN';");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class updateWeek : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "Clubs");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transfers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "SeasonNumber",
                table: "TournamentClubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SeasonNumber",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Week",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentWeek",
                table: "GameStates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Footballers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Potential",
                table: "Footballers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ScheduleTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Week = table.Column<int>(type: "int", nullable: false),
                    TournamentType = table.Column<int>(type: "int", nullable: true),
                    Round = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsTransferOpen = table.Column<bool>(type: "bit", nullable: false),
                    IsTrainingBoost = table.Column<bool>(type: "bit", nullable: false),
                    IsSeasonEnd = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_SeasonNumber_Week",
                table: "Matches",
                columns: new[] { "SeasonNumber", "Week" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleTemplates_Week",
                table: "ScheduleTemplates",
                column: "Week");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Matches_SeasonNumber_Week",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SeasonNumber",
                table: "TournamentClubs");

            migrationBuilder.DropColumn(
                name: "SeasonNumber",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Week",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CurrentWeek",
                table: "GameStates");

            migrationBuilder.DropColumn(
                name: "Potential",
                table: "Footballers");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Footballers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Drawn",
                table: "TournamentClubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Lost",
                table: "TournamentClubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Played",
                table: "TournamentClubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Won",
                table: "TournamentClubs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "AwayClubId",
                table: "Matches",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Group",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlayed",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Leg",
                table: "Matches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Round",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Drawn",
                table: "TournamentClubs");

            migrationBuilder.DropColumn(
                name: "Lost",
                table: "TournamentClubs");

            migrationBuilder.DropColumn(
                name: "Played",
                table: "TournamentClubs");

            migrationBuilder.DropColumn(
                name: "Won",
                table: "TournamentClubs");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "IsPlayed",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Leg",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Round",
                table: "Matches");

            migrationBuilder.AlterColumn<int>(
                name: "AwayClubId",
                table: "Matches",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLineupFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    FootballerId = table.Column<int>(type: "int", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    IsOwnGoal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchGoals_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchGoals_Footballers_FootballerId",
                        column: x => x.FootballerId,
                        principalTable: "Footballers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchGoals_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchLineups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchId = table.Column<int>(type: "int", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    Formation = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLineups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchLineups_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchLineupPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MatchLineupId = table.Column<int>(type: "int", nullable: false),
                    FootballerId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchLineupPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchLineupPlayers_Footballers_FootballerId",
                        column: x => x.FootballerId,
                        principalTable: "Footballers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchLineupPlayers_MatchLineups_MatchLineupId",
                        column: x => x.MatchLineupId,
                        principalTable: "MatchLineups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchGoals_ClubId",
                table: "MatchGoals",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchGoals_FootballerId",
                table: "MatchGoals",
                column: "FootballerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchGoals_MatchId",
                table: "MatchGoals",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineupPlayers_FootballerId",
                table: "MatchLineupPlayers",
                column: "FootballerId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineupPlayers_MatchLineupId",
                table: "MatchLineupPlayers",
                column: "MatchLineupId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_ClubId",
                table: "MatchLineups",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_MatchLineups_MatchId",
                table: "MatchLineups",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchGoals");

            migrationBuilder.DropTable(
                name: "MatchLineupPlayers");

            migrationBuilder.DropTable(
                name: "MatchLineups");
        }
    }
}

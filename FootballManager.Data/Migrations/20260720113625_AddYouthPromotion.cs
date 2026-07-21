using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddYouthPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YouthTrainingQuality",
                table: "Clubs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "PlayerGivenNames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGivenNames", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSurnames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSurnames", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerGivenNames_Nation",
                table: "PlayerGivenNames",
                column: "Nation");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSurnames_Nation",
                table: "PlayerSurnames",
                column: "Nation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGivenNames");

            migrationBuilder.DropTable(
                name: "PlayerSurnames");

            migrationBuilder.DropColumn(
                name: "YouthTrainingQuality",
                table: "Clubs");
        }
    }
}

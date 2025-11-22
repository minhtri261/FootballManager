using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClubMarket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "Clubs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinhVien5TotWeb.Migrations
{
    /// <inheritdoc />
    public partial class updateScoreResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentLevel",
                table: "ScoringResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RejectionCount",
                table: "Applications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLevel",
                table: "ScoringResults");

            migrationBuilder.DropColumn(
                name: "RejectionCount",
                table: "Applications");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SinhVien5TotWeb.Migrations
{
    /// <inheritdoc />
    public partial class updateApplicationCriterion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationCriterion_Applications_ApplicationId",
                table: "ApplicationCriterion");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationCriterion_Criteria_CriterionId",
                table: "ApplicationCriterion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationCriterion",
                table: "ApplicationCriterion");

            migrationBuilder.RenameTable(
                name: "ApplicationCriterion",
                newName: "ApplicationCriterias");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationCriterion_CriterionId",
                table: "ApplicationCriterias",
                newName: "IX_ApplicationCriterias_CriterionId");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "ApplicationCriterias",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationCriterias",
                table: "ApplicationCriterias",
                columns: new[] { "ApplicationId", "CriterionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationCriterias_Applications_ApplicationId",
                table: "ApplicationCriterias",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "ApplicationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationCriterias_Criteria_CriterionId",
                table: "ApplicationCriterias",
                column: "CriterionId",
                principalTable: "Criteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationCriterias_Applications_ApplicationId",
                table: "ApplicationCriterias");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationCriterias_Criteria_CriterionId",
                table: "ApplicationCriterias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationCriterias",
                table: "ApplicationCriterias");

            migrationBuilder.DropColumn(
                name: "id",
                table: "ApplicationCriterias");

            migrationBuilder.RenameTable(
                name: "ApplicationCriterias",
                newName: "ApplicationCriterion");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationCriterias_CriterionId",
                table: "ApplicationCriterion",
                newName: "IX_ApplicationCriterion_CriterionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationCriterion",
                table: "ApplicationCriterion",
                columns: new[] { "ApplicationId", "CriterionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationCriterion_Applications_ApplicationId",
                table: "ApplicationCriterion",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "ApplicationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationCriterion_Criteria_CriterionId",
                table: "ApplicationCriterion",
                column: "CriterionId",
                principalTable: "Criteria",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

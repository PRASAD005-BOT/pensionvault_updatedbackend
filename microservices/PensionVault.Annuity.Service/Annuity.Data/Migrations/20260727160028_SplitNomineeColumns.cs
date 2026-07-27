using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Annuity.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitNomineeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomineeDetails",
                table: "AnnuityPlans");

            migrationBuilder.AddColumn<string>(
                name: "NomineeBankAccount",
                table: "AnnuityPlans",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomineeName",
                table: "AnnuityPlans",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NomineePercent",
                table: "AnnuityPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NomineeRelation",
                table: "AnnuityPlans",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomineeBankAccount",
                table: "AnnuityPlans");

            migrationBuilder.DropColumn(
                name: "NomineeName",
                table: "AnnuityPlans");

            migrationBuilder.DropColumn(
                name: "NomineePercent",
                table: "AnnuityPlans");

            migrationBuilder.DropColumn(
                name: "NomineeRelation",
                table: "AnnuityPlans");

            migrationBuilder.AddColumn<string>(
                name: "NomineeDetails",
                table: "AnnuityPlans",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

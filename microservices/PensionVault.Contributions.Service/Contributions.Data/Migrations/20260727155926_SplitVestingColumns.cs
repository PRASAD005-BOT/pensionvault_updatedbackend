using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contributions.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitVestingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VestingSchedule",
                table: "FundSchemes");

            migrationBuilder.AddColumn<decimal>(
                name: "VestingPercent",
                table: "FundSchemes",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "VestingYears",
                table: "FundSchemes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VestingPercent",
                table: "FundSchemes");

            migrationBuilder.DropColumn(
                name: "VestingYears",
                table: "FundSchemes");

            migrationBuilder.AddColumn<string>(
                name: "VestingSchedule",
                table: "FundSchemes",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

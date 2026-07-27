using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Members.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitJsonColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NomineeDetails",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "VestingSchedule",
                table: "FundSchemes");

            migrationBuilder.DropColumn(
                name: "ContactDetails",
                table: "Employers");

            migrationBuilder.AddColumn<string>(
                name: "NomineeBankAccount",
                table: "Members",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomineeName",
                table: "Members",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NomineePercent",
                table: "Members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NomineeRelation",
                table: "Members",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

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

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Employers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Employers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortalJoinCode",
                table: "Employers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NomineeBankAccount",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "NomineeName",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "NomineePercent",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "NomineeRelation",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "VestingPercent",
                table: "FundSchemes");

            migrationBuilder.DropColumn(
                name: "VestingYears",
                table: "FundSchemes");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "PortalJoinCode",
                table: "Employers");

            migrationBuilder.AddColumn<string>(
                name: "ProfileImageUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomineeDetails",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VestingSchedule",
                table: "FundSchemes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactDetails",
                table: "Employers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}

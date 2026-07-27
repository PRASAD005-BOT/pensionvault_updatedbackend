using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Members.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToFundScheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FundSchemes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "FundSchemes");
        }
    }
}

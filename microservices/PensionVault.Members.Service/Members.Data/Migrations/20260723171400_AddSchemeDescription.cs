using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Members.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemeDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FundSchemes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
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

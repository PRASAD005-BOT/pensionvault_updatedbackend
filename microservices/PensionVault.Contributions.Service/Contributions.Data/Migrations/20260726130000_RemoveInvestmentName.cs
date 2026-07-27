using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contributions.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvestmentName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvestmentName",
                table: "InvestmentPortfolios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvestmentName",
                table: "InvestmentPortfolios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contributions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Start the Investment module fresh: clear any previously-seeded mock investments.
            migrationBuilder.Sql("DELETE FROM [InvestmentPortfolios]");

            migrationBuilder.AddColumn<string>(
                name: "InvestmentName",
                table: "InvestmentPortfolios",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvestmentName",
                table: "InvestmentPortfolios");
        }
    }
}

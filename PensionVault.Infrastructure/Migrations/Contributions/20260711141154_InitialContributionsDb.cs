using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PensionVault.Infrastructure.Migrations.Contributions
{
    /// <inheritdoc />
    public partial class InitialContributionsDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContributionRemittances",
                columns: table => new
                {
                    RemittanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemittancePeriod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TotalEmployeeShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalEmployerShare = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPensionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemittanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoverageCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionRemittances", x => x.RemittanceId);
                });

            migrationBuilder.CreateTable(
                name: "FundSchemes",
                columns: table => new
                {
                    SchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SchemeType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EmployeeContributionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EmployerContributionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    InterestRatePA = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    VestingSchedule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSchemes", x => x.SchemeId);
                });

            migrationBuilder.CreateTable(
                name: "MemberContributions",
                columns: table => new
                {
                    ContributionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemittanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EmployeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PensionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberContributions", x => x.ContributionId);
                    table.ForeignKey(
                        name: "FK_MemberContributions_ContributionRemittances_RemittanceId",
                        column: x => x.RemittanceId,
                        principalTable: "ContributionRemittances",
                        principalColumn: "RemittanceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CorpusRecords",
                columns: table => new
                {
                    CorpusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalContributions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalWithdrawals = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InvestmentIncome = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ManagementExpenses = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingCorpus = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorpusRecords", x => x.CorpusId);
                    table.ForeignKey(
                        name: "FK_CorpusRecords_FundSchemes_SchemeId",
                        column: x => x.SchemeId,
                        principalTable: "FundSchemes",
                        principalColumn: "SchemeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundAccounts",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountOpenDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeContributionBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerContributionBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PensionBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestAccrued = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VestingPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundAccounts", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_FundAccounts_FundSchemes_SchemeId",
                        column: x => x.SchemeId,
                        principalTable: "FundSchemes",
                        principalColumn: "SchemeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvestmentPortfolios",
                columns: table => new
                {
                    PortfolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllocationPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    InvestedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YieldEarned = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestmentPortfolios", x => x.PortfolioId);
                    table.ForeignKey(
                        name: "FK_InvestmentPortfolios_FundSchemes_SchemeId",
                        column: x => x.SchemeId,
                        principalTable: "FundSchemes",
                        principalColumn: "SchemeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterestCreditRecords",
                columns: table => new
                {
                    InterestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalContributions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestRateApplied = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    InterestAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestCreditRecords", x => x.InterestId);
                    table.ForeignKey(
                        name: "FK_InterestCreditRecords_FundAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "FundAccounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    EntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_FundAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "FundAccounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorpusRecords_SchemeId",
                table: "CorpusRecords",
                column: "SchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_FundAccounts_SchemeId",
                table: "FundAccounts",
                column: "SchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterestCreditRecords_AccountId",
                table: "InterestCreditRecords",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestmentPortfolios_SchemeId",
                table: "InvestmentPortfolios",
                column: "SchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_AccountId",
                table: "LedgerEntries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberContributions_RemittanceId",
                table: "MemberContributions",
                column: "RemittanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorpusRecords");

            migrationBuilder.DropTable(
                name: "InterestCreditRecords");

            migrationBuilder.DropTable(
                name: "InvestmentPortfolios");

            migrationBuilder.DropTable(
                name: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "MemberContributions");

            migrationBuilder.DropTable(
                name: "FundAccounts");

            migrationBuilder.DropTable(
                name: "ContributionRemittances");

            migrationBuilder.DropTable(
                name: "FundSchemes");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PensionVault.Infrastructure.Migrations.Annuity
{
    /// <inheritdoc />
    public partial class InitialAnnuityDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnuityPlans",
                columns: table => new
                {
                    AnnuityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PurchaseValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyPension = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AnnuityStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NomineeDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnuityPlans", x => x.AnnuityId);
                });

            migrationBuilder.CreateTable(
                name: "AnnuityRequests",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PensionBalanceAtRequest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstimatedMonthly = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnuityRequests", x => x.RequestId);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPensionDisbursements",
                columns: table => new
                {
                    DisbursementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnnuityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxDeducted = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DisbursedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPensionDisbursements", x => x.DisbursementId);
                    table.ForeignKey(
                        name: "FK_MonthlyPensionDisbursements_AnnuityPlans_AnnuityId",
                        column: x => x.AnnuityId,
                        principalTable: "AnnuityPlans",
                        principalColumn: "AnnuityId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPensionDisbursements_AnnuityId",
                table: "MonthlyPensionDisbursements",
                column: "AnnuityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnnuityRequests");

            migrationBuilder.DropTable(
                name: "MonthlyPensionDisbursements");

            migrationBuilder.DropTable(
                name: "AnnuityPlans");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Claims.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimReasonDescriptionAndProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "BenefitClaims",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedDate",
                table: "BenefitClaims",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "BenefitClaims",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "BenefitClaims",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "BenefitClaims");

            migrationBuilder.DropColumn(
                name: "ProcessedDate",
                table: "BenefitClaims");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "BenefitClaims");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "BenefitClaims");
        }
    }
}

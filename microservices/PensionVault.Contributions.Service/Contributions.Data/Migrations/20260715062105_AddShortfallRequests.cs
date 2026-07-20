using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Contributions.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShortfallRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShortfallRequests",
                columns: table => new
                {
                    ShortfallRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContributionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RaisedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortfallRequests", x => x.ShortfallRequestId);
                    table.ForeignKey(
                        name: "FK_ShortfallRequests_MemberContributions_ContributionId",
                        column: x => x.ContributionId,
                        principalTable: "MemberContributions",
                        principalColumn: "ContributionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortfallRequests_ContributionId",
                table: "ShortfallRequests",
                column: "ContributionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShortfallRequests");
        }
    }
}

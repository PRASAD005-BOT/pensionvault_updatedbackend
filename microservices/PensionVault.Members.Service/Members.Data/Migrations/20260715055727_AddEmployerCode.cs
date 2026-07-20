using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Members.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployerCode",
                table: "Employers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Backfill a unique code for any pre-existing rows before the unique index is created below.
            migrationBuilder.Sql(@"
                UPDATE e
                SET e.EmployerCode = 'EMP-' + UPPER(LEFT(CONVERT(varchar(36), NEWID()), 6))
                FROM Employers e
                WHERE e.EmployerCode = ''
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Employers_EmployerCode",
                table: "Employers",
                column: "EmployerCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employers_EmployerCode",
                table: "Employers");

            migrationBuilder.DropColumn(
                name: "EmployerCode",
                table: "Employers");
        }
    }
}

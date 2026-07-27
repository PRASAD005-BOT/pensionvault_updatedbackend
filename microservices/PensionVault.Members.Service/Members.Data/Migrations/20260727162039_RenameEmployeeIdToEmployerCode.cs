using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Members.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmployeeIdToEmployerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "Users",
                newName: "EmployerCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployerCode",
                table: "Users",
                newName: "EmployeeId");
        }
    }
}

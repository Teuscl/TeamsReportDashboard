using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsReportDashboard.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Removinguniqueindexonemailsender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reports_RequesterEmail",
                table: "Reports");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reports_RequesterEmail",
                table: "Reports",
                column: "RequesterEmail",
                unique: true);
        }
    }
}

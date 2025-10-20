using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsReportDashboard.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisJobToReportRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AnalysisJobId",
                table: "Reports",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AnalysisJobId",
                table: "Reports",
                column: "AnalysisJobId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AnalysisJobs_AnalysisJobId",
                table: "Reports",
                column: "AnalysisJobId",
                principalTable: "AnalysisJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AnalysisJobs_AnalysisJobId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_AnalysisJobId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "AnalysisJobId",
                table: "Reports");
        }
    }
}

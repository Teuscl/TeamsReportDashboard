using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsReportDashboard.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemPromptIdToAnalysisJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SystemPromptId",
                table: "AnalysisJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJobs_SystemPromptId",
                table: "AnalysisJobs",
                column: "SystemPromptId");

            migrationBuilder.AddForeignKey(
                name: "FK_AnalysisJobs_SystemPrompts_SystemPromptId",
                table: "AnalysisJobs",
                column: "SystemPromptId",
                principalTable: "SystemPrompts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AnalysisJobs_SystemPrompts_SystemPromptId",
                table: "AnalysisJobs");

            migrationBuilder.DropIndex(
                name: "IX_AnalysisJobs_SystemPromptId",
                table: "AnalysisJobs");

            migrationBuilder.DropColumn(
                name: "SystemPromptId",
                table: "AnalysisJobs");
        }
    }
}

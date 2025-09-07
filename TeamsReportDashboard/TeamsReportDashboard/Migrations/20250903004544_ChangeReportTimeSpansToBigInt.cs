using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsReportDashboard.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ChangeReportTimeSpansToBigInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Conversão para a coluna FirstResponseTime ---

            // 1. Adiciona uma coluna temporária para armazenar os Ticks (bigint)
            migrationBuilder.AddColumn<long>(name: "FirstResponseTime_temp", table: "Reports", nullable: false, defaultValue: 0L);

            // 2. Executa um SQL para converter os dados de 'time' para Ticks e preencher a nova coluna
            // (Ticks = intervalos de 100 nanossegundos)
            migrationBuilder.Sql("UPDATE [Reports] SET [FirstResponseTime_temp] = CAST(DATEDIFF_BIG(ns, '00:00:00', [FirstResponseTime]) AS BIGINT) / 100");

            // 3. Remove a coluna 'time' original
            migrationBuilder.DropColumn(name: "FirstResponseTime", table: "Reports");

            // 4. Renomeia a coluna temporária para o nome final
            migrationBuilder.RenameColumn(name: "FirstResponseTime_temp", table: "Reports", newName: "FirstResponseTime");


            // --- Conversão para a coluna AverageHandlingTime (repita o processo) ---

            migrationBuilder.AddColumn<long>(name: "AverageHandlingTime_temp", table: "Reports", nullable: false, defaultValue: 0L);
    
            migrationBuilder.Sql("UPDATE [Reports] SET [AverageHandlingTime_temp] = CAST(DATEDIFF_BIG(ns, '00:00:00', [AverageHandlingTime]) AS BIGINT) / 100");

            migrationBuilder.DropColumn(name: "AverageHandlingTime", table: "Reports");
    
            migrationBuilder.RenameColumn(name: "AverageHandlingTime_temp", table: "Reports", newName: "AverageHandlingTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "FirstResponseTime",
                table: "Reports",
                type: "time",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "AverageHandlingTime",
                table: "Reports",
                type: "time",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}

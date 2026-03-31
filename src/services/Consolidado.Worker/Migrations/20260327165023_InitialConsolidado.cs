using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consolidado.Worker.Migrations
{
    /// <inheritdoc />
    public partial class InitialConsolidado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventosProcessados",
                columns: table => new
                {
                    EventoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosProcessados", x => x.EventoId);
                });

            migrationBuilder.CreateTable(
                name: "SaldosDiarios",
                columns: table => new
                {
                    DataReferencia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UltimaAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaldosDiarios", x => x.DataReferencia);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosProcessados");

            migrationBuilder.DropTable(
                name: "SaldosDiarios");
        }
    }
}

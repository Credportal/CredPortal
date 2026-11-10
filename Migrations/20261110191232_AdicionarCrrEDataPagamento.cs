using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCrrEDataPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataPagamento",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Crr",
                table: "MovimentosExternos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataPagamento",
                table: "Amortizacoes",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataPagamento",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "Crr",
                table: "MovimentosExternos");

            migrationBuilder.DropColumn(
                name: "DataPagamento",
                table: "Amortizacoes");
        }
    }
}

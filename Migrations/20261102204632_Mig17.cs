using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DataDesembolso",
                table: "Pedidos",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Amortizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: false),
                    CRR = table.Column<long>(type: "bigint", nullable: true),
                    NR = table.Column<int>(type: "integer", nullable: false),
                    Vencimento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Prestacao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Principal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Juro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoDevedor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxaMultaDia = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrincipalPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    JuroPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MultaPago = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    JuroAPerdoar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MultaAPerdoar = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amortizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Amortizacoes_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Amortizacoes_PedidoId",
                table: "Amortizacoes",
                column: "PedidoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Amortizacoes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataDesembolso",
                table: "Pedidos",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}

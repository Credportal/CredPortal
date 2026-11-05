using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FechosDiariosRegistos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataFecho = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DataHoraProcessamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    TotalDespesasVolume = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDespesasNumero = table.Column<int>(type: "integer", nullable: false),
                    TotalDesembolsosVolume = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDesembolsosNumero = table.Column<int>(type: "integer", nullable: false),
                    TotalReembolsosVolume = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalReembolsosNumero = table.Column<int>(type: "integer", nullable: false),
                    PedidosPendentesNumero = table.Column<int>(type: "integer", nullable: false),
                    PedidosAprovadosNumero = table.Column<int>(type: "integer", nullable: false),
                    CarteiraVigenteNumero = table.Column<int>(type: "integer", nullable: false),
                    CarteiraVigenteVolume = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechosDiariosRegistos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FechosDiariosContas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechoDiarioRegistoId = table.Column<int>(type: "integer", nullable: false),
                    Banco = table.Column<string>(type: "text", nullable: false),
                    Conta = table.Column<string>(type: "text", nullable: false),
                    Titular = table.Column<string>(type: "text", nullable: false),
                    SaldoAbertura = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Creditos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Debitos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SaldoFecho = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechosDiariosContas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FechosDiariosContas_FechosDiariosRegistos_FechoDiarioRegist~",
                        column: x => x.FechoDiarioRegistoId,
                        principalTable: "FechosDiariosRegistos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FechosDiariosGestores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FechoDiarioRegistoId = table.Column<int>(type: "integer", nullable: false),
                    GestorNome = table.Column<string>(type: "text", nullable: false),
                    DesembolsoMontante = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    QtdNovos = table.Column<int>(type: "integer", nullable: false),
                    QtdRep = table.Column<int>(type: "integer", nullable: false),
                    VigenteQtd = table.Column<int>(type: "integer", nullable: false),
                    VigenteCapital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReembolsoCapital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReembolsoQtd = table.Column<int>(type: "integer", nullable: false),
                    MoraHojeCapital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MoraTotalQtd = table.Column<int>(type: "integer", nullable: false),
                    MoraTotalCapital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MoraTotalPercentual = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Dias7Capital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Dias7Qtd = table.Column<int>(type: "integer", nullable: false),
                    Dias15Capital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Dias15Qtd = table.Column<int>(type: "integer", nullable: false),
                    Dias30Capital = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Dias30Qtd = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FechosDiariosGestores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FechosDiariosGestores_FechosDiariosRegistos_FechoDiarioRegi~",
                        column: x => x.FechoDiarioRegistoId,
                        principalTable: "FechosDiariosRegistos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FechosDiariosContas_FechoDiarioRegistoId",
                table: "FechosDiariosContas",
                column: "FechoDiarioRegistoId");

            migrationBuilder.CreateIndex(
                name: "IX_FechosDiariosGestores_FechoDiarioRegistoId",
                table: "FechosDiariosGestores",
                column: "FechoDiarioRegistoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FechosDiariosContas");

            migrationBuilder.DropTable(
                name: "FechosDiariosGestores");

            migrationBuilder.DropTable(
                name: "FechosDiariosRegistos");
        }
    }
}

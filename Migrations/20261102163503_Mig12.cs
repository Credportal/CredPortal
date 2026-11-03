using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CapitalPago",
                table: "Pedidos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataDesembolso",
                table: "Pedidos",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDesembolsado",
                table: "Pedidos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "MovimentosExternos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Credito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Debito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ContaId = table.Column<int>(type: "integer", nullable: false),
                    EntidadeId = table.Column<int>(type: "integer", nullable: true),
                    TipoOperacao = table.Column<string>(type: "text", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Agencia = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosExternos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosExternos_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimentosExternos_Entidades_EntidadeId",
                        column: x => x.EntidadeId,
                        principalTable: "Entidades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosExternos_ContaId",
                table: "MovimentosExternos",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosExternos_EntidadeId",
                table: "MovimentosExternos",
                column: "EntidadeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosExternos");

            migrationBuilder.DropColumn(
                name: "CapitalPago",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "DataDesembolso",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "ValorDesembolsado",
                table: "Pedidos");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CRR = table.Column<decimal>(type: "numeric(38,0)", nullable: true),
                    EntidadeId = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Prazo = table.Column<int>(type: "integer", nullable: false),
                    Taxa = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Prestacao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorDecisao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrazoDecisao = table.Column<int>(type: "integer", nullable: false),
                    TaxaDecisao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrestacaoDecisao = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GestorId = table.Column<int>(type: "integer", nullable: true),
                    Destino = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    EstadoId = table.Column<int>(type: "integer", nullable: false),
                    AgenciaId = table.Column<int>(type: "integer", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Entidades_EntidadeId",
                        column: x => x.EntidadeId,
                        principalTable: "Entidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pedidos_Usuarios_GestorId",
                        column: x => x.GestorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pedidos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_EntidadeId",
                table: "Pedidos",
                column: "EntidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_GestorId",
                table: "Pedidos",
                column: "GestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_UsuarioId",
                table: "Pedidos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pedidos");
        }
    }
}

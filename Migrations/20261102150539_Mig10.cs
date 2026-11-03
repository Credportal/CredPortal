using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MovimentosInternos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Credito = table.Column<decimal>(type: "numeric", nullable: false),
                    Debito = table.Column<decimal>(type: "numeric", nullable: false),
                    ContaOrigemId = table.Column<int>(type: "integer", nullable: true),
                    ContaDestinoId = table.Column<int>(type: "integer", nullable: true),
                    TipoOperacao = table.Column<string>(type: "text", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    Agencia = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosInternos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimentosInternos_Contas_ContaDestinoId",
                        column: x => x.ContaDestinoId,
                        principalTable: "Contas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MovimentosInternos_Contas_ContaOrigemId",
                        column: x => x.ContaOrigemId,
                        principalTable: "Contas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosInternos_ContaDestinoId",
                table: "MovimentosInternos",
                column: "ContaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosInternos_ContaOrigemId",
                table: "MovimentosInternos",
                column: "ContaOrigemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimentosInternos");
        }
    }
}

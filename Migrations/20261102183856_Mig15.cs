using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Agencia",
                table: "MovimentosInternos");

            migrationBuilder.DropColumn(
                name: "Agencia",
                table: "MovimentosExternos");

            migrationBuilder.DropColumn(
                name: "Credito",
                table: "Entidades");

            migrationBuilder.DropColumn(
                name: "Debito",
                table: "Entidades");

            migrationBuilder.DropColumn(
                name: "Saldo",
                table: "Entidades");

            migrationBuilder.AddColumn<int>(
                name: "AgenciaId",
                table: "MovimentosInternos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgenciaId",
                table: "MovimentosExternos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustosAdm",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PedidoId = table.Column<int>(type: "integer", nullable: true),
                    EntidadeId = table.Column<int>(type: "integer", nullable: false),
                    CRR = table.Column<long>(type: "bigint", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ContaId = table.Column<int>(type: "integer", nullable: false),
                    AgenciaId = table.Column<int>(type: "integer", nullable: true),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustosAdm", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustosAdm_Contas_ContaId",
                        column: x => x.ContaId,
                        principalTable: "Contas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustosAdm_Entidades_EntidadeId",
                        column: x => x.EntidadeId,
                        principalTable: "Entidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustosAdm_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CustosAdm_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosInternos_AgenciaId",
                table: "MovimentosInternos",
                column: "AgenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentosExternos_AgenciaId",
                table: "MovimentosExternos",
                column: "AgenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosAdm_ContaId",
                table: "CustosAdm",
                column: "ContaId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosAdm_EntidadeId",
                table: "CustosAdm",
                column: "EntidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosAdm_PedidoId",
                table: "CustosAdm",
                column: "PedidoId");

            migrationBuilder.CreateIndex(
                name: "IX_CustosAdm_UsuarioId",
                table: "CustosAdm",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentosExternos_Agencias_AgenciaId",
                table: "MovimentosExternos",
                column: "AgenciaId",
                principalTable: "Agencias",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentosInternos_Agencias_AgenciaId",
                table: "MovimentosInternos",
                column: "AgenciaId",
                principalTable: "Agencias",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimentosExternos_Agencias_AgenciaId",
                table: "MovimentosExternos");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentosInternos_Agencias_AgenciaId",
                table: "MovimentosInternos");

            migrationBuilder.DropTable(
                name: "CustosAdm");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosInternos_AgenciaId",
                table: "MovimentosInternos");

            migrationBuilder.DropIndex(
                name: "IX_MovimentosExternos_AgenciaId",
                table: "MovimentosExternos");

            migrationBuilder.DropColumn(
                name: "AgenciaId",
                table: "MovimentosInternos");

            migrationBuilder.DropColumn(
                name: "AgenciaId",
                table: "MovimentosExternos");

            migrationBuilder.AddColumn<string>(
                name: "Agencia",
                table: "MovimentosInternos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Agencia",
                table: "MovimentosExternos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Credito",
                table: "Entidades",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Debito",
                table: "Entidades",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Saldo",
                table: "Entidades",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}

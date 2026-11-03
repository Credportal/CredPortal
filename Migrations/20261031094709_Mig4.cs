using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Intervenientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntidadeId = table.Column<int>(type: "integer", nullable: false),
                    Crr = table.Column<long>(type: "bigint", nullable: true),
                    IntervenienteID = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    AgenciaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intervenientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Intervenientes_Entidades_EntidadeId",
                        column: x => x.EntidadeId,
                        principalTable: "Entidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Intervenientes_Entidades_IntervenienteID",
                        column: x => x.IntervenienteID,
                        principalTable: "Entidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Intervenientes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intervenientes_EntidadeId",
                table: "Intervenientes",
                column: "EntidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Intervenientes_IntervenienteID",
                table: "Intervenientes",
                column: "IntervenienteID");

            migrationBuilder.CreateIndex(
                name: "IX_Intervenientes_UsuarioId",
                table: "Intervenientes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Intervenientes");
        }
    }
}

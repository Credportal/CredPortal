using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClausulaQuintaTexto",
                table: "Configuracoes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClausulaTerceiraTexto",
                table: "Configuracoes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClausulaQuintaTexto",
                table: "Configuracoes");

            migrationBuilder.DropColumn(
                name: "ClausulaTerceiraTexto",
                table: "Configuracoes");
        }
    }
}

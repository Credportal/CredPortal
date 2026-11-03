using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NomeEmpresa = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Contactos = table.Column<string>(type: "text", nullable: false),
                    Sede = table.Column<string>(type: "text", nullable: false),
                    Representante = table.Column<string>(type: "text", nullable: false),
                    QualidadeRepresentante = table.Column<string>(type: "text", nullable: false),
                    ClausulaPrimeiraTexto = table.Column<string>(type: "text", nullable: false),
                    ClausulaSegundaTexto = table.Column<string>(type: "text", nullable: false),
                    ClausulaQuartaTexto = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracoes");
        }
    }
}

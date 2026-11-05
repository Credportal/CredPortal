using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig21 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiasControlo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DataReferencia = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EstaAberto = table.Column<bool>(type: "boolean", nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsuarioAberturaId = table.Column<int>(type: "integer", nullable: true),
                    DataFecho = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UsuarioFechoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiasControlo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiasControlo");
        }
    }
}

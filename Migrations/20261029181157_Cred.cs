using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Cred : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Contacto = table.Column<string>(type: "text", nullable: false),
                    Endereco = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cargos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cargo = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "estados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<int>(type: "integer", nullable: false),
                    Senha = table.Column<string>(type: "text", nullable: false),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DataActualizacao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Criar = table.Column<string>(type: "text", nullable: false),
                    Editar = table.Column<string>(type: "text", nullable: false),
                    Visualizar = table.Column<string>(type: "text", nullable: false),
                    Administrar = table.Column<string>(type: "text", nullable: false),
                    AgenciaId = table.Column<int>(type: "integer", nullable: true),
                    CargoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Agencias_AgenciaId",
                        column: x => x.AgenciaId,
                        principalTable: "Agencias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Usuarios_cargos_CargoId",
                        column: x => x.CargoId,
                        principalTable: "cargos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Entidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nuit = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Aniversario = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Genero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstadoCivil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Naturalidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoCliente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Documento = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NrDocumento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LocalEmissao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Emissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Validade = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Alternativo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Profissao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cargo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocalTrabalho = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Pai = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Mae = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Conjuge = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TelefoneConjuge = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AgenciaId = table.Column<int>(type: "integer", nullable: true),
                    UsuarioId = table.Column<int>(type: "integer", nullable: true),
                    DataCadastro = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entidades_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entidades_UsuarioId",
                table: "Entidades",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_AgenciaId",
                table: "Usuarios",
                column: "AgenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CargoId",
                table: "Usuarios",
                column: "CargoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entidades");

            migrationBuilder.DropTable(
                name: "estados");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Agencias");

            migrationBuilder.DropTable(
                name: "cargos");
        }
    }
}

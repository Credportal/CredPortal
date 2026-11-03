using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Credito",
                table: "Contas",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Debito",
                table: "Contas",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Saldo",
                table: "Contas",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Credito",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "Debito",
                table: "Contas");

            migrationBuilder.DropColumn(
                name: "Saldo",
                table: "Contas");
        }
    }
}

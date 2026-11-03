using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Mig13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalDebito",
                table: "Entidades",
                newName: "Debito");

            migrationBuilder.RenameColumn(
                name: "TotalCredito",
                table: "Entidades",
                newName: "Credito");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Debito",
                table: "Entidades",
                newName: "TotalDebito");

            migrationBuilder.RenameColumn(
                name: "Credito",
                table: "Entidades",
                newName: "TotalCredito");
        }
    }
}

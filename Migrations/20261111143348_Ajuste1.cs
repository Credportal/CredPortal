using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cred.Migrations
{
    /// <inheritdoc />
    public partial class Ajuste1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarteiraVigenteNumero",
                table: "FechosDiariosRegistos");

            migrationBuilder.RenameColumn(
                name: "UsuarioId",
                table: "FechosDiariosRegistos",
                newName: "CarteiraVigenteTotalNr");

            migrationBuilder.RenameColumn(
                name: "DataHoraProcessamento",
                table: "FechosDiariosRegistos",
                newName: "DataReferencia");

            migrationBuilder.RenameColumn(
                name: "CarteiraVigenteVolume",
                table: "FechosDiariosRegistos",
                newName: "CarteiraVigenteTotalVolume");

            migrationBuilder.RenameColumn(
                name: "VigenteQtd",
                table: "FechosDiariosGestores",
                newName: "VigenteNr");

            migrationBuilder.RenameColumn(
                name: "VigenteCapital",
                table: "FechosDiariosGestores",
                newName: "VigenteVolume");

            migrationBuilder.RenameColumn(
                name: "ReembolsoQtd",
                table: "FechosDiariosGestores",
                newName: "ReembolsoNr");

            migrationBuilder.RenameColumn(
                name: "ReembolsoCapital",
                table: "FechosDiariosGestores",
                newName: "ReembolsoVolume");

            migrationBuilder.RenameColumn(
                name: "QtdRep",
                table: "FechosDiariosGestores",
                newName: "PedidosPendentesNr");

            migrationBuilder.RenameColumn(
                name: "QtdNovos",
                table: "FechosDiariosGestores",
                newName: "PedidosAprovadosNr");

            migrationBuilder.RenameColumn(
                name: "MoraTotalQtd",
                table: "FechosDiariosGestores",
                newName: "MoraMenor90Nr");

            migrationBuilder.RenameColumn(
                name: "MoraTotalPercentual",
                table: "FechosDiariosGestores",
                newName: "PedidosPendentesVolume");

            migrationBuilder.RenameColumn(
                name: "MoraTotalCapital",
                table: "FechosDiariosGestores",
                newName: "PedidosAprovadosVolume");

            migrationBuilder.RenameColumn(
                name: "MoraHojeCapital",
                table: "FechosDiariosGestores",
                newName: "MoraMenor90Volume");

            migrationBuilder.RenameColumn(
                name: "Dias7Qtd",
                table: "FechosDiariosGestores",
                newName: "MoraMenor7Nr");

            migrationBuilder.RenameColumn(
                name: "Dias7Capital",
                table: "FechosDiariosGestores",
                newName: "MoraMenor7Volume");

            migrationBuilder.RenameColumn(
                name: "Dias30Qtd",
                table: "FechosDiariosGestores",
                newName: "MoraMenor60Nr");

            migrationBuilder.RenameColumn(
                name: "Dias30Capital",
                table: "FechosDiariosGestores",
                newName: "MoraMenor60Volume");

            migrationBuilder.RenameColumn(
                name: "Dias15Qtd",
                table: "FechosDiariosGestores",
                newName: "MoraMenor30Nr");

            migrationBuilder.RenameColumn(
                name: "Dias15Capital",
                table: "FechosDiariosGestores",
                newName: "MoraMenor30Volume");

            migrationBuilder.RenameColumn(
                name: "DesembolsoMontante",
                table: "FechosDiariosGestores",
                newName: "MoraMenor15Volume");

            migrationBuilder.AddColumn<bool>(
                name: "Fechado",
                table: "FechosDiariosRegistos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioFecho",
                table: "FechosDiariosRegistos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DesembolsoNr",
                table: "FechosDiariosGestores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DesembolsoVolume",
                table: "FechosDiariosGestores",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "JurosRecebidosNr",
                table: "FechosDiariosGestores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "JurosRecebidosVolume",
                table: "FechosDiariosGestores",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MoraMaior90Nr",
                table: "FechosDiariosGestores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MoraMaior90Volume",
                table: "FechosDiariosGestores",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MoraMenor15Nr",
                table: "FechosDiariosGestores",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fechado",
                table: "FechosDiariosRegistos");

            migrationBuilder.DropColumn(
                name: "UsuarioFecho",
                table: "FechosDiariosRegistos");

            migrationBuilder.DropColumn(
                name: "DesembolsoNr",
                table: "FechosDiariosGestores");

            migrationBuilder.DropColumn(
                name: "DesembolsoVolume",
                table: "FechosDiariosGestores");

            migrationBuilder.DropColumn(
                name: "JurosRecebidosNr",
                table: "FechosDiariosGestores");

            migrationBuilder.DropColumn(
                name: "JurosRecebidosVolume",
                table: "FechosDiariosGestores");

            migrationBuilder.DropColumn(
                name: "MoraMaior90Nr",
                table: "FechosDiariosGestores");

            migrationBuilder.DropColumn(
                name: "MoraMaior90Volume",
                table: "FechosDiariosGestores");

            migrationBuilder.DropColumn(
                name: "MoraMenor15Nr",
                table: "FechosDiariosGestores");

            migrationBuilder.RenameColumn(
                name: "DataReferencia",
                table: "FechosDiariosRegistos",
                newName: "DataHoraProcessamento");

            migrationBuilder.RenameColumn(
                name: "CarteiraVigenteTotalVolume",
                table: "FechosDiariosRegistos",
                newName: "CarteiraVigenteVolume");

            migrationBuilder.RenameColumn(
                name: "CarteiraVigenteTotalNr",
                table: "FechosDiariosRegistos",
                newName: "UsuarioId");

            migrationBuilder.RenameColumn(
                name: "VigenteVolume",
                table: "FechosDiariosGestores",
                newName: "VigenteCapital");

            migrationBuilder.RenameColumn(
                name: "VigenteNr",
                table: "FechosDiariosGestores",
                newName: "VigenteQtd");

            migrationBuilder.RenameColumn(
                name: "ReembolsoVolume",
                table: "FechosDiariosGestores",
                newName: "ReembolsoCapital");

            migrationBuilder.RenameColumn(
                name: "ReembolsoNr",
                table: "FechosDiariosGestores",
                newName: "ReembolsoQtd");

            migrationBuilder.RenameColumn(
                name: "PedidosPendentesVolume",
                table: "FechosDiariosGestores",
                newName: "MoraTotalPercentual");

            migrationBuilder.RenameColumn(
                name: "PedidosPendentesNr",
                table: "FechosDiariosGestores",
                newName: "QtdRep");

            migrationBuilder.RenameColumn(
                name: "PedidosAprovadosVolume",
                table: "FechosDiariosGestores",
                newName: "MoraTotalCapital");

            migrationBuilder.RenameColumn(
                name: "PedidosAprovadosNr",
                table: "FechosDiariosGestores",
                newName: "QtdNovos");

            migrationBuilder.RenameColumn(
                name: "MoraMenor90Volume",
                table: "FechosDiariosGestores",
                newName: "MoraHojeCapital");

            migrationBuilder.RenameColumn(
                name: "MoraMenor90Nr",
                table: "FechosDiariosGestores",
                newName: "MoraTotalQtd");

            migrationBuilder.RenameColumn(
                name: "MoraMenor7Volume",
                table: "FechosDiariosGestores",
                newName: "Dias7Capital");

            migrationBuilder.RenameColumn(
                name: "MoraMenor7Nr",
                table: "FechosDiariosGestores",
                newName: "Dias7Qtd");

            migrationBuilder.RenameColumn(
                name: "MoraMenor60Volume",
                table: "FechosDiariosGestores",
                newName: "Dias30Capital");

            migrationBuilder.RenameColumn(
                name: "MoraMenor60Nr",
                table: "FechosDiariosGestores",
                newName: "Dias30Qtd");

            migrationBuilder.RenameColumn(
                name: "MoraMenor30Volume",
                table: "FechosDiariosGestores",
                newName: "Dias15Capital");

            migrationBuilder.RenameColumn(
                name: "MoraMenor30Nr",
                table: "FechosDiariosGestores",
                newName: "Dias15Qtd");

            migrationBuilder.RenameColumn(
                name: "MoraMenor15Volume",
                table: "FechosDiariosGestores",
                newName: "DesembolsoMontante");

            migrationBuilder.AddColumn<int>(
                name: "CarteiraVigenteNumero",
                table: "FechosDiariosRegistos",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}

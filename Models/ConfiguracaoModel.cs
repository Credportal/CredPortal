namespace Analise.Models
{
    public class ConfiguracaoModel
    {
        public int Id { get; set; }

        // Dados Institucionais (Editáveis)
        public string NomeEmpresa { get; set; } = "Nael Microcrédito E.I.";
        public string Email { get; set; } = "naelmicrocredito@gmail.com";
        public string Contactos { get; set; } = "862357628 / 842357628";
        public string Sede { get; set; } = "Bairro Agostinho Neto, cidade de Maputo";
        public string Representante { get; set; } = "Velma Sara Frederico";
        public string QualidadeRepresentante { get; set; } = "Administrador";

        // Textos Editáveis das Cláusulas do Contrato
        public string ClausulaPrimeiraTexto { get; set; } = "Pelo presente, concede-se ao mutuário o valor aprovado, a ser pago em prestações mensais sobre o capital em dívida.";
        public string ClausulaSegundaTexto { get; set; } = "O empréstimo vence juros à taxa estipulada mensalmente sobre o capital.";
        public string ClausulaTerceiraTexto { get; set; } = "1. O mutuário aceita expressamente devolver o crédito dentro do prazo acordado, contados a partir da data do desembolso.\n2. O mutuário compromete-se a efetuar os reembolsos mediante apresentação do comprovativo junto às instalações do operador.";
        public string ClausulaQuartaTexto { get; set; } = "Os custos administrativos associados à operação de crédito são da responsabilidade do mutuário.";
        public string ClausulaQuintaTexto { get; set; } = "Pode ser considerado imediatamente vencido o presente contrato exigindo-se de imediato o pagamento de todo o valor em dívida em caso de incumprimento.";
    }
}
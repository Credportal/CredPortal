using System.Collections.Generic;

namespace Analise.Models
{
    public class OperacoesViewModel
    {
        public List<ContaModel> ListaContas { get; set; }
        public List<MovimentoInternoModel> ListaMovimentos { get; set; }

        // Instâncias para receber os dados dos formulários de Depósito e Transferência
        public MovimentoInternoModel NovoDeposito { get; set; } = new MovimentoInternoModel();
        public MovimentoInternoModel NovaTransferencia { get; set; } = new MovimentoInternoModel();
    }
}
using System.Collections.Generic;

namespace Analise.Models
{
    public class GarantiaViewModel
    {
        public int PedidoId { get; set; }
        public int EntidadeId { get; set; }
        public string NomeEntidade { get; set; }
        public long? Crr { get; set; }

        // Objeto para o formulário de criação de nova garantia
        public GarantiaModel NovaGarantia { get; set; } = new GarantiaModel();

        // Lista de garantias associadas a esta Entidade / CRR
        public List<GarantiaModel> ListaGarantias { get; set; } = new List<GarantiaModel>();
    }
}
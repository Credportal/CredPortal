using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Analise.Models
{
    public class IntervenienteViewModel
    {
        public int PedidoId { get; set; }
        public int EntidadeId { get; set; }
        public string NomeEntidade { get; set; }
        public long? Crr { get; set; }

        public IntervenienteModel NovoInterveniente { get; set; } = new IntervenienteModel();
        public List<SelectListItem> ListaEntidades { get; set; } = new List<SelectListItem>();
        public List<IntervenienteModel> ListaIntervenientes { get; set; } = new List<IntervenienteModel>();

        public GarantiaModel NovaGarantiaInterveniente { get; set; } = new GarantiaModel();

        // Mapeamento de IntervenienteID -> Lista de Garantias prestadas neste CRR
        public Dictionary<int, List<GarantiaModel>> GarantiasPorInterveniente { get; set; } = new Dictionary<int, List<GarantiaModel>>();
    }
}
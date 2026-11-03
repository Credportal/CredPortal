using Analise.Enuns;
using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class EstadoViewModel
    {
        public EstadoModel EstadoNome { get; set; }
        public List<EstadoModel> ListaEstados { get; set; }
    }
}

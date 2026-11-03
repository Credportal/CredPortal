using Analise.Enuns;
using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class EntidadeViewModel
    {
        public EntidadeModel EntidadeNome { get; set; }
        public List<EntidadeModel> ListaEntidades { get; set; }
    }
}

using Analise.Enuns;
using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class EstadoModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Digite o estado")]
        public string Estado { get; set; }

        public DateTimeOffset DataCadastro { get; set; }
    }
}

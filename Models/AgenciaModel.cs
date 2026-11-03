using Analise.Enuns;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class AgenciaModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Digite o nome da agência")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Digite o contacto")]
        public string Contacto { get; set; }

        [Required(ErrorMessage = "Digite o endereço")]
        public string Endereco { get; set; }
        public DateTimeOffset DataCadastro { get; set; }
    }
}

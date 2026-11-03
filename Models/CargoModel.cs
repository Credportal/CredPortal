using Analise.Enuns;
using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class CargoModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Digite o cargo")]
        public string Cargo { get; set; }

        public DateTimeOffset DataCadastro { get; set; }
    }
}

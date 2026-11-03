using Analise.Enuns;
using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class CargoViewModel
    {
        public CargoModel CargoNome { get; set; }
        public List<CargoModel> ListaCargos { get; set; }
    }
}

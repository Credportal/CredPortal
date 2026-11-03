using Analise.Enuns;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class UsuarioSemSenhaModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Digite o nome")]
        public string Nome { get; set; }
        [Required(ErrorMessage = "Digite o login")]
        public string Login { get; set; }
        [Required(ErrorMessage = "Digite o e-mail")]
        [EmailAddress(ErrorMessage = "Formato do e-mail inválido")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Selecione o perfil para o usuário")]
        public PerfilEnum?Perfil { get; set; }

        [Required(ErrorMessage = "Informe a permissão Criar")]
        public string Criar { get; set; }

        [Required(ErrorMessage = "Informe a permissão Editar")]
        public string Editar { get; set; }

        [Required(ErrorMessage = "Informe a permissão Visualizar")]
        public string Visualizar { get; set; }

        [Required(ErrorMessage = "Informe a permissão Administrar")]
        public string Administrar { get; set; }
        public int? AgenciaId { get; set; }
        [NotMapped]
        public List<SelectListItem> ListaAgencias { get; set; }
        public int? CargoId { get; set; }
        public CargoModel Cargo { get; set; }
        [NotMapped]
        public List<SelectListItem> ListaCargos { get; set; }

    }
}

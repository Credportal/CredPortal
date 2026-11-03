using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class AlterarSenhaModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Digite a senha actual")]
        public string SenhaActual { get; set; }
        [Required(ErrorMessage = "Digite a nova senha")]
        public string NovaSenha { get; set; }
        [Required(ErrorMessage = "Confirme a nova senha")]
        [Compare("NovaSenha",ErrorMessage ="A senha de confirmação não confere com a nova senha criada")]
        public string ConfirmarNovaSenha { get; set; }
    }
}

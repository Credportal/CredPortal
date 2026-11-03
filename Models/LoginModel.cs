using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class LoginModel
    {
        public string Email { get; set; }
        [Required(ErrorMessage = "Digite o login")]
        public string Login { get; set; }
        [Required(ErrorMessage = "Digite a senha")]
        public string Senha { get; set; }

        [Required]
        public string Assunto { get; set; }

        [Required]
        public string Mensagem { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Analise.Models
{
    public class EmailModel
    {
        [Required(ErrorMessage = "Digite o login")]
        public string Login { get; set; }
        [Required, EmailAddress]
        public string Para { get; set; }

        [Required]
        public string Assunto { get; set; }

        [Required]
        public string Mensagem { get; set; }
    }
}

using Analise.Enuns;
using Analise.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Analise.Models
{
    public class UsuarioModel
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
        public PerfilEnum? Perfil { get; set; }
        [Required(ErrorMessage = "Digite a senha")]
        public string Senha {  get; set; }
        
        public DateTimeOffset DataCadastro { get; set; }
        public DateTimeOffset? DataActualizacao { get; set; }

        public bool SenhaValida(string senha)
        {
            return Senha == senha.GerarHash();
        }
        public void SetSenhaHash()
        {
            Senha = Senha.GerarHash();
        }
        public void SetNovaSenha(string novaSenha)
        {
            Senha=novaSenha.GerarHash();
        }
        //public string GerarNovaSenha()
        //{
        //    string novaSenha = Guid.NewGuid().ToString().Substring(0, 8);
        //    Senha = novaSenha.GerarHash();
        //    return novaSenha;
        //}

        public string GerarNovaSenha()
        {
            const string caracteres = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz123456789@#$&";
            // Note que removi 'O', 'o' e '0'

            var random = new Random();
            var novaSenha = new char[8]; // tamanho da senha

            for (int i = 0; i < novaSenha.Length; i++)
            {
                novaSenha[i] = caracteres[random.Next(caracteres.Length)];
            }

            string senhaLimpa = new string(novaSenha);

            // Armazena o hash da senha no banco
            Senha = senhaLimpa.GerarHash();

            return senhaLimpa; // retorna a senha limpa para enviar por email
        }

        [Required(ErrorMessage = "Informe a permissão Criar")]
        public string Criar { get; set; }

        [Required(ErrorMessage = "Informe a permissão Editar")]
        public string Editar { get; set; }

        [Required(ErrorMessage = "Informe a permissão Visualizar")]
        public string Visualizar { get; set; }

        [Required(ErrorMessage = "Informe a permissão Administrar")]
        public string Administrar { get; set; }


        public int? AgenciaId { get; set; }
        public AgenciaModel Agencia { get; set; }
        [NotMapped]
        public List<SelectListItem> ListaAgencias { get; set; }

        public int? CargoId { get; set; }
        public CargoModel Cargo { get; set; }
        [NotMapped]
        public List<SelectListItem> ListaCargos { get; set; }
    }
}

using Analise.Models;

namespace Analise.Repositorio
{
    public interface IUsuarioRepositorio
    {
        UsuarioModel BuscarLogin(string login);
        UsuarioModel BuscarPorEmailELogin(string email ,string login);
        UsuarioModel ListarPorId(int id);
        List<UsuarioModel> BuscarTodos();
        UsuarioModel Adicionar(UsuarioModel usuario);
        UsuarioModel Actualizar(UsuarioModel usuario);
        UsuarioModel AlterarSenha(AlterarSenhaModel alterarSenha);
        bool Apagar(int id);
    }
}

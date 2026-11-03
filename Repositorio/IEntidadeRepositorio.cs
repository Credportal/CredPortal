using Analise.Models;

namespace Analise.Repositorio
{
    public interface IEntidadeRepositorio
    {
        EntidadeModel ListarPorId(int id);
        List<EntidadeModel> BuscarTodos();
        EntidadeModel Adicionar(EntidadeModel entidade);
        EntidadeModel Actualizar(EntidadeModel entidade);    
    }
}

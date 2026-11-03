using Analise.Models;

namespace Analise.Repositorio
{
    public interface IAgenciaRepositorio
    {
        AgenciaModel ListarPorId(int id);
        List<AgenciaModel> BuscarTodos();
        AgenciaModel Adicionar(AgenciaModel agencia);
        AgenciaModel Actualizar(AgenciaModel agencia);    
    }
}

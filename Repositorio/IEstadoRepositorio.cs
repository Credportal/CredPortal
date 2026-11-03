using Analise.Models;

namespace Analise.Repositorio
{
    public interface IEstadoRepositorio
    {
        EstadoModel ListarPorId(int id);
        List<EstadoModel> BuscarTodos();
        EstadoModel Adicionar(EstadoModel estado);
        EstadoModel Actualizar(EstadoModel estado);
    }
}

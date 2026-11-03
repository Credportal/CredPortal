using Analise.Models;

namespace Analise.Repositorio
{
    public interface ICargoRepositorio
    {
        CargoModel ListarPorId(int id);
        List<CargoModel> BuscarTodos();
        CargoModel Adicionar(CargoModel cargo);
        CargoModel Actualizar(CargoModel cargo);
    }
}

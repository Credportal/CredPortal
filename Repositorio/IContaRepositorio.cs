using Analise.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public interface IContaRepositorio
    {
        Task<List<ContaModel>> BuscarTodasAsync();
        Task<ContaModel> BuscarPorIdAsync(int id);
        Task<ContaModel> AdicionarAsync(ContaModel conta);
        Task<ContaModel> AtualizarAsync(ContaModel conta);
        Task<bool> ApagarAsync(int id);
    }
}
using Analise.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public interface IMovimentoInternoRepositorio
    {
        Task<List<MovimentoInternoModel>> BuscarTodosAsync();
        Task<MovimentoInternoModel> BuscarPorIdAsync(int id);
        Task<MovimentoInternoModel> AdicionarAsync(MovimentoInternoModel movimento);
        Task<List<MovimentoInternoModel>> BuscarPorContaAsync(int contaId);
    }
}
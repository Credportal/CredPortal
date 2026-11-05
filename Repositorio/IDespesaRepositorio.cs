using Analise.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public interface IDespesaRepositorio
    {
        Task<List<DespesaModel>> BuscarTodasAsync();
        Task<DespesaModel> BuscarPorIdAsync(int id);
        Task AdicionarComMovimentoAsync(DespesaModel despesa, UsuarioModel usuarioLogado);
        Task AtualizarAsync(DespesaModel despesa);
        Task<List<ContaModel>> BuscarContasAsync();
    }
}
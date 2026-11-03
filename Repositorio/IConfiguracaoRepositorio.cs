using Analise.Models;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public interface IConfiguracaoRepositorio
    {
        Task<ConfiguracaoModel> ObterConfiguracaoAsync();
        Task AtualizarAsync(ConfiguracaoModel configuracao);
    }
}
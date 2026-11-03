using Analise.Data;
using Analise.Models;

namespace Analise.Repositorio
{
    public class SimuladorRepositorio : ISimuladorRepositorio
    {
        private readonly BancoContext _context;

        public SimuladorRepositorio(BancoContext bancoContext)
        {
            _context = bancoContext;
        }

        // Implementação dos métodos de persistência do simulador quando necessário
    }
}
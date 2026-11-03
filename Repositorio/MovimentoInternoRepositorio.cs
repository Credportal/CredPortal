using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public class MovimentoInternoRepositorio : IMovimentoInternoRepositorio
    {
        private readonly BancoContext _bancoContext;

        public MovimentoInternoRepositorio(BancoContext bancoContext)
        {
            _bancoContext = bancoContext;
        }

        public async Task<List<MovimentoInternoModel>> BuscarTodosAsync()
        {
            return await _bancoContext.MovimentosInternos
                .Include(n => n.ContaOrigem)
                .Include(n => n.ContaDestino)
                .OrderByDescending(n => n.DataCadastro)
                .ToListAsync();
        }

        public async Task<MovimentoInternoModel> BuscarPorIdAsync(int id)
        {
            return await _bancoContext.MovimentosInternos
                .Include(n => n.ContaOrigem)
                .Include(n => n.ContaDestino)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<MovimentoInternoModel> AdicionarAsync(MovimentoInternoModel movimento)
        {
            await _bancoContext.MovimentosInternos.AddAsync(movimento);
            await _bancoContext.SaveChangesAsync();
            return movimento;
        }

        public async Task<List<MovimentoInternoModel>> BuscarPorContaAsync(int contaId)
        {
            return await _bancoContext.MovimentosInternos
                .Include(n => n.ContaOrigem)
                .Include(n => n.ContaDestino)
                .Where(n => n.ContaOrigemId == contaId || n.ContaDestinoId == contaId)
                .OrderByDescending(n => n.DataCadastro)
                .ToListAsync();
        }
    }
}
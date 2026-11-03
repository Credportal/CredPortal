using Analise.Data; // Ajuste para o namespace do seu DbContext
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public class ContaRepositorio : IContaRepositorio
    {
        private readonly BancoContext _contexto; // Substitua pelo nome do seu Contexto

        public ContaRepositorio(BancoContext contexto)
        {
            _contexto = contexto;
        }

        public async Task<List<ContaModel>> BuscarTodasAsync()
        {
            return await _contexto.Contas.ToListAsync();
        }

        public async Task<ContaModel> BuscarPorIdAsync(int id)
        {
            return await _contexto.Contas.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<ContaModel> AdicionarAsync(ContaModel conta)
        {
            await _contexto.Contas.AddAsync(conta);
            await _contexto.SaveChangesAsync();
            return conta;
        }

        public async Task<ContaModel> AtualizarAsync(ContaModel conta)
        {
            _contexto.Contas.Update(conta);
            await _contexto.SaveChangesAsync();
            return conta;
        }

        public async Task<bool> ApagarAsync(int id)
        {
            var contaDB = await BuscarPorIdAsync(id);
            if (contaDB == null) return false;

            _contexto.Contas.Remove(contaDB);
            await _contexto.SaveChangesAsync();
            return true;
        }
    }
}
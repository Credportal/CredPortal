using Analise.Data;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Repositories // Ajuste o namespace conforme o seu projeto
{
    public class FechoDiarioRepositorio : IFechoDiarioRepositorio
    {
        private readonly BancoContext _context;

        public FechoDiarioRepositorio(BancoContext context)
        {
            _context = context;
        }

        public async Task<FechoDiarioRegisto> ObterPorDataAsync(DateTime data)
        {
            return await _context.FechosDiariosRegistos
                .Include(r => r.SaldosContas)
                .Include(r => r.ResumoGestores)
                .FirstOrDefaultAsync(r => r.DataReferencia.Date == data.Date);
        }

        public async Task<bool> ExisteFechoParaDataAsync(DateTime data)
        {
            return await _context.FechosDiariosRegistos
                .AnyAsync(r => r.DataReferencia.Date == data.Date);
        }

        public async Task InserirFechoAsync(FechoDiarioRegisto fecho)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.FechosDiariosRegistos.Add(fecho);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ContaModel>> ObterContasAsync()
        {
            return await _context.Contas.ToListAsync();
        }

        public async Task<List<MovimentoExternoModel>> ObterMovimentosExternosPorDataAsync(DateTime data, string tipoOperacao)
        {
            return await _context.MovimentosExternos
                .Where(m => m.TipoOperacao == tipoOperacao && m.DataCadastro.Date == data.Date)
                .ToListAsync();
        }

        public async Task<List<AmortizacaoModel>> ObterAmortizacoesPagasPorDataAsync(DateTime data)
        {
            return await _context.Amortizacoes
                .Where(a => a.DataPagamento.HasValue && a.DataPagamento.Value.Date == data.Date)
                .ToListAsync();
        }

        public async Task<List<PedidoModel>> ObterPedidosAsync()
        {
            return await _context.Pedidos
                .Include(p => p.Usuario)
                .ToListAsync();
        }
    }
}
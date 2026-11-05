using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public class DespesaRepositorio : IDespesaRepositorio
    {
        private readonly BancoContext _context;

        public DespesaRepositorio(BancoContext context)
        {
            _context = context;
        }

        public async Task<List<DespesaModel>> BuscarTodasAsync()
        {
            return await _context.Despesas
                .Include(x => x.Conta)
                .Include(x => x.UsuarioCadastro)
                .ToListAsync();
        }

        public async Task<DespesaModel> BuscarPorIdAsync(int id)
        {
            return await _context.Despesas
                .Include(x => x.Conta)
                .Include(x => x.UsuarioCadastro)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AdicionarComMovimentoAsync(DespesaModel despesa, UsuarioModel usuarioLogado)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var conta = await _context.Contas.FirstOrDefaultAsync(c => c.Id == despesa.ContaId);
                if (conta == null)
                {
                    throw new Exception("A conta bancária selecionada é inválida.");
                }

                despesa.DataCadastro = DateTime.Now;
                await _context.Despesas.AddAsync(despesa);

                string descricaoMovimento = $"Pagamento de Despesa: {despesa.Descricao}";

                var movimentoInterno = new MovimentoInternoModel
                {
                    Descricao = descricaoMovimento,
                    Debito = despesa.Valor,
                    ContaOrigemId = conta.Id,
                    ContaDestinoId = conta.Id,
                    TipoOperacao = "Despesa",
                    DataCadastro = DateTimeOffset.Now,
                    UsuarioId = usuarioLogado.Id,
                    AgenciaId = usuarioLogado.AgenciaId
                };
                await _context.MovimentosInternos.AddAsync(movimentoInterno);

                conta.Debito += despesa.Valor;
                _context.Contas.Update(conta);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task AtualizarAsync(DespesaModel despesa)
        {
            _context.Despesas.Update(despesa);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ContaModel>> BuscarContasAsync()
        {
            return await _context.Contas.ToListAsync();
        }
    }
}
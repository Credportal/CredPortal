using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public class PedidoRepositorio : IPedidoRepositorio
    {
        private readonly BancoContext _context;

        public PedidoRepositorio(BancoContext bancoContext)
        {
            _context = bancoContext;
        }

        public async Task<List<PedidoModel>> BuscarTodosAsync()
        {
            return await _context.Pedidos
                .Include(p => p.Entidade)
                .Include(p => p.Usuario)
                .Include(p => p.Gestor)
                .ToListAsync();
        }

        public async Task<PedidoModel> BuscarPorIdAsync(int id)
        {
            return await _context.Pedidos
                .Include(p => p.Entidade)
                .Include(p => p.Usuario)
                .Include(p => p.Gestor)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<int> ContarPedidosPorEntidadeAsync(int entidadeId)
        {
            return await _context.Pedidos.CountAsync(p => p.EntidadeId == entidadeId);
        }

        public async Task<PedidoModel> AdicionarAsync(PedidoModel pedido)
        {
            int totalPedidos = await ContarPedidosPorEntidadeAsync(pedido.EntidadeId);
            int sequencial = totalPedidos + 1;

            string crrString = $"{pedido.EntidadeId}00{sequencial}";
            if (long.TryParse(crrString, out long crrCalculado))
            {
                pedido.CRR = crrCalculado;
            }

            if (pedido.EstadoId == 0)
            {
                pedido.EstadoId = 1;
            }

            pedido.DataCadastro = DateTime.Now;

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();
            return pedido;
        }

        public async Task<PedidoModel> AtualizarAsync(PedidoModel pedido)
        {
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();
            return pedido;
        }

        public async Task<bool> ApagarAsync(int id)
        {
            var pedidoDb = await BuscarPorIdAsync(id);
            if (pedidoDb == null) return false;

            _context.Pedidos.Remove(pedidoDb);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- MÉTODOS DE GARANTIAS ---
        public async Task<List<GarantiaModel>> BuscarGarantiasPorEntidadeECrrAsync(int entidadeId, long? crr)
        {
            return await _context.Garantias
                .Include(g => g.Entidade)
                .Include(g => g.Usuario)
                .Where(g => g.EntidadeId == entidadeId && g.Crr == crr)
                .ToListAsync();
        }

        public async Task<GarantiaModel> AdicionarGarantiaAsync(GarantiaModel garantia)
        {
            await _context.Garantias.AddAsync(garantia);
            await _context.SaveChangesAsync();
            return garantia;
        }

        public async Task<bool> ApagarGarantiaAsync(int id)
        {
            var garantia = await _context.Garantias.FindAsync(id);
            if (garantia == null) return false;

            _context.Garantias.Remove(garantia);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- MÉTODOS DE INTERVENIENTES ---
        public async Task<List<IntervenienteModel>> BuscarIntervenientesPorPedidoAsync(int entidadeId, long? crr)
        {
            var intervenientes = await _context.Intervenientes
                .Include(i => i.Entidade)
                .Include(i => i.IntervenienteEntidade)
                .Where(i => i.EntidadeId == entidadeId && i.Crr == crr)
                .ToListAsync();

            // Calcular dinamicamente o somatório das garantias prestadas por cada interveniente para este CRR
            foreach (var inter in intervenientes)
            {
                var garantiasInter = await BuscarGarantiasIntervenienteAsync(inter.IntervenienteID, crr);
                inter.ValorCompraGarantia = garantiasInter.Sum(g => g.ValorAquisicao);
                inter.ValorActualGarantia = garantiasInter.Sum(g => g.ValorActual);
            }

            return intervenientes;
        }

        public async Task<IntervenienteModel> AdicionarIntervenienteAsync(IntervenienteModel interveniente)
        {
            await _context.Intervenientes.AddAsync(interveniente);
            await _context.SaveChangesAsync();
            return interveniente;
        }

        public async Task<bool> RemoverIntervenienteAsync(int id)
        {
            var interveniente = await _context.Intervenientes.FindAsync(id);
            if (interveniente == null) return false;

            _context.Intervenientes.Remove(interveniente);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<GarantiaModel>> BuscarGarantiasIntervenienteAsync(int entidadeIdInterveniente, long? crr)
        {
            return await _context.Garantias
                .Where(g => g.EntidadeId == entidadeIdInterveniente && g.Crr == crr)
                .ToListAsync();
        }

        public async Task<GarantiaModel> AdicionarGarantiaIntervenienteAsync(GarantiaModel garantia)
        {
            await _context.Garantias.AddAsync(garantia);
            await _context.SaveChangesAsync();
            return garantia;
        }

        public async Task<List<GarantiaModel>> BuscarGarantiasClienteAsync(int entidadeId, long? crr)
        {
            return await _context.Garantias
                .Where(g => g.EntidadeId == entidadeId && g.Crr == crr)
                .ToListAsync();
        }

        public async Task AtualizarDecisaoPedidoAsync(PedidoModel pedido)
        {
            var pedidoDb = await _context.Pedidos.FindAsync(pedido.Id);
            if (pedidoDb != null)
            {
                pedidoDb.ValorDecisao = pedido.ValorDecisao;
                pedidoDb.PrazoDecisao = pedido.PrazoDecisao;
                pedidoDb.TaxaDecisao = pedido.TaxaDecisao;
                pedidoDb.PrestacaoDecisao = pedido.PrestacaoDecisao;
                pedidoDb.EstadoId = pedido.EstadoId;

                _context.Pedidos.Update(pedidoDb);
                await _context.SaveChangesAsync();
            }
        }

    }
}
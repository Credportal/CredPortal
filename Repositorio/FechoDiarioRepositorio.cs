using Analise.Data;
using Analise.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Repositorio
{
    public class FechoDiarioRepositorio : IFechoDiarioRepositorio
    {
        private readonly BancoContext _context;

        public FechoDiarioRepositorio(BancoContext context)
        {
            _context = context;
        }

        public async Task<FechoDiarioViewModel> ObterDadosFechoDiarioAsync(DateTime dataReferencia)
        {
            var dataLimpa = dataReferencia.Date;

            // 1. TENTA CARREGAR O SNAPSHOT HISTÓRICO SE O DIA JÁ ESTIVER FECHADO
            var fechoExistente = await _context.FechosDiariosRegistos
                .Include(f => f.SaldosContas)
                .Include(f => f.MapaDesempenho)
                .FirstOrDefaultAsync(f => f.DataFecho.Date == dataLimpa);

            if (fechoExistente != null)
            {
                return new FechoDiarioViewModel
                {
                    DataReferencia = fechoExistente.DataFecho,
                    TotalDespesasNumero = fechoExistente.TotalDespesasNumero,
                    TotalDespesasVolume = fechoExistente.TotalDespesasVolume,
                    TotalDesembolsosNumero = fechoExistente.TotalDesembolsosNumero,
                    TotalDesembolsosVolume = fechoExistente.TotalDesembolsosVolume,
                    TotalReembolsosNumero = fechoExistente.TotalReembolsosNumero,
                    TotalReembolsosVolume = fechoExistente.TotalReembolsosVolume,
                    PedidosPendentesNumero = fechoExistente.PedidosPendentesNumero,
                    PedidosAprovadosNumero = fechoExistente.PedidosAprovadosNumero,
                    CarteiraVigenteNumero = fechoExistente.CarteiraVigenteNumero,
                    CarteiraVigenteVolume = fechoExistente.CarteiraVigenteVolume,
                    SaldosContas = fechoExistente.SaldosContas.Select(c => new ContaResumoSaldoDto
                    {
                        Banco = c.Banco,
                        Conta = c.Conta,
                        Titular = c.Titular,
                        SaldoAbertura = c.SaldoAbertura,
                        Creditos = c.Creditos,
                        Debitos = c.Debitos,
                        SaldoFecho = c.SaldoFecho
                    }).ToList(),
                    MapaDesempenho = fechoExistente.MapaDesempenho.Select(g => new MapaDesempenhoLinhaDto
                    {
                        GestorNome = g.GestorNome,
                        DesembolsoMontante = g.DesembolsoMontante,
                        QtdNovos = g.QtdNovos,
                        QtdRep = g.QtdRep,
                        VigenteQtd = g.VigenteQtd,
                        VigenteCapital = g.VigenteCapital,
                        ReembolsoCapital = g.ReembolsoCapital,
                        ReembolsoQtd = g.ReembolsoQtd,
                        MoraHojeCapital = g.MoraHojeCapital,
                        MoraTotalQtd = g.MoraTotalQtd,
                        MoraTotalCapital = g.MoraTotalCapital,
                        // REMOVA ESTA LINHA: MoraTotalPercentual = g.MoraTotalPercentual,
                        Dias7Capital = g.Dias7Capital,
                        Dias7Qtd = g.Dias7Qtd,
                        Dias15Capital = g.Dias15Capital,
                        Dias15Qtd = g.Dias15Qtd,
                        Dias30Capital = g.Dias30Capital,
                        Dias30Qtd = g.Dias30Qtd
                    }).ToList()
                };
            }

            // 2. CASO CONTRÁRIO, CALCULA EM TEMPO REAL PARA O DIA ABERTO
            var inicioDia = dataReferencia.Date;
            var fimDia = dataReferencia.Date.AddDays(1).AddTicks(-1);
            var inicioMes = new DateTime(dataReferencia.Year, dataReferencia.Month, 1);

            var viewModel = new FechoDiarioViewModel
            {
                DataReferencia = dataReferencia
            };

            // 1. DESPESAS DO DIA
            var despesasDoDia = await _context.Despesas
                .Where(d => d.DataCadastro >= inicioDia && d.DataCadastro <= fimDia)
                .ToListAsync();

            viewModel.TotalDespesasNumero = despesasDoDia.Count;
            viewModel.TotalDespesasVolume = despesasDoDia.Sum(d => d.Valor);

            // 2. DESEMBOLSOS DO DIA
            var movimentosDesembolso = await _context.MovimentosExternos
                .Where(m => m.TipoOperacao != null && m.TipoOperacao.Contains("Desembolso") && m.DataCadastro >= inicioDia && m.DataCadastro <= fimDia)
                .ToListAsync();

            viewModel.TotalDesembolsosNumero = movimentosDesembolso.Count;
            viewModel.TotalDesembolsosVolume = movimentosDesembolso.Sum(m => m.Debito);

            // 3. REEMBOLSOS / PAGAMENTOS RECEBIDOS NO DIA
            var reembolsosDoDia = await _context.MovimentosExternos
                .Where(m => m.TipoOperacao != null && m.TipoOperacao.Contains("Reembolso") && m.DataCadastro >= inicioDia && m.DataCadastro <= fimDia)
                .ToListAsync();

            viewModel.TotalReembolsosNumero = reembolsosDoDia.Count;
            viewModel.TotalReembolsosVolume = reembolsosDoDia.Sum(m => m.Credito);

            // 4. ESTADO DOS PEDIDOS
            viewModel.PedidosPendentesNumero = await _context.Pedidos.CountAsync(p => p.EstadoId == 1);
            viewModel.PedidosAprovadosNumero = await _context.Pedidos.CountAsync(p => p.EstadoId == 2);

            // 5. SALDOS DE CONTAS (Com Abertura, Créditos, Débitos e Fecho)
            var contasDb = await _context.Contas.ToListAsync();
            var listaSaldosContas = new List<ContaResumoSaldoDto>();

            foreach (var c in contasDb)
            {
                decimal debitosDespesas = await _context.Despesas
                    .Where(d => d.ContaId == c.Id && d.DataCadastro >= inicioDia && d.DataCadastro <= fimDia)
                    .SumAsync(d => (decimal?)d.Valor) ?? 0;

                decimal debitosMov = await _context.MovimentosInternos
                    .Where(m => m.ContaOrigemId == c.Id && m.DataCadastro >= inicioDia && m.DataCadastro <= fimDia)
                    .SumAsync(m => (decimal?)m.Debito) ?? 0;

                decimal totalDebitos = debitosDespesas + debitosMov;

                decimal totalCreditos = await _context.MovimentosExternos
                    .Where(m => m.ContaId == c.Id && m.DataCadastro >= inicioDia && m.DataCadastro <= fimDia)
                    .SumAsync(m => (decimal?)m.Credito) ?? 0;

                decimal saldoFecho = c.Saldo;

                // Corrigido o operador de cálculo do saldo de abertura
                decimal saldoAbertura = saldoFecho - totalCreditos + totalDebitos;

                listaSaldosContas.Add(new ContaResumoSaldoDto
                {
                    Banco = c.Banco,
                    Conta = c.Conta,
                    Titular = c.Titular,
                    SaldoAbertura = saldoAbertura,
                    Creditos = totalCreditos,
                    Debitos = totalDebitos,
                    SaldoFecho = saldoFecho
                });
            }

            viewModel.SaldosContas = listaSaldosContas;

            // 6. MAPA DE DESEMPENHO POR GESTOR
            var pedidosComGestor = await _context.Pedidos
                .Include(p => p.Gestor)
                .Where(p => p.GestorId != null)
                .ToListAsync();

            var gestoresAgrupados = pedidosComGestor.GroupBy(p => p.Gestor?.Nome ?? "Sem Gestor");

            foreach (var grupo in gestoresAgrupados)
            {
                var pedidosGestor = grupo.ToList();

                var linha = new MapaDesempenhoLinhaDto
                {
                    GestorNome = grupo.Key,
                    DesembolsoMontante = pedidosGestor.Where(p => p.DataDesembolso >= inicioMes && p.DataDesembolso <= fimDia).Sum(p => p.ValorDesembolsado),
                    QtdNovos = pedidosGestor.Count(p => p.DataDesembolso >= inicioMes && p.DataDesembolso <= fimDia),
                    QtdRep = 0,
                    VigenteQtd = pedidosGestor.Count(p => p.CapitalVigente > 0),
                    VigenteCapital = pedidosGestor.Sum(p => p.CapitalVigente),
                    ReembolsoCapital = pedidosGestor.Sum(p => p.CapitalPago),
                    ReembolsoQtd = pedidosGestor.Count(p => p.CapitalPago > 0),
                    MoraHojeQtd = 0,
                    MoraHojeCapital = 0,
                    MoraTotalQtd = 0,
                    MoraTotalCapital = 0
                };

                viewModel.MapaDesempenho.Add(linha);
            }

            viewModel.CarteiraVigenteNumero = pedidosComGestor.Count(p => p.CapitalVigente > 0);
            viewModel.CarteiraVigenteVolume = pedidosComGestor.Sum(p => p.CapitalVigente);

            viewModel.CarteiraMoraNumero = 0;
            viewModel.CarteiraMoraVolume = 0;

            return viewModel;
        }

        public async Task FecharDiaEspecificoAsync(DateTime dataReferencia, int usuarioId)
        {
            var dataLimpa = dataReferencia.Date;

            // 1. OBTÉM OS DADOS DO DIA PARA CONGELAR NO SNAPSHOT
            var dadosFecho = await ObterDadosFechoDiarioAsync(dataLimpa);

            // 2. ATUALIZA OU CRIA O CONTROLO DO DIA
            var controlo = await _context.Set<DiaControloModel>()
                .FirstOrDefaultAsync(d => d.DataReferencia.Date == dataLimpa);

            if (controlo == null)
            {
                controlo = new DiaControloModel
                {
                    DataReferencia = dataLimpa,
                    EstaAberto = false,
                    DataFecho = DateTime.Now,
                    UsuarioFechoId = usuarioId,
                    DataAbertura = DateTime.Now,
                    UsuarioAberturaId = usuarioId
                };
                await _context.Set<DiaControloModel>().AddAsync(controlo);
            }
            else
            {
                controlo.EstaAberto = false;
                controlo.DataFecho = DateTime.Now;
                controlo.UsuarioFechoId = usuarioId;
                _context.Set<DiaControloModel>().Update(controlo);
            }

            // 3. GRAVA O SNAPSHOT COMPLETO NA BASE DE DADOS
            var registoExistente = await _context.FechosDiariosRegistos
                .Include(f => f.SaldosContas)
                .Include(f => f.MapaDesempenho)
                .FirstOrDefaultAsync(f => f.DataFecho.Date == dataLimpa);

            if (registoExistente != null)
            {
                _context.FechosDiariosContas.RemoveRange(registoExistente.SaldosContas);
                _context.FechosDiariosGestores.RemoveRange(registoExistente.MapaDesempenho);
                _context.FechosDiariosRegistos.Remove(registoExistente);
            }

            var novoRegisto = new FechoDiarioRegisto
            {
                DataFecho = dataLimpa,
                DataHoraProcessamento = DateTime.Now,
                UsuarioId = usuarioId,
                TotalDespesasVolume = dadosFecho.TotalDespesasVolume,
                TotalDespesasNumero = dadosFecho.TotalDespesasNumero,
                TotalDesembolsosVolume = dadosFecho.TotalDesembolsosVolume,
                TotalDesembolsosNumero = dadosFecho.TotalDesembolsosNumero,
                TotalReembolsosVolume = dadosFecho.TotalReembolsosVolume,
                TotalReembolsosNumero = dadosFecho.TotalReembolsosNumero,
                PedidosPendentesNumero = dadosFecho.PedidosPendentesNumero,
                PedidosAprovadosNumero = dadosFecho.PedidosAprovadosNumero,
                CarteiraVigenteNumero = dadosFecho.CarteiraVigenteNumero,
                CarteiraVigenteVolume = dadosFecho.CarteiraVigenteVolume,
                SaldosContas = dadosFecho.SaldosContas.Select(c => new FechoDiarioConta
                {
                    Banco = c.Banco,
                    Conta = c.Conta,
                    Titular = c.Titular,
                    SaldoAbertura = c.SaldoAbertura,
                    Creditos = c.Creditos,
                    Debitos = c.Debitos,
                    SaldoFecho = c.SaldoFecho
                }).ToList(),
                MapaDesempenho = dadosFecho.MapaDesempenho.Select(g => new FechoDiarioGestor
                {
                    GestorNome = g.GestorNome,
                    DesembolsoMontante = g.DesembolsoMontante,
                    QtdNovos = g.QtdNovos,
                    QtdRep = g.QtdRep,
                    VigenteQtd = g.VigenteQtd,
                    VigenteCapital = g.VigenteCapital,
                    ReembolsoCapital = g.ReembolsoCapital,
                    ReembolsoQtd = g.ReembolsoQtd,
                    MoraHojeCapital = g.MoraHojeCapital,
                    MoraTotalQtd = g.MoraTotalQtd,
                    MoraTotalCapital = g.MoraTotalCapital,
                    MoraTotalPercentual = g.MoraTotalPercentual,
                    Dias7Capital = g.Dias7Capital,
                    Dias7Qtd = g.Dias7Qtd,
                    Dias15Capital = g.Dias15Capital,
                    Dias15Qtd = g.Dias15Qtd,
                    Dias30Capital = g.Dias30Capital,
                    Dias30Qtd = g.Dias30Qtd
                }).ToList()
            };

            await _context.FechosDiariosRegistos.AddAsync(novoRegisto);
            await _context.SaveChangesAsync();
        }
    }
}
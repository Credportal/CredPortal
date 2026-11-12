using Analise.Data;
using Analise.Enuns;
using Analise.Filters;
using Analise.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    [PermissaoPerfil(PerfilEnum.Admin, PerfilEnum.Super, PerfilEnum.Gerente)]
    public class FechoDiarioController : Controller
    {
        private readonly BancoContext _bancoContext;

        public FechoDiarioController(BancoContext bancoContext)
        {
            _bancoContext = bancoContext;
        }

        // GET: /FechoDiario/Index?data=YYYY-MM-DD
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? data)
        {
            DateTime dataReferencia = data ?? DateTime.Today;

            // 1. Verificar se o dia já foi fechado e gravado na base de dados
            var fechoExistente = await _bancoContext.FechosDiariosRegistos
                .Include(r => r.SaldosContas)
                .Include(r => r.ResumoGestores)
                .FirstOrDefaultAsync(r => r.DataReferencia.Date == dataReferencia.Date);

            var viewModel = new FechoDiarioViewModel
            {
                DataReferencia = dataReferencia,
                DiaFechado = fechoExistente != null && fechoExistente.Fechado,
                ListaUsuarios = await _bancoContext.Usuarios.AsNoTracking().ToListAsync()
            };

            if (fechoExistente != null)
            {
                // Se já estiver fechado, carrega os dados congelados/persistidos da BD
                viewModel.TotalDespesasDiaVolume = fechoExistente.TotalDespesasVolume;
                viewModel.TotalDespesasDiaNr = fechoExistente.TotalDespesasNumero;
                viewModel.TotalDesembolsosVolume = fechoExistente.TotalDesembolsosVolume;
                viewModel.TotalDesembolsosNumero = fechoExistente.TotalDesembolsosNumero;
                viewModel.TotalReembolsosVolume = fechoExistente.TotalReembolsosVolume;
                viewModel.TotalReembolsosNumero = fechoExistente.TotalReembolsosNumero;
                viewModel.PedidosPendentesNumero = fechoExistente.PedidosPendentesNumero;
                viewModel.PedidosAprovadosNumero = fechoExistente.PedidosAprovadosNumero;
                viewModel.CarteiraVigenteTotalVolume = fechoExistente.CarteiraVigenteTotalVolume;
                viewModel.CarteiraVigenteTotalNr = fechoExistente.CarteiraVigenteTotalNr;

                viewModel.SaldosContas = fechoExistente.SaldosContas.Select(c => new FechoDiarioContaViewModel
                {
                    NomeConta = $"{c.Banco} - {c.Conta} ({c.Titular})",
                    SaldoAbertura = c.SaldoAbertura,
                    CreditosDia = c.Creditos,
                    DebitosDia = c.Debitos,
                    SaldoFecho = c.SaldoFecho
                }).ToList();

                viewModel.ResumoGestores = fechoExistente.ResumoGestores.Select(g => new FechoDiarioGestorViewModel
                {
                    GestorNome = g.GestorNome,
                    DesembolsoVolume = g.DesembolsoVolume,
                    DesembolsoNr = g.DesembolsoNr,
                    ReembolsoVolume = g.ReembolsoVolume,
                    ReembolsoNr = g.ReembolsoNr,
                    PedidosPendentesVolume = g.PedidosPendentesVolume,
                    PedidosPendentesNr = g.PedidosPendentesNr,
                    PedidosAprovadosVolume = g.PedidosAprovadosVolume,
                    PedidosAprovadosNr = g.PedidosAprovadosNr,
                    JurosRecebidosVolume = g.JurosRecebidosVolume,
                    JurosRecebidosNr = g.JurosRecebidosNr,
                    VigenteVolume = g.VigenteVolume,
                    VigenteNr = g.VigenteNr,
                    MoraMenor7Volume = g.MoraMenor7Volume,
                    MoraMenor7Nr = g.MoraMenor7Nr,
                    MoraMenor15Volume = g.MoraMenor15Volume,
                    MoraMenor15Nr = g.MoraMenor15Nr,
                    MoraMenor30Volume = g.MoraMenor30Volume,
                    MoraMenor30Nr = g.MoraMenor30Nr,
                    MoraMenor60Volume = g.MoraMenor60Volume,
                    MoraMenor60Nr = g.MoraMenor60Nr,
                    MoraMenor90Volume = g.MoraMenor90Volume,
                    MoraMenor90Nr = g.MoraMenor90Nr,
                    MoraMaior90Volume = g.MoraMaior90Volume,
                    MoraMaior90Nr = g.MoraMaior90Nr
                }).ToList();

                viewModel.CarteiraMoraGestores = viewModel.ResumoGestores;
            }
            else
            {
                // 2. BUSCA REAL DA BASE DE DADOS (Dinâmico para a data selecionada)

                // A) Saldos e Movimentos de Contas
                var contasDb = await _bancoContext.Contas.ToListAsync();
                foreach (var conta in contasDb)
                {
                    decimal creditosDia = await _bancoContext.MovimentosInternos
                        .Where(m => m.ContaDestinoId == conta.Id && m.DataCadastro.Date == dataReferencia.Date)
                        .SumAsync(m => (decimal?)m.Credito) ?? 0;

                    decimal debitosDia = await _bancoContext.MovimentosInternos
                        .Where(m => m.ContaOrigemId == conta.Id && m.DataCadastro.Date == dataReferencia.Date)
                        .SumAsync(m => (decimal?)m.Debito) ?? 0;

                    viewModel.SaldosContas.Add(new FechoDiarioContaViewModel
                    {
                        NomeConta = $"{conta.Banco ?? "Banco"} - {conta.Conta ?? conta.Conta}",
                        SaldoAbertura = conta.Saldo - creditosDia + debitosDia,
                        CreditosDia = creditosDia,
                        DebitosDia = debitosDia,
                        SaldoFecho = conta.Saldo
                    });
                }

                // B) Despesas do Dia
                var despesasDia = await _bancoContext.MovimentosExternos
                    .Where(m => m.TipoOperacao == "Despesa" && m.DataCadastro.Date == dataReferencia.Date)
                    .ToListAsync();

                viewModel.TotalDespesasDiaVolume = despesasDia.Sum(d => d.Debito);
                viewModel.TotalDespesasDiaNr = despesasDia.Count;

                // C) Carregar Pedidos e Amortizações para cruzamento com Gestores e Mora
                var pedidosDb = await _bancoContext.Pedidos
                    .Include(p => p.Gestor)
                    .ToListAsync();

                var amortizacoesDb = await _bancoContext.Amortizacoes
                    .Include(a => a.Pedido)
                    .ThenInclude(p => p.Gestor)
                    .ToListAsync();

                var gestoresNomes = pedidosDb
                    .Select(p => p.Gestor != null ? p.Gestor.Nome : $"Gestor #{p.GestorId}")
                    .Distinct()
                    .ToList();

                foreach (var gestorNome in gestoresNomes)
                {
                    var pedidosGestor = pedidosDb.Where(p => (p.Gestor != null ? p.Gestor.Nome : $"Gestor #{p.GestorId}") == gestorNome).ToList();

                    var desembolsosGestor = pedidosGestor
                        .Where(p => p.DataCadastro.Date == dataReferencia.Date && p.EstadoId == 2)
                        .ToList();

                    var amortizacoesDiaGestor = amortizacoesDb
                        .Where(a => a.Pedido != null &&
                                    (a.Pedido.Gestor != null ? a.Pedido.Gestor.Nome : $"Gestor #{a.Pedido.GestorId}") == gestorNome &&
                                    a.DataPagamento.HasValue && a.DataPagamento.Value.Date == dataReferencia.Date)
                        .ToList();

                    var pedidosVigentesGestor = pedidosGestor
                        .Where(p => p.CapitalVigente > 0m)
                        .ToList();

                    var amortizacoesMoraGestor = amortizacoesDb
                        .Where(a => a.Pedido != null &&
                                    (a.Pedido.Gestor != null ? a.Pedido.Gestor.Nome : $"Gestor #{a.Pedido.GestorId}") == gestorNome &&
                                    a.Vencimento.Date < dataReferencia.Date &&
                                    a.PrincipalPorPagar > 0m)
                        .ToList();

                    int ObterDiasAtraso(DateTime vencimento) =>
                        dataReferencia.Date > vencimento.Date ? (dataReferencia.Date - vencimento.Date).Days : 0;

                    var mora7 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) <= 7).ToList();
                    var mora15 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 7 && ObterDiasAtraso(a.Vencimento) <= 15).ToList();
                    var mora30 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 15 && ObterDiasAtraso(a.Vencimento) <= 30).ToList();
                    var mora60 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 30 && ObterDiasAtraso(a.Vencimento) <= 60).ToList();
                    var mora90 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 60 && ObterDiasAtraso(a.Vencimento) <= 90).ToList();
                    var moraMaior90 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 90).ToList();

                    viewModel.ResumoGestores.Add(new FechoDiarioGestorViewModel
                    {
                        GestorNome = gestorNome,
                        DesembolsoVolume = desembolsosGestor.Sum(d => (decimal?)d.ValorDesembolsado) ?? 0m,
                        DesembolsoNr = desembolsosGestor.Count,
                        ReembolsoVolume = amortizacoesDiaGestor.Sum(a => (decimal?)a.TotalPago) ?? 0m,
                        ReembolsoNr = amortizacoesDiaGestor.Count,
                        PedidosPendentesVolume = pedidosGestor.Where(p => p.EstadoId == 1).Sum(p => (decimal?)p.Valor) ?? 0m,
                        PedidosPendentesNr = pedidosGestor.Count(p => p.EstadoId == 1),
                        PedidosAprovadosVolume = pedidosGestor.Where(p => p.EstadoId == 2).Sum(p => (decimal?)p.ValorDecisao) ?? 0m,
                        PedidosAprovadosNr = pedidosGestor.Count(p => p.EstadoId == 2),
                        JurosRecebidosVolume = amortizacoesDiaGestor.Sum(a => (decimal?)a.JuroPago) ?? 0m,
                        JurosRecebidosNr = amortizacoesDiaGestor.Count,

                        VigenteVolume = pedidosVigentesGestor.Sum(p => (decimal?)p.CapitalVigente) ?? 0m,
                        VigenteNr = pedidosVigentesGestor.Count,

                        MoraMenor7Volume = mora7.Sum(a => a.PrincipalPorPagar),
                        MoraMenor7Nr = mora7.Select(a => a.PedidoId).Distinct().Count(),
                        MoraMenor15Volume = mora15.Sum(a => a.PrincipalPorPagar),
                        MoraMenor15Nr = mora15.Select(a => a.PedidoId).Distinct().Count(),
                        MoraMenor30Volume = mora30.Sum(a => a.PrincipalPorPagar),
                        MoraMenor30Nr = mora30.Select(a => a.PedidoId).Distinct().Count(),
                        MoraMenor60Volume = mora60.Sum(a => a.PrincipalPorPagar),
                        MoraMenor60Nr = mora60.Select(a => a.PedidoId).Distinct().Count(),
                        MoraMenor90Volume = mora90.Sum(a => a.PrincipalPorPagar),
                        MoraMenor90Nr = mora90.Select(a => a.PedidoId).Distinct().Count(),
                        MoraMaior90Volume = moraMaior90.Sum(a => a.PrincipalPorPagar),
                        MoraMaior90Nr = moraMaior90.Select(a => a.PedidoId).Distinct().Count()
                    });
                }

                viewModel.CarteiraMoraGestores = viewModel.ResumoGestores;
                viewModel.CarteiraVigenteTotalVolume = viewModel.ResumoGestores.Sum(x => x.VigenteVolume);
                viewModel.CarteiraVigenteTotalNr = viewModel.ResumoGestores.Sum(x => x.VigenteNr);
            }

            return View(viewModel);
        }

        // POST: /FechoDiario/FecharDia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FecharDia(DateTime dataFecho)
        {
            var jaExiste = await _bancoContext.FechosDiariosRegistos
                .AnyAsync(r => r.DataReferencia.Date == dataFecho.Date);

            if (jaExiste)
            {
                TempData["MensagemErro"] = "O fecho para esta data já foi realizado e gravado anteriormente.";
                return RedirectToAction("Index", new { data = dataFecho.ToString("yyyy-MM-dd") });
            }

            using var transaction = await _bancoContext.Database.BeginTransactionAsync();
            try
            {
                var contasDb = await _bancoContext.Contas.ToListAsync();
                var despesasDia = await _bancoContext.MovimentosExternos
                    .Where(m => m.TipoOperacao == "Despesa" && m.DataCadastro.Date == dataFecho.Date)
                    .ToListAsync();

                var novoFecho = new FechoDiarioRegisto
                {
                    DataReferencia = dataFecho,
                    DataFecho = DateTime.Now,
                    UsuarioFecho = User.Identity?.Name ?? "Admin",
                    Fechado = true,
                    TotalDespesasVolume = despesasDia.Sum(d => d.Debito),
                    TotalDespesasNumero = despesasDia.Count,
                    TotalDesembolsosVolume = 0,
                    TotalDesembolsosNumero = 0,
                    TotalReembolsosVolume = 0,
                    TotalReembolsosNumero = 0,
                    PedidosPendentesNumero = 0,
                    PedidosAprovadosNumero = 0,
                    CarteiraVigenteTotalVolume = contasDb.Sum(c => c.Saldo),
                    CarteiraVigenteTotalNr = contasDb.Count
                };

                _bancoContext.FechosDiariosRegistos.Add(novoFecho);
                await _bancoContext.SaveChangesAsync();

                foreach (var c in contasDb)
                {
                    _bancoContext.FechosDiariosContas.Add(new FechoDiarioConta
                    {
                        FechoDiarioRegistoId = novoFecho.Id,
                        Banco = c.Banco ?? "Banco",
                        Conta = c.Conta ?? "Conta",
                        Titular = c.Titular ?? "Titular",
                        SaldoAbertura = c.Saldo,
                        Creditos = 0,
                        Debitos = 0,
                        SaldoFecho = c.Saldo
                    });
                }

                await _bancoContext.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensagemSucesso"] = $"Fecho do dia {dataFecho:dd/MM/yyyy} gravado com sucesso!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["MensagemErro"] = "Erro ao gravar o fecho diário: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Index", new { data = dataFecho.ToString("yyyy-MM-dd") });
        }

        // GET: /FechoDiario/GerarMapaContasPdf?data=YYYY-MM-DD
        [HttpGet]
        public async Task<IActionResult> GerarMapaContasPdf(DateTime? data)
        {
            DateTime dataReferencia = data ?? DateTime.Today;

            var contasDb = await _bancoContext.Contas.ToListAsync();
            var despesasDia = await _bancoContext.MovimentosExternos
                .Where(m => m.TipoOperacao == "Despesa" && m.DataCadastro.Date == dataReferencia.Date)
                .ToListAsync();

            var saldosContas = new List<FechoDiarioContaViewModel>();
            foreach (var conta in contasDb)
            {
                decimal creditosDia = await _bancoContext.MovimentosInternos
                    .Where(m => m.ContaDestinoId == conta.Id && m.DataCadastro.Date == dataReferencia.Date)
                    .SumAsync(m => (decimal?)m.Credito) ?? 0m;

                decimal debitosDia = await _bancoContext.MovimentosInternos
                    .Where(m => m.ContaOrigemId == conta.Id && m.DataCadastro.Date == dataReferencia.Date)
                    .SumAsync(m => (decimal?)m.Debito) ?? 0m;

                saldosContas.Add(new FechoDiarioContaViewModel
                {
                    NomeConta = $"{conta.Banco ?? "Banco"} - {conta.Conta ?? conta.Conta}",
                    SaldoAbertura = conta.Saldo - creditosDia + debitosDia,
                    CreditosDia = creditosDia,
                    DebitosDia = debitosDia,
                    SaldoFecho = conta.Saldo
                });
            }

            decimal totalDespesasVolume = despesasDia.Sum(d => d.Debito);
            int totalDespesasNr = despesasDia.Count;

            var culturaPt = new System.Globalization.CultureInfo("pt-PT");
            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9).FontColor("#172b4d"));

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("MAPA DE CONTAS E CAIXA — ").FontSize(7.5f).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(7.5f).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(18).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(8.5f).FontColor("#718096");
                            });
                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data Ref: {dataReferencia:dd/MM/yyyy}").Bold().FontSize(9).AlignRight();
                                c.Item().Text($"Gerado em: {DateTime.Now:HH:mm dd/MM/yyyy}").FontSize(8f).Italic().AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(3).LineHorizontal(1).LineColor("#079b62");
                        col.Item().Background("#079b62").Padding(6).AlignCenter().Text("RESUMO DE CAIXA E CONTAS BANCÁRIAS")
                            .Bold().FontSize(11).FontColor("#FFFFFF");

                        col.Item().PaddingTop(5);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#edf2f7").Padding(5).Text("Banco / Conta").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(5).AlignRight().Text("Saldo Abertura").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(5).AlignRight().Text("Créditos (+)").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(5).AlignRight().Text("Débitos (-)").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(5).AlignRight().Text("Saldo Fecho").Bold().FontSize(8.5f);
                            });

                            foreach (var c in saldosContas)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(c.NomeConta).FontSize(8.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text($"{c.SaldoAbertura:N2}").FontSize(8.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text($"+ {c.CreditosDia:N2}").FontColor("#079b62").FontSize(8.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text($"- {c.DebitosDia:N2}").FontColor("#c53030").FontSize(8.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text($"{c.SaldoFecho:N2}").Bold().FontSize(8.5f);
                            }

                            table.Cell().Background("#f7fafc").Padding(5).Text("TOTAL GERAL").Bold().FontSize(8.5f);
                            table.Cell().Background("#f7fafc").Padding(5).AlignRight().Text($"{saldosContas.Sum(x => x.SaldoAbertura):N2}").Bold().FontSize(8.5f);
                            table.Cell().Background("#f7fafc").Padding(5).AlignRight().Text($"{saldosContas.Sum(x => x.CreditosDia):N2}").Bold().FontColor("#079b62").FontSize(8.5f);
                            table.Cell().Background("#f7fafc").Padding(5).AlignRight().Text($"{saldosContas.Sum(x => x.DebitosDia):N2}").Bold().FontColor("#c53030").FontSize(8.5f);
                            table.Cell().Background("#f7fafc").Padding(5).AlignRight().Text($"{saldosContas.Sum(x => x.SaldoFecho):N2}").Bold().FontSize(8.5f);
                        });

                        col.Item().PaddingTop(10);

                        col.Item().Background("#1e293b").Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL DESPESAS DO DIA:").Bold().FontColor("#FFFFFF").FontSize(10);
                            row.ConstantItem(200).AlignRight().Text($"MT {totalDespesasVolume:N2} (Nº: {totalDespesasNr})").Bold().FontColor("#f6ad55").FontSize(10);
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Mapa_Contas_{dataReferencia:yyyyMMdd}.pdf");
        }

        // GET: /FechoDiario/GerarMapaDesempenhoPdf?data=YYYY-MM-DD
        [HttpGet]
        public async Task<IActionResult> GerarMapaDesempenhoPdf(DateTime? data)
        {
            DateTime dataReferencia = data ?? DateTime.Today;

            var pedidosDb = await _bancoContext.Pedidos.Include(p => p.Gestor).ToListAsync();
            var amortizacoesDb = await _bancoContext.Amortizacoes.Include(a => a.Pedido).ThenInclude(p => p.Gestor).ToListAsync();

            var gestoresNomes = pedidosDb
                .Select(p => p.Gestor != null ? p.Gestor.Nome : $"Gestor #{p.GestorId}")
                .Distinct()
                .ToList();

            var resumoGestores = new List<FechoDiarioGestorViewModel>();

            foreach (var gestorNome in gestoresNomes)
            {
                var pedidosGestor = pedidosDb.Where(p => (p.Gestor != null ? p.Gestor.Nome : $"Gestor #{p.GestorId}") == gestorNome).ToList();

                var desembolsosGestor = pedidosGestor.Where(p => p.DataCadastro.Date == dataReferencia.Date && p.EstadoId == 2).ToList();
                var amortizacoesDiaGestor = amortizacoesDb.Where(a => a.Pedido != null && (a.Pedido.Gestor != null ? a.Pedido.Gestor.Nome : $"Gestor #{a.Pedido.GestorId}") == gestorNome && a.DataPagamento.HasValue && a.DataPagamento.Value.Date == dataReferencia.Date).ToList();
                var pedidosVigentesGestor = pedidosGestor.Where(p => p.CapitalVigente > 0m).ToList();

                var amortizacoesMoraGestor = amortizacoesDb.Where(a => a.Pedido != null && (a.Pedido.Gestor != null ? a.Pedido.Gestor.Nome : $"Gestor #{a.Pedido.GestorId}") == gestorNome && a.Vencimento.Date < dataReferencia.Date && a.PrincipalPorPagar > 0m).ToList();

                int ObterDiasAtraso(DateTime vencimento) => dataReferencia.Date > vencimento.Date ? (dataReferencia.Date - vencimento.Date).Days : 0;

                var mora7 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) <= 7).ToList();
                var mora15 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 7 && ObterDiasAtraso(a.Vencimento) <= 15).ToList();
                var mora30 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 15 && ObterDiasAtraso(a.Vencimento) <= 30).ToList();
                var mora60 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 30 && ObterDiasAtraso(a.Vencimento) <= 60).ToList();
                var mora90 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 60 && ObterDiasAtraso(a.Vencimento) <= 90).ToList();
                var moraMaior90 = amortizacoesMoraGestor.Where(a => ObterDiasAtraso(a.Vencimento) > 90).ToList();

                resumoGestores.Add(new FechoDiarioGestorViewModel
                {
                    GestorNome = gestorNome,
                    DesembolsoVolume = desembolsosGestor.Sum(d => (decimal?)d.ValorDesembolsado) ?? 0m,
                    ReembolsoVolume = amortizacoesDiaGestor.Sum(a => (decimal?)a.TotalPago) ?? 0m,
                    PedidosPendentesVolume = pedidosGestor.Where(p => p.EstadoId == 1).Sum(p => (decimal?)p.Valor) ?? 0m,
                    PedidosAprovadosVolume = pedidosGestor.Where(p => p.EstadoId == 2).Sum(p => (decimal?)p.ValorDecisao) ?? 0m,
                    JurosRecebidosVolume = amortizacoesDiaGestor.Sum(a => (decimal?)a.JuroPago) ?? 0m,
                    VigenteVolume = pedidosVigentesGestor.Sum(p => (decimal?)p.CapitalVigente) ?? 0m,
                    MoraMenor7Volume = mora7.Sum(a => a.PrincipalPorPagar),
                    MoraMenor15Volume = mora15.Sum(a => a.PrincipalPorPagar),
                    MoraMenor30Volume = mora30.Sum(a => a.PrincipalPorPagar),
                    MoraMenor60Volume = mora60.Sum(a => a.PrincipalPorPagar),
                    MoraMenor90Volume = mora90.Sum(a => a.PrincipalPorPagar),
                    MoraMaior90Volume = moraMaior90.Sum(a => a.PrincipalPorPagar)
                });
            }

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4.Landscape());
                    page.Margin(15);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(7.5f).FontColor("#172b4d"));

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("MAPA DE DESEMPENHO E FECHO DIÁRIO — ").FontSize(7f).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(7f).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(5);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("MAPA DE DESEMPENHO E FECHO DO DIA").Bold().FontSize(13).FontColor("#079b62");
                                c.Item().Text($"Data de Referência: {dataReferencia:dd/MM/yyyy} | Empresa: {nomeEmpresa}").FontSize(8f).FontColor("#718096");
                            });
                            row.ConstantItem(250).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Gerado em: {DateTime.Now:HH:mm:ss dd/MM/yyyy}").FontSize(7.5f).Italic().AlignRight();
                                c.Item().Text("Operador: Admin").FontSize(7.5f).FontColor("#718096").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(2).LineHorizontal(1).LineColor("#079b62");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(2.2f);
                                columns.RelativeColumn(2.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#1e293b").Padding(4).Text("GESTOR").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                                header.Cell().Background("#1e293b").Padding(4).AlignRight().Text("Desembolso").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                                header.Cell().Background("#1e293bニング").Padding(4).AlignRight().Text("Reembolso").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                                header.Cell().Background("#1e293b").Padding(4).AlignRight().Text("Pendentes").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                                header.Cell().Background("#1e293b").Padding(4).AlignRight().Text("Aprovados").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                                header.Cell().Background("#1e293b").Padding(4).AlignRight().Text("Juros Rec.").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                                header.Cell().Background("#1e293b").Padding(4).AlignRight().Text("Vigente").Bold().FontColor("#FFFFFF").FontSize(7.5f);
                            });

                            foreach (var g in resumoGestores)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(g.GestorNome).Bold().FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{g.DesembolsoVolume:N2}").FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{g.ReembolsoVolume:N2}").FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{g.PedidosPendentesVolume:N2}").FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{g.PedidosAprovadosVolume:N2}").FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{g.JurosRecebidosVolume:N2}").FontSize(7.5f);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{g.VigenteVolume:N2}").Bold().FontSize(7.5f);
                            }
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Mapa_Desempenho_{dataReferencia:yyyyMMdd}.pdf");
        }
    }
}
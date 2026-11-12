using Analise.Data;
using Analise.Enuns;
using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    [PermissaoPerfil(PerfilEnum.Admin, PerfilEnum.Super, PerfilEnum.Gerente)]
    public class FechoDiarioController : Controller
    {
        private readonly BancoContext _bancoContext;
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly ISessao _sessao;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public FechoDiarioController(BancoContext bancoContext,
             IWebHostEnvironment webHostEnvironment, IUsuarioRepositorio usuarioRepositorio,
             ISessao sessao)
        {
            _webHostEnvironment = webHostEnvironment;
            _bancoContext = bancoContext;
            _sessao = sessao;
            _usuarioRepositorio = usuarioRepositorio;
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
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

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
            string numeroRef = $"MCTA-{dataReferencia:yyyyMMdd}";

            // --- CARREGAR O LOGOTIPO E A MARCA D'ÁGUA DO DISCO ---
            byte[] logoBytes = null;
            string caminhoLogo = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");
            if (System.IO.File.Exists(caminhoLogo))
            {
                logoBytes = System.IO.File.ReadAllBytes(caminhoLogo);
            }

            byte[] marcaDaguaBytes = null;
            string caminhoMarcaDagua = Path.Combine(_webHostEnvironment.WebRootPath, "images", "MarcaAgua.png");
            if (System.IO.File.Exists(caminhoMarcaDagua))
            {
                marcaDaguaBytes = System.IO.File.ReadAllBytes(caminhoMarcaDagua);
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(30);
                    page.MarginBottom(75);
                    page.MarginLeft(30);
                    page.MarginRight(30);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9).FontColor("#172b4d"));

                    // Rodapé Institucional Oficial Esticado
                    page.Footer().AlignCenter().Column(colRodape =>
                    {
                        colRodape.Item().LineHorizontal(1f).LineColor("#b89735");
                        colRodape.Item().PaddingTop(4).Text(PadraoTimbradoComponent.RodapeTimbrado).FontSize(10f).FontColor("#079b62").Bold();
                    });

                    // --- CAMADAS (Marca d'água no fundo + Papel Timbrado Padrão) ---
                    page.Content().Layers(layers =>
                    {
                        if (marcaDaguaBytes != null)
                        {
                            layers.Layer().AlignCenter().AlignMiddle().Column(c =>
                            {
                                c.Item().Width(420).Image(marcaDaguaBytes);
                            });
                        }

                        layers.PrimaryLayer().Component(new PadraoTimbradoComponent(
                            "RESUMO DE CAIXA E CONTAS BANCÁRIAS",
                            numeroRef,
                            logoBytes,
                            marcaDaguaBytes,
                            conteudo =>
                            {
                                conteudo.Column(col =>
                                {
                                    col.Spacing(8);

                                    col.Item().Text(t =>
                                    {
                                        t.Span("Data de Referência: ").Bold();
                                        t.Span($"{dataReferencia.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}");
                                    });

                                    col.Item().PaddingTop(3);

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
                                            header.Cell().Background("#079b62").Padding(5).Text("Banco / Conta").Bold().FontSize(8.5f).FontColor("#FFFFFF");
                                            header.Cell().Background("#079b62").Padding(5).AlignRight().Text("Saldo Abertura").Bold().FontSize(8.5f).FontColor("#FFFFFF");
                                            header.Cell().Background("#079b62").Padding(5).AlignRight().Text("Créditos (+)").Bold().FontSize(8.5f).FontColor("#FFFFFF");
                                            header.Cell().Background("#079b62").Padding(5).AlignRight().Text("Débitos (-)").Bold().FontSize(8.5f).FontColor("#FFFFFF");
                                            header.Cell().Background("#079b62").Padding(5).AlignRight().Text("Saldo Fecho").Bold().FontSize(8.5f).FontColor("#FFFFFF");
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

                                    col.Item().PaddingTop(12);

                                    col.Item().Background("#1e293b").Padding(8).Row(row =>
                                    {
                                        row.RelativeItem().Text("TOTAL DESPESAS DO DIA:").Bold().FontColor("#FFFFFF").FontSize(10);
                                        row.ConstantItem(200).AlignRight().Text($"MT {totalDespesasVolume:N2} (Nº: {totalDespesasNr})").Bold().FontColor("#f6ad55").FontSize(10);
                                    });
                                });
                            }));
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Mapa_Contas_{dataReferencia:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> GerarMapaDesempenhoPdf(DateTime? data)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

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

                // Clientes únicos na carteira vigente para a contagem correta
                int vigenteCount = pedidosVigentesGestor.Select(p => p.EntidadeId).Distinct().Count();

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
                    DesembolsoNr = desembolsosGestor.Count,

                    ReembolsoVolume = amortizacoesDiaGestor.Sum(a => (decimal?)a.TotalPago) ?? 0m,
                    ReembolsoNr = amortizacoesDiaGestor.Count,

                    PedidosPendentesVolume = pedidosGestor.Where(p => p.EstadoId == 1).Sum(p => (decimal?)p.Valor) ?? 0m,
                    PedidosPendentesNr = pedidosGestor.Count(p => p.EstadoId == 1),

                    PedidosAprovadosVolume = pedidosGestor.Where(p => p.EstadoId == 2).Sum(p => (decimal?)p.ValorDecisao) ?? 0m,
                    PedidosAprovadosNr = pedidosGestor.Count(p => p.EstadoId == 2),

                    JurosRecebidosVolume = amortizacoesDiaGestor.Sum(a => (decimal?)a.JuroPago) ?? 0m,
                    JurosRecebidosNr = amortizacoesDiaGestor.Count(a => a.JuroPago > 0),

                    VigenteVolume = pedidosVigentesGestor.Sum(p => (decimal?)p.CapitalVigente) ?? 0m,
                    VigenteNr = vigenteCount,

                    MoraMenor7Volume = mora7.Sum(a => a.PrincipalPorPagar),
                    MoraMenor7Nr = mora7.Count,

                    MoraMenor15Volume = mora15.Sum(a => a.PrincipalPorPagar),
                    MoraMenor15Nr = mora15.Count,

                    MoraMenor30Volume = mora30.Sum(a => a.PrincipalPorPagar),
                    MoraMenor30Nr = mora30.Count,

                    MoraMenor60Volume = mora60.Sum(a => a.PrincipalPorPagar),
                    MoraMenor60Nr = mora60.Count,

                    MoraMenor90Volume = mora90.Sum(a => a.PrincipalPorPagar),
                    MoraMenor90Nr = mora90.Count,

                    MoraMaior90Volume = moraMaior90.Sum(a => a.PrincipalPorPagar),
                    MoraMaior90Nr = moraMaior90.Count
                });
            }

            string numeroProcesso = $"MD-{dataReferencia:yyyyMMdd}";

            // --- CARREGAR O LOGOTIPO E A MARCA D'ÁGUA DO DISCO ---
            byte[] logoBytes = null;
            string caminhoLogo = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png");
            if (System.IO.File.Exists(caminhoLogo))
            {
                logoBytes = System.IO.File.ReadAllBytes(caminhoLogo);
            }

            byte[] marcaDaguaBytes = null;
            string caminhoMarcaDagua = Path.Combine(_webHostEnvironment.WebRootPath, "images", "MarcaAgua.png");
            if (System.IO.File.Exists(caminhoMarcaDagua))
            {
                marcaDaguaBytes = System.IO.File.ReadAllBytes(caminhoMarcaDagua);
            }

            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.MarginTop(30);
                    page.MarginBottom(65);
                    page.MarginLeft(20);
                    page.MarginRight(20);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(6.5f).FontColor("#172b4d"));

                    // Rodapé Institucional Oficial Esticado
                    page.Footer().AlignCenter().Column(colRodape =>
                    {
                        colRodape.Item().LineHorizontal(1f).LineColor("#b89735");
                        colRodape.Item().PaddingTop(4).Text(PadraoTimbradoComponent.RodapeTimbrado).FontSize(9f).FontColor("#079b62").Bold();
                    });

                    // --- CAMADAS (Marca d'água no fundo + Papel Timbrado Padrão) ---
                    page.Content().Layers(layers =>
                    {
                        if (marcaDaguaBytes != null)
                        {
                            layers.Layer().AlignCenter().AlignMiddle().Column(c =>
                            {
                                c.Item().Width(420).Image(marcaDaguaBytes);
                            });
                        }

                        layers.PrimaryLayer().Component(new PadraoTimbradoComponent(
                            "MAPA DE DESEMPENHO E FECHO DO DIA",
                            numeroProcesso,
                            logoBytes,
                            marcaDaguaBytes,
                            conteudo =>
                            {
                                conteudo.Column(col =>
                                {
                                    col.Spacing(5);

                                    col.Item().Text(t =>
                                    {
                                        t.Span("Data de Referência: ").Bold();
                                        t.Span($"{dataReferencia.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}");
                                    });

                                    col.Item().PaddingTop(2);

                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2.2f); // GESTOR
                                            columns.RelativeColumn(1.6f); // Desembolso
                                            columns.RelativeColumn(1.6f); // Reembolso
                                            columns.RelativeColumn(1.6f); // Pendentes
                                            columns.RelativeColumn(1.6f); // Aprovados
                                            columns.RelativeColumn(1.6f); // Juros Rec.
                                            columns.RelativeColumn(1.9f); // Vigente
                                            columns.RelativeColumn(1.5f); // <7d
                                            columns.RelativeColumn(1.5f); // <15d
                                            columns.RelativeColumn(1.5f); // <30d
                                            columns.RelativeColumn(1.5f); // <60d
                                            columns.RelativeColumn(1.5f); // <90d
                                            columns.RelativeColumn(1.5f); // >90d
                                            columns.RelativeColumn(1.7f); // Mora Total
                                            columns.RelativeColumn(1.3f); // % Mora
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background("#079b62").Padding(3).Text("GESTOR").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#079b62").Padding(3).AlignRight().Text("Desembolso").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#079b62").Padding(3).AlignRight().Text("Reembolso").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#079b62").Padding(3).AlignRight().Text("Pendentes").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#079b62").Padding(3).AlignRight().Text("Aprovados").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#079b62").Padding(3).AlignRight().Text("Juros Rec.").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#079b62").Padding(3).AlignRight().Text("Vigente").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("<7d").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("<15d").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("<30d").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("< 60d").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("< 90d").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("> 90d").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("Mora Total").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                            header.Cell().Background("#c53030").Padding(3).AlignRight().Text("% Mora").Bold().FontColor("#FFFFFF").FontSize(6.5f);
                                        });

                                        foreach (var g in resumoGestores)
                                        {
                                            decimal moraTotalGestor = g.MoraMenor7Volume + g.MoraMenor15Volume + g.MoraMenor30Volume + g.MoraMenor60Volume + g.MoraMenor90Volume + g.MoraMaior90Volume;
                                            int moraTotalNr = g.MoraMenor7Nr + g.MoraMenor15Nr + g.MoraMenor30Nr + g.MoraMenor60Nr + g.MoraMenor90Nr + g.MoraMaior90Nr;
                                            decimal pctMoraGestor = g.VigenteVolume > 0 ? (moraTotalGestor / g.VigenteVolume) * 100m : 0m;

                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).Text(g.GestorNome).Bold().FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.DesembolsoVolume:N2}/{g.DesembolsoNr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.ReembolsoVolume:N2}/{g.ReembolsoNr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.PedidosPendentesVolume:N2}/{g.PedidosPendentesNr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.PedidosAprovadosVolume:N2}/{g.PedidosAprovadosNr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.JurosRecebidosVolume:N2}/{g.JurosRecebidosNr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.VigenteVolume:N2}/{g.VigenteNr}").Bold().FontSize(6.5f);

                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.MoraMenor7Volume:N2}/{g.MoraMenor7Nr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.MoraMenor15Volume:N2}/{g.MoraMenor15Nr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.MoraMenor30Volume:N2}/{g.MoraMenor30Nr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.MoraMenor60Volume:N2}/{g.MoraMenor60Nr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.MoraMenor90Volume:N2}/{g.MoraMenor90Nr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{g.MoraMaior90Volume:N2}/{g.MoraMaior90Nr}").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{moraTotalGestor:N2}/{moraTotalNr}").Bold().FontColor("#c53030").FontSize(6.5f);
                                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(3).AlignRight().Text($"{pctMoraGestor:N2}%").Bold().FontColor("#c53030").FontSize(6.5f);
                                        }

                                        // --- LINHA DE TOTAL GERAL ---
                                        decimal totDesembolso = resumoGestores.Sum(x => x.DesembolsoVolume);
                                        int totDesembolsoNr = resumoGestores.Sum(x => x.DesembolsoNr);

                                        decimal totReembolso = resumoGestores.Sum(x => x.ReembolsoVolume);
                                        int totReembolsoNr = resumoGestores.Sum(x => x.ReembolsoNr);

                                        decimal totPendentes = resumoGestores.Sum(x => x.PedidosPendentesVolume);
                                        int totPendentesNr = resumoGestores.Sum(x => x.PedidosPendentesNr);

                                        decimal totAprovados = resumoGestores.Sum(x => x.PedidosAprovadosVolume);
                                        int totAprovadosNr = resumoGestores.Sum(x => x.PedidosAprovadosNr);

                                        decimal totJuros = resumoGestores.Sum(x => x.JurosRecebidosVolume);
                                        int totJurosNr = resumoGestores.Sum(x => x.JurosRecebidosNr);

                                        decimal totVigente = resumoGestores.Sum(x => x.VigenteVolume);
                                        int totVigenteNr = resumoGestores.Sum(x => x.VigenteNr);

                                        decimal totM7 = resumoGestores.Sum(x => x.MoraMenor7Volume);
                                        int totM7Nr = resumoGestores.Sum(x => x.MoraMenor7Nr);

                                        decimal totM15 = resumoGestores.Sum(x => x.MoraMenor15Volume);
                                        int totM15Nr = resumoGestores.Sum(x => x.MoraMenor15Nr);

                                        decimal totM30 = resumoGestores.Sum(x => x.MoraMenor30Volume);
                                        int totM30Nr = resumoGestores.Sum(x => x.MoraMenor30Nr);

                                        decimal totM60 = resumoGestores.Sum(x => x.MoraMenor60Volume);
                                        int totM60Nr = resumoGestores.Sum(x => x.MoraMenor60Nr);

                                        decimal totM90 = resumoGestores.Sum(x => x.MoraMenor90Volume);
                                        int totM90Nr = resumoGestores.Sum(x => x.MoraMenor90Nr);

                                        decimal totMMaior90 = resumoGestores.Sum(x => x.MoraMaior90Volume);
                                        int totMMaior90Nr = resumoGestores.Sum(x => x.MoraMaior90Nr);

                                        decimal totMoraGeral = totM7 + totM15 + totM30 + totM60 + totM90 + totMMaior90;
                                        int totMoraNrGeral = totM7Nr + totM15Nr + totM30Nr + totM60Nr + totM90Nr + totMMaior90Nr;
                                        decimal totPctMora = totVigente > 0 ? (totMoraGeral / totVigente) * 100m : 0m;

                                        table.Cell().Background("#edf2f7").Padding(3).Text("TOTAL GERAL").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totDesembolso:N2}/{totDesembolsoNr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totReembolso:N2}/{totReembolsoNr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totPendentes:N2}/{totPendentesNr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totAprovados:N2}/{totAprovadosNr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totJuros:N2}/{totJurosNr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totVigente:N2}/{totVigenteNr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totM7:N2}/{totM7Nr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totM15:N2}/{totM15Nr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totM30:N2}/{totM30Nr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totM60:N2}/{totM60Nr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totM90:N2}/{totM90Nr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totMMaior90:N2}/{totMMaior90Nr}").Bold().FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totMoraGeral:N2}/{totMoraNrGeral}").Bold().FontColor("#c53030").FontSize(6.5f);
                                        table.Cell().Background("#edf2f7").Padding(3).AlignRight().Text($"{totPctMora:N2}%").Bold().FontColor("#c53030").FontSize(6.5f);
                                    });
                                });
                            }));
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Mapa_Desempenho_{dataReferencia:yyyyMMdd}.pdf");
        }
    }
}
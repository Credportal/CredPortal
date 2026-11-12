using Analise.Data;
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class RelatorioController : Controller
    {
        private readonly IPedidoRepositorio _pedidoRepositorio;
        private readonly IEntidadeRepositorio _entidadeRepositorio;
        private readonly BancoContext _bancoContext;
        private readonly ISessao _sessao;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RelatorioController(
            IPedidoRepositorio pedidoRepositorio,
            IEntidadeRepositorio entidadeRepositorio,
            BancoContext bancoContext,
            ISessao sessao, IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _pedidoRepositorio = pedidoRepositorio;
            _entidadeRepositorio = entidadeRepositorio;
            _bancoContext = bancoContext;
            _sessao = sessao;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string tipoRelatorio = "Pendentes",
            DateTime? dataInicial = null,
            DateTime? dataFinal = null,
            int? gestorId = null,
            int? entidadeId = null,
            int? contaId = null)
        {
            if (_sessao.BuscarSessaoDoUsuario() == null)
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // Tratamento e validação de datas caso venham invertidas
                if (dataInicial.HasValue && dataFinal.HasValue && dataInicial.Value > dataFinal.Value)
                {
                    var temp = dataInicial;
                    dataInicial = dataFinal;
                    dataFinal = temp;
                }

                var pedidos = await _pedidoRepositorio.BuscarTodosAsync() ?? new List<PedidoModel>();

                // Listas para os filtros globais
                ViewBag.ListaGestores = pedidos.Where(p => p.Gestor != null).Select(p => p.Gestor).Distinct().ToList();
                ViewBag.ListaEntidades = pedidos.Where(p => p.Entidade != null).Select(p => p.Entidade).Distinct().ToList();

                // Devolver os filtros selecionados à View
                ViewBag.FiltroGestorId = gestorId;
                ViewBag.FiltroEntidadeId = entidadeId;
                ViewBag.FiltroContaId = contaId;
                ViewBag.FiltroDataInicial = dataInicial?.ToString("yyyy-MM-dd");
                ViewBag.FiltroDataFinal = dataFinal?.ToString("yyyy-MM-dd");

                var pedidosGerais = pedidos.AsEnumerable();

                if (gestorId.HasValue && gestorId.Value > 0)
                {
                    pedidosGerais = pedidosGerais.Where(p => p.GestorId == gestorId.Value);
                }

                if (entidadeId.HasValue && entidadeId.Value > 0)
                {
                    pedidosGerais = pedidosGerais.Where(p => p.EntidadeId == entidadeId.Value);
                }

                var pedidosFiltrados = pedidosGerais;
                if (dataInicial.HasValue)
                {
                    pedidosFiltrados = pedidosFiltrados.Where(p => p.DataCadastro >= dataInicial.Value);
                }
                if (dataFinal.HasValue)
                {
                    pedidosFiltrados = pedidosFiltrados.Where(p => p.DataCadastro <= dataFinal.Value.AddDays(1));
                }

                var pedidosDesembolsados = pedidosGerais;
                if (dataInicial.HasValue)
                {
                    pedidosDesembolsados = pedidosDesembolsados.Where(p => p.DataDesembolso.HasValue && p.DataDesembolso.Value >= dataInicial.Value);
                }
                if (dataFinal.HasValue)
                {
                    pedidosDesembolsados = pedidosDesembolsados.Where(p => p.DataDesembolso.HasValue && p.DataDesembolso.Value <= dataFinal.Value.AddDays(1));
                }

                var pedidoIdsEmMora = _bancoContext.Amortizacoes
                    .AsEnumerable()
                    .Where(a => a.DiasAtraso > 0 && a.PrincipalPorPagar > 0)
                    .Select(a => a.PedidoId)
                    .Distinct()
                    .ToList();

                var hoje = DateTime.Today;

                var queryAmortizacoes = _bancoContext.Amortizacoes
                    .Include(a => a.Pedido)
                    .ThenInclude(p => p.Entidade)
                    .Include(a => a.Pedido)
                    .ThenInclude(p => p.Gestor)
                    .AsEnumerable();

                if (gestorId.HasValue && gestorId.Value > 0)
                {
                    queryAmortizacoes = queryAmortizacoes.Where(a => a.Pedido?.GestorId == gestorId.Value);
                }
                if (entidadeId.HasValue && entidadeId.Value > 0)
                {
                    queryAmortizacoes = queryAmortizacoes.Where(a => a.Pedido?.EntidadeId == entidadeId.Value);
                }

                if (dataInicial.HasValue)
                {
                    queryAmortizacoes = queryAmortizacoes.Where(a => a.Vencimento.Date >= dataInicial.Value.Date);
                }
                if (dataFinal.HasValue)
                {
                    queryAmortizacoes = queryAmortizacoes.Where(a => a.Vencimento.Date <= dataFinal.Value.Date);
                }

                ViewBag.VencimentosProximos = queryAmortizacoes
                    .Where(a => a.PrincipalPorPagar > 0 && a.Vencimento.Date >= hoje)
                    .OrderBy(a => a.Vencimento)
                    .ToList();

                ViewBag.PagamentosPendentes = queryAmortizacoes
                    .Where(a => a.PrincipalPorPagar > 0 && a.Vencimento.Date < hoje)
                    .OrderBy(a => a.Vencimento)
                    .ToList();

                // Pagamentos Feitos (Amortizações liquidadas onde PrincipalPorPagar <= 0)
                ViewBag.PagamentosFeitos = queryAmortizacoes
                    .Where(a => a.PrincipalPorPagar <= 0)
                    .OrderByDescending(a => a.Vencimento)
                    .ToList();

                // --- CARREGAR LISTA DE CLIENTES ---
                var clientesQuery = _entidadeRepositorio.BuscarTodos() ?? new List<EntidadeModel>();

                if (entidadeId.HasValue && entidadeId.Value > 0)
                {
                    clientesQuery = clientesQuery.Where(c => c.Id == entidadeId.Value).ToList();
                }
                if (dataInicial.HasValue)
                {
                    clientesQuery = clientesQuery.Where(c => c.DataCadastro >= dataInicial.Value).ToList();
                }
                if (dataFinal.HasValue)
                {
                    clientesQuery = clientesQuery.Where(c => c.DataCadastro <= dataFinal.Value.AddDays(1)).ToList();
                }

                ViewBag.Clientes = clientesQuery.OrderBy(c => c.Nome).ToList();

                // --- CARREGAR MOVIMENTOS EXTERNOS ---
                var queryMovimentos = _bancoContext.MovimentosExternos
                    .Include(m => m.Entidade)
                    .Include(m => m.Conta)
                    .AsEnumerable();

                if (entidadeId.HasValue && entidadeId.Value > 0)
                {
                    queryMovimentos = queryMovimentos.Where(m => m.EntidadeId == entidadeId.Value);
                }

                if (dataInicial.HasValue)
                {
                    queryMovimentos = queryMovimentos.Where(m => m.DataCadastro.Date >= dataInicial.Value.Date);
                }

                if (dataFinal.HasValue)
                {
                    queryMovimentos = queryMovimentos.Where(m => m.DataCadastro.Date <= dataFinal.Value.Date);
                }

                ViewBag.MovimentosExternos = queryMovimentos.OrderByDescending(m => m.DataCadastro).ToList();

                // --- CARREGAR CONTAS PARA O FILTRO ---
                var contasLista = _bancoContext.Contas.AsEnumerable().ToList();
                ViewBag.ListaContasFiltro = contasLista;

                // --- CARREGAR MOVIMENTOS INTERNOS ---
                var queryMovimentosInternos = _bancoContext.MovimentosInternos
                    .Include(m => m.ContaOrigem)
                    .Include(m => m.ContaDestino)
                    .AsEnumerable();

                if (contaId.HasValue && contaId.Value > 0)
                {
                    queryMovimentosInternos = queryMovimentosInternos.Where(m => m.ContaOrigemId == contaId.Value || m.ContaDestinoId == contaId.Value);
                }

                if (dataInicial.HasValue)
                {
                    queryMovimentosInternos = queryMovimentosInternos.Where(m => m.DataCadastro.Date >= dataInicial.Value.Date);
                }

                if (dataFinal.HasValue)
                {
                    queryMovimentosInternos = queryMovimentosInternos.Where(m => m.DataCadastro.Date <= dataFinal.Value.Date);
                }

                ViewBag.MovimentosInternos = queryMovimentosInternos.OrderByDescending(m => m.DataCadastro).ToList();
                // ----------------------------------

                ViewBag.RelatorioTipo = tipoRelatorio;
                ViewBag.Pendentes = pedidosFiltrados.Where(p => p.EstadoId == 1).ToList();
                ViewBag.Aprovados = pedidosFiltrados.Where(p => p.EstadoId == 2).ToList();
                ViewBag.Rejeitados = pedidosFiltrados.Where(p => p.EstadoId == 4).ToList();
                ViewBag.Vigentes = pedidosFiltrados.Where(p => p.EstadoId == 3).ToList();
                ViewBag.Liquidados = pedidosFiltrados.Where(p => p.EstadoId == 5).ToList();
                ViewBag.Desembolsos = pedidosDesembolsados.Where(p => p.DataDesembolso.HasValue).ToList();
                ViewBag.Mora = pedidosFiltrados.Where(p => pedidoIdsEmMora.Contains(p.Id)).ToList();
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao gerar relatórios: {ex.Message}";

                ViewBag.Pendentes = new List<PedidoModel>();
                ViewBag.Aprovados = new List<PedidoModel>();
                ViewBag.Rejeitados = new List<PedidoModel>();
                ViewBag.Vigentes = new List<PedidoModel>();
                ViewBag.Liquidados = new List<PedidoModel>();
                ViewBag.Desembolsos = new List<PedidoModel>();
                ViewBag.Mora = new List<PedidoModel>();
                ViewBag.VencimentosProximos = new List<Analise.Models.AmortizacaoModel>();
                ViewBag.PagamentosPendentes = new List<Analise.Models.AmortizacaoModel>();
                ViewBag.PagamentosFeitos = new List<Analise.Models.AmortizacaoModel>();
                ViewBag.MovimentosExternos = new List<MovimentoExternoModel>();
                ViewBag.MovimentosInternos = new List<MovimentoInternoModel>();
                ViewBag.Clientes = new List<EntidadeModel>();
                ViewBag.ListaGestores = new List<UsuarioModel>();
                ViewBag.ListaEntidades = new List<EntidadeModel>();
                ViewBag.ListaContasFiltro = new List<ContaModel>();
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GerarCarteiraPdf(
     string tipoRelatorio,
     DateTime? dataInicial,
     DateTime? dataFinal,
     int? gestorId,
     int? entidadeId,
     int? contaId)
        {
            tipoRelatorio ??= "Pendentes";
            var culturaPt = new CultureInfo("pt-PT");

            // Obter utilizador logado para auditoria no documento
            var usuarioSessao = _sessao.BuscarSessaoDoUsuario();
            string nomeUsuarioProcessamento = usuarioSessao?.Nome ?? "Administrador";

            if (dataInicial.HasValue && dataFinal.HasValue && dataInicial.Value > dataFinal.Value)
            {
                var temp = dataInicial;
                dataInicial = dataFinal;
                dataFinal = temp;
            }

            // Buscar descrições amigáveis para os filtros selecionados
            string nomeGestorFiltro = "Todos";
            if (gestorId.HasValue && gestorId.Value > 0)
            {
                var gestorObj = await _bancoContext.Usuarios.FirstOrDefaultAsync(u => u.Id == gestorId.Value);
                if (gestorObj != null) nomeGestorFiltro = gestorObj.Nome;
            }

            string nomeClienteFiltro = "Todos";
            if (entidadeId.HasValue && entidadeId.Value > 0)
            {
                var clienteObj = await _bancoContext.Entidades.FirstOrDefaultAsync(e => e.Id == entidadeId.Value);
                if (clienteObj != null) nomeClienteFiltro = clienteObj.Nome;
            }

            string nomeContaFiltro = "Todas";
            if (contaId.HasValue && contaId.Value > 0)
            {
                var contaObj = await _bancoContext.Contas.FirstOrDefaultAsync(c => c.Id == contaId.Value);
                if (contaObj != null) nomeContaFiltro = $"{contaObj.Banco} - {contaObj.Conta}";
            }

            // Consultas Base
            var queryPedidos = _bancoContext.Pedidos
                .Include(p => p.Gestor)
                .Include(p => p.Entidade)
                .AsQueryable();

            if (gestorId.HasValue && gestorId.Value > 0) queryPedidos = queryPedidos.Where(p => p.GestorId == gestorId.Value);
            if (entidadeId.HasValue && entidadeId.Value > 0) queryPedidos = queryPedidos.Where(p => p.EntidadeId == entidadeId.Value);
            if (dataInicial.HasValue) queryPedidos = queryPedidos.Where(p => p.DataCadastro >= dataInicial.Value);
            if (dataFinal.HasValue) queryPedidos = queryPedidos.Where(p => p.DataCadastro <= dataFinal.Value.AddDays(1));

            var queryAmortizacoes = _bancoContext.Amortizacoes
                .Include(a => a.Pedido).ThenInclude(p => p.Gestor)
                .Include(a => a.Pedido).ThenInclude(p => p.Entidade)
                .AsQueryable();

            if (gestorId.HasValue && gestorId.Value > 0) queryAmortizacoes = queryAmortizacoes.Where(a => a.Pedido.GestorId == gestorId.Value);
            if (entidadeId.HasValue && entidadeId.Value > 0) queryAmortizacoes = queryAmortizacoes.Where(a => a.Pedido.EntidadeId == entidadeId.Value);
            if (dataInicial.HasValue) queryAmortizacoes = queryAmortizacoes.Where(a => a.Vencimento.Date >= dataInicial.Value.Date);
            if (dataFinal.HasValue) queryAmortizacoes = queryAmortizacoes.Where(a => a.Vencimento.Date <= dataFinal.Value.Date);

            List<PedidoModel> listaPedidosFiltro = new();
            List<AmortizacaoModel> listaAmortFiltro = new();
            List<EntidadeModel> listaClientesFiltro = new();
            List<MovimentoExternoModel> listaExtratoExtFiltro = new();
            List<MovimentoInternoModel> listaExtratoIntFiltro = new();

            if (tipoRelatorio == "Pendentes" || tipoRelatorio == "Rejeitados")
            {
                int estadoIdFiltro = tipoRelatorio == "Pendentes" ? 1 : 4;
                listaPedidosFiltro = await queryPedidos.Where(p => p.EstadoId == estadoIdFiltro).ToListAsync();
            }
            else if (tipoRelatorio == "Aprovados" || tipoRelatorio == "Vigentes" || tipoRelatorio == "Liquidados" || tipoRelatorio == "Desembolsos")
            {
                int eId = tipoRelatorio switch { "Aprovados" => 2, "Vigentes" => 3, "Liquidados" => 5, _ => 3 };
                listaPedidosFiltro = await queryPedidos.Where(p => p.EstadoId == eId).ToListAsync();
            }
            else if (tipoRelatorio == "VencimentosProximos" || tipoRelatorio == "PagamentosPendentes" || tipoRelatorio == "PagamentosFeitos")
            {
                bool isMora = tipoRelatorio == "PagamentosPendentes";
                bool isFeito = tipoRelatorio == "PagamentosFeitos";

                listaAmortFiltro = await queryAmortizacoes
                    .Where(a => isFeito ? a.PrincipalPorPagar <= 0
                                        : (isMora ? a.Vencimento.Date < DateTime.Today && a.PrincipalPorPagar > 0
                                                  : a.Vencimento.Date >= DateTime.Today && a.PrincipalPorPagar > 0))
                    .OrderBy(a => a.Vencimento)
                    .ToListAsync();
            }
            else if (tipoRelatorio == "Clientes")
            {
                var cliQuery = _entidadeRepositorio.BuscarTodos() ?? new List<EntidadeModel>();
                if (dataInicial.HasValue) cliQuery = cliQuery.Where(c => c.DataCadastro >= dataInicial.Value).ToList();
                if (dataFinal.HasValue) cliQuery = cliQuery.Where(c => c.DataCadastro <= dataFinal.Value.AddDays(1)).ToList();
                listaClientesFiltro = cliQuery.OrderBy(c => c.Nome).ToList();
            }
            else if (tipoRelatorio == "ExtratoExterno")
            {
                var extQuery = _bancoContext.MovimentosExternos
                    .Include(m => m.Entidade)
                    .Include(m => m.Conta)
                    .AsQueryable();

                if (entidadeId.HasValue && entidadeId.Value > 0) extQuery = extQuery.Where(m => m.EntidadeId == entidadeId.Value);
                if (dataInicial.HasValue) extQuery = extQuery.Where(m => m.DataCadastro.Date >= dataInicial.Value.Date);
                if (dataFinal.HasValue) extQuery = extQuery.Where(m => m.DataCadastro.Date <= dataFinal.Value.Date);
                listaExtratoExtFiltro = await extQuery.OrderByDescending(m => m.DataCadastro).ToListAsync();
            }
            else if (tipoRelatorio == "ExtratoInterno")
            {
                var intQuery = _bancoContext.MovimentosInternos
                    .Include(m => m.ContaOrigem)
                    .Include(m => m.ContaDestino)
                    .AsQueryable();

                if (contaId.HasValue && contaId.Value > 0) intQuery = intQuery.Where(m => m.ContaOrigemId == contaId.Value || m.ContaDestinoId == contaId.Value);
                if (dataInicial.HasValue) intQuery = intQuery.Where(m => m.DataCadastro.Date >= dataInicial.Value.Date);
                if (dataFinal.HasValue) intQuery = intQuery.Where(m => m.DataCadastro.Date <= dataFinal.Value.Date);
                listaExtratoIntFiltro = await intQuery.OrderByDescending(m => m.DataCadastro).ToListAsync();
            }

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

            string tituloRelatorioStr = $"Relatório - {tipoRelatorio}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Orientação Paisagem (Landscape) ideal para relatórios largos
                    page.Size(PageSizes.A4.Landscape());
                    page.MarginTop(20);
                    page.MarginBottom(45);
                    page.MarginLeft(20);
                    page.MarginRight(20);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(8).FontColor("#172b4d"));

                    // Rodapé Institucional Oficial Limpo e sem duplicações
                    page.Footer().AlignCenter().Column(colFooter =>
                    {
                        colFooter.Item().LineHorizontal(1f).LineColor("#b89735");
                        colFooter.Item().PaddingTop(3).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Processado por: ").FontSize(7.5f).Italic().FontColor("#a0aec0");
                                text.Span(nomeUsuarioProcessamento).FontSize(7.5f).Bold().FontColor("#718096");
                            });

                            row.RelativeItem(2).AlignCenter().Text(PadraoTimbradoComponent.RodapeTimbrado).FontSize(7.5f).Bold().FontColor("#079b62");

                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7.5f).Italic().FontColor("#a0aec0");
                            });
                        });
                    });

                    // --- CAMADAS (Marca d'água no fundo + Conteúdo estruturado pelo componente) ---
                    page.Content().Layers(layers =>
                    {
                        if (marcaDaguaBytes != null)
                        {
                            layers.Layer().AlignCenter().AlignMiddle().Column(c =>
                            {
                                c.Item().Width(500).Image(marcaDaguaBytes);
                            });
                        }

                        layers.PrimaryLayer().Component(new PadraoTimbradoComponent(
                            tituloRelatorioStr,
                            null,
                            logoBytes,
                            marcaDaguaBytes,
                            conteudo =>
                            {
                                conteudo.Column(col =>
                                {
                                    col.Spacing(8);

                                    // Bloco de informações adicionais de filtros (Período e Filtros aplicados)
                                    col.Item().Background("#f5f7fb").Padding(6).Row(rowFiltros =>
                                    {
                                        string pIni = dataInicial.HasValue ? dataInicial.Value.ToString("dd/MM/yyyy") : "Início";
                                        string pFin = dataFinal.HasValue ? dataFinal.Value.ToString("dd/MM/yyyy") : "Hoje";

                                        rowFiltros.RelativeItem().Text(t =>
                                        {
                                            t.Span("Período Analisado: ").Bold().FontSize(8f);
                                            t.Span($"{pIni} a {pFin}").FontSize(8f);
                                        });

                                        if (tipoRelatorio != "Clientes")
                                        {
                                            string filtrosTxt = "Filtros: ";
                                            if (tipoRelatorio == "ExtratoExterno") filtrosTxt += $"Cliente: {nomeClienteFiltro}";
                                            else if (tipoRelatorio == "ExtratoInterno") filtrosTxt += $"Conta: {nomeContaFiltro}";
                                            else filtrosTxt += $"Gestor: {nomeGestorFiltro} | Cliente: {nomeClienteFiltro}";

                                            rowFiltros.RelativeItem().AlignRight().Text(filtrosTxt).FontSize(8f).Italic().FontColor("#4a5568");
                                        }
                                    });

                                    col.Item().PaddingTop(4);

                                    // Tabela de Dados Específica de cada Relatório
                                    col.Item().Table(table =>
                                    {
                                        if (tipoRelatorio == "Clientes")
                                        {
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Cliente").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("NUIT").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Contacto").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Data").Bold();
                                            });

                                            foreach (var cli in listaClientesFiltro)
                                            {
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(cli.Nome ?? "N/D").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(cli.Nuit ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(cli.Telefone ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(cli.DataCadastro.ToString("dd/MM/yyyy"));
                                            }

                                            table.Cell().ColumnSpan(3).Background("#f1f5f9").Padding(4).Text($"TOTAL: {listaClientesFiltro.Count}").Bold();
                                            table.Cell().Background("#f1f5f9").Padding(4).Text("");
                                        }
                                        else if (tipoRelatorio == "ExtratoExterno")
                                        {
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(2); c.RelativeColumn(2.5f); c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Data").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Cliente").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Tipo").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Descrição").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Conta").Bold();
                                            });

                                            decimal totalExtratoExt = 0;
                                            foreach (var m in listaExtratoExtFiltro)
                                            {
                                                decimal valorMov = m.Debito != 0 ? m.Debito : m.Credito;
                                                bool isReembolso = m.TipoOperacao != null && m.TipoOperacao.Contains("Reembolso", StringComparison.OrdinalIgnoreCase);

                                                decimal valorFinal = isReembolso ? -valorMov : valorMov;
                                                totalExtratoExt += valorFinal;

                                                string sinalStr = valorFinal > 0 ? "+ " : (valorFinal < 0 ? "- " : "");
                                                string valorFormatado = $"{sinalStr}MT {Math.Abs(valorFinal):N2}";
                                                string corValor = valorFinal < 0 ? "#c53030" : "#079b62";

                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.DataCadastro.ToString("dd/MM/yyyy HH:mm"));
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.Entidade?.Nome ?? "N/D").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.TipoOperacao ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.Descricao ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text(valorFormatado).Bold().FontColor(corValor);
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.Conta?.Banco ?? "N/D");
                                            }

                                            string sinalTotalExt = totalExtratoExt > 0 ? "+ " : (totalExtratoExt < 0 ? "- " : "");
                                            string totalExtFormatado = $"{sinalTotalExt}MT {Math.Abs(totalExtratoExt):N2}";

                                            table.Cell().ColumnSpan(4).Background("#f1f5f9").Padding(4).Text("TOTAL").Bold();
                                            table.Cell().Background("#f1f5f9").Padding(4).AlignRight().Text(totalExtFormatado).Bold().FontColor("#079b62");
                                            table.Cell().Background("#f1f5f9").Padding(4).Text("");
                                        }
                                        else if (tipoRelatorio == "ExtratoInterno")
                                        {
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(2); c.RelativeColumn(2.5f); c.RelativeColumn(2.5f); c.RelativeColumn(2); c.RelativeColumn(3);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Data").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Origem").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Destino").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Descrição").Bold();
                                            });

                                            decimal totalExtratoInt = 0;
                                            foreach (var m in listaExtratoIntFiltro)
                                            {
                                                totalExtratoInt += m.Debito;

                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.DataCadastro.ToString("dd/MM/yyyy HH:mm"));
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.ContaOrigem?.Banco ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.ContaDestino?.Banco ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{m.Debito:N2} MT").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(m.Descricao ?? "N/D");
                                            }

                                            table.Cell().ColumnSpan(3).Background("#f1f5f9").Padding(4).Text("TOTAL GERAL").Bold();
                                            table.Cell().Background("#f1f5f9").Padding(4).AlignRight().Text($"{totalExtratoInt:N2} MT").Bold().FontColor("#079b62");
                                            table.Cell().Background("#f1f5f9").Padding(4).Text("");
                                        }
                                        else if (tipoRelatorio == "VencimentosProximos" || tipoRelatorio == "PagamentosPendentes" || tipoRelatorio == "PagamentosFeitos")
                                        {
                                            bool isMora = tipoRelatorio == "PagamentosPendentes";
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(1.5f);
                                                c.RelativeColumn(1.5f); c.RelativeColumn(2); c.RelativeColumn(1.5f);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Gestor").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Cliente").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("CRR").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Vencimento").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor por Pagar").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignCenter().Text(isMora ? "Atraso" : "Estado").Bold();
                                            });

                                            decimal totalAmortizacoes = 0;
                                            foreach (var item in listaAmortFiltro)
                                            {
                                                int dias = (DateTime.Today - item.Vencimento.Date).Days;
                                                decimal total = item.PrincipalPorPagar + item.JuroPorPagar + item.MultaPorPagar;
                                                totalAmortizacoes += total;

                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(item.Pedido?.Gestor?.Nome ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(item.Pedido?.Entidade?.Nome ?? "N/D").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(item.Pedido?.CRR?.ToString() ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(item.Vencimento.ToString("dd/MM/yyyy"));
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{total:N2} MT").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text(isMora ? $"+{dias} dias" : "Liquidado");
                                            }

                                            table.Cell().ColumnSpan(4).Background("#f1f5f9").Padding(4).Text("TOTAL GERAL").Bold();
                                            table.Cell().Background("#f1f5f9").Padding(4).AlignRight().Text($"{totalAmortizacoes:N2} MT").Bold().FontColor("#c53030");
                                            table.Cell().Background("#f1f5f9").Padding(4).Text("");
                                        }
                                        else
                                        {
                                            table.ColumnsDefinition(c =>
                                            {
                                                c.RelativeColumn(2); c.RelativeColumn(3); c.RelativeColumn(1.5f);
                                                c.RelativeColumn(1.5f); c.RelativeColumn(2); c.RelativeColumn(1);
                                                c.RelativeColumn(1); c.RelativeColumn(2);
                                            });

                                            table.Header(h =>
                                            {
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Gestor").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Cliente").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("CRR").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).Text("Data").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignCenter().Text("Prazo").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Taxa").Bold();
                                                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Prestação").Bold();
                                            });

                                            decimal totalValorPedidos = 0;
                                            decimal totalPrestacaoPedidos = 0;

                                            foreach (var p in listaPedidosFiltro)
                                            {
                                                decimal val = p.ValorDecisao ?? p.Valor;
                                                decimal prest = p.PrestacaoDecisao ?? p.Prestacao;
                                                totalValorPedidos += val;
                                                totalPrestacaoPedidos += prest;

                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(p.Gestor?.Nome ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(p.Entidade?.Nome ?? "N/D").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(p.CRR?.ToString() ?? "N/D");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text((p.DataDesembolso ?? p.DataCadastro).ToString("dd/MM/yyyy"));
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{val:N2} MT").Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignCenter().Text($"{(p.PrazoDecisao ?? p.Prazo)} m");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{(p.TaxaDecisao ?? p.Taxa):N2}%");
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{prest:N2} MT").FontColor("#079b62");
                                            }

                                            table.Cell().ColumnSpan(4).Background("#f1f5f9").Padding(4).Text("TOTAL").Bold();
                                            table.Cell().Background("#f1f5f9").Padding(4).AlignRight().Text($"{totalValorPedidos:N2} MT").Bold();
                                            table.Cell().Background("#f1f5f9").Padding(4).Text("");
                                            table.Cell().Background("#f1f5f9").Padding(4).Text("");
                                            table.Cell().Background("#f1f5f9").Padding(4).AlignRight().Text($"{totalPrestacaoPedidos:N2} MT").Bold().FontColor("#079b62");
                                        }
                                    });
                                });
                            }));
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Relatorio_{tipoRelatorio}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private static void HeaderTabelaPedidos(TableDescriptor table)
        {
            table.Header(h =>
            {
                h.Cell().Background("#edf2f7").Padding(4).Text("Gestor").Bold();
                h.Cell().Background("#edf2f7").Padding(4).Text("Cliente").Bold();
                h.Cell().Background("#edf2f7").Padding(4).Text("CRR").Bold();
                h.Cell().Background("#edf2f7").Padding(4).Text("Data").Bold();
                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor").Bold();
                h.Cell().Background("#edf2f7").Padding(4).AlignCenter().Text("Prazo").Bold();
                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Taxa").Bold();
                h.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Prestação").Bold();
            });
        }
    }
}
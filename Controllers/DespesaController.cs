using Analise.Enuns;
using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    [PermissaoPerfil(PerfilEnum.Admin, PerfilEnum.Super, PerfilEnum.Gerente)]
    public class DespesaController : Controller
    {
        private readonly IDespesaRepositorio _despesaRepositorio;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ISessao _sessao;

        public DespesaController(IDespesaRepositorio despesaRepositorio, 
            ISessao sessao, IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            _despesaRepositorio = despesaRepositorio;
            _sessao = sessao;
        }

        public async Task<IActionResult> Index()
        {
            var despesas = await _despesaRepositorio.BuscarTodasAsync();
            ViewBag.ListaContas = await _despesaRepositorio.BuscarContasAsync();

            var viewModel = new DespesaModel
            {
                ListaContas = new System.Collections.Generic.List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
            };

            ViewBag.ListaDespesas = despesas;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(DespesaModel despesa)
        {
            try
            {
                var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
                if (usuarioLogado != null)
                {
                    despesa.UsuarioCadastroId = usuarioLogado.Id;
                }

                // --- CAPTURAR ERROS DE VALIDAÇÃO DETALHADOS ---
                if (!ModelState.IsValid)
                {
                    var erros = new System.Text.StringBuilder();
                    foreach (var state in ModelState.Values)
                    {
                        foreach (var error in state.Errors)
                        {
                            erros.AppendLine(error.ErrorMessage);
                        }
                    }
                    TempData["MensagemErro"] = "Erros de validação: " + erros.ToString();

                    var despesasRecentes = await _despesaRepositorio.BuscarTodasAsync();
                    ViewBag.ListaContas = await _despesaRepositorio.BuscarContasAsync();
                    ViewBag.ListaDespesas = despesasRecentes;
                    return View("Index", despesa);
                }

                await _despesaRepositorio.AdicionarComMovimentoAsync(despesa, usuarioLogado);
                TempData["MensagemSucesso"] = "Despesa registada com sucesso!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                string detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["MensagemErro"] = $"Erro ao registar despesa: {detalhe}";
            }

            var despesas = await _despesaRepositorio.BuscarTodasAsync();
            ViewBag.ListaContas = await _despesaRepositorio.BuscarContasAsync();
            ViewBag.ListaDespesas = despesas;
            return View("Index", despesa);
        }
        [HttpGet]
        public async Task<IActionResult> GerarRelatorioDespesasPdf()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            // Buscar a lista de despesas (incluindo a conta associada, se houver Include no repositório)
            var listaDespesas = await _despesaRepositorio.BuscarTodasAsync();

            string operadorStr = usuarioLogado?.Login ?? "Administrador";
            string numeroRecibo = $"DSP-{DateTime.Now:yyyyMMdd}";

            decimal totalDespesas = listaDespesas != null ? listaDespesas.Sum(d => d.Valor) : 0;

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
                    page.Size(PageSizes.A4);
                    page.MarginTop(36);
                    page.MarginBottom(75);
                    page.MarginLeft(36);
                    page.MarginRight(36);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9).FontColor("#172b4d"));

                    // Rodapé Institucional Oficial Esticado
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().LineHorizontal(1f).LineColor("#b89735");
                        col.Item().PaddingTop(4).Text(PadraoTimbradoComponent.RodapeTimbrado).FontSize(10.5f).FontColor("#079b62").Bold();
                    });

                    // --- CAMADAS (Marca d'água no fundo + Conteúdo estruturado pelo componente) ---
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
                            "RELATÓRIO DE DESPESAS E SAÍDAS FINANCEIRAS",
                            numeroRecibo,
                            logoBytes,
                            marcaDaguaBytes,
                            conteudo =>
                            {
                                conteudo.Column(col =>
                                {
                                    col.Spacing(8);

                                    // --- RESUMO TOTAL ---
                                    col.Item().Background("#f5f7fb").Padding(6).Row(row =>
                                    {
                                        row.RelativeItem().Text(t => { t.Span("Total Acumulado de Despesas: ").Bold(); t.Span($"{totalDespesas:N2} MT").Bold().FontColor("#c53030"); });
                                    });

                                    col.Item().PaddingTop(5);

                                    // --- TABELA DE DESPESAS ---
                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Data
                                            columns.RelativeColumn(3.5f); // Descrição
                                            columns.RelativeColumn(2.5f); // Conta
                                            columns.RelativeColumn(2); // Valor
                                        });

                                        // Cabeçalho da Tabela
                                        table.Header(header =>
                                        {
                                            header.Cell().Background("#edf2f7").Padding(5).Text("Data / Hora").Bold().FontSize(8.5f);
                                            header.Cell().Background("#edf2f7").Padding(5).Text("Descrição").Bold().FontSize(8.5f);
                                            header.Cell().Background("#edf2f7").Padding(5).Text("Conta de Origem").Bold().FontSize(8.5f);
                                            header.Cell().Background("#edf2f7").Padding(5).AlignRight().Text("Valor (MT)").Bold().FontSize(8.5f);
                                        });

                                        if (listaDespesas != null && listaDespesas.Any())
                                        {
                                            foreach (var d in listaDespesas)
                                            {
                                                string contaInfo = d.Conta != null ? $"{d.Conta.Banco} - {d.Conta.Conta}" : "N/D";

                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(d.DataCadastro.ToString("dd/MM/yyyy HH:mm")).FontSize(8.5f);
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(d.Descricao ?? "-").FontSize(8.5f).Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(contaInfo).FontSize(8.5f);
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).AlignRight().Text($"{d.Valor:N2} MT").FontSize(8.5f).Bold().FontColor("#c53030");
                                            }
                                        }
                                        else
                                        {
                                            table.Cell().ColumnSpan(4).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(10).AlignCenter().Text("Nenhuma despesa registada até o momento.").FontSize(9).Italic().FontColor("#718096");
                                        }
                                    });

                                    // --- LOCAL E ASSINATURAS ---
                                    col.Item().PaddingTop(20);

                                    col.Item().Row(row =>
                                    {
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(8.5f).Italic();
                                        });
                                    });

                                    col.Item().PaddingTop(15);

                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn();
                                            columns.ConstantColumn(80);
                                            columns.RelativeColumn();
                                        });

                                        table.Cell().Column(c =>
                                        {
                                            c.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor("#718096");
                                            c.Item().Text("Assinatura do Responsável").FontSize(8.5f).Bold();
                                            c.Item().Text(operadorStr).FontSize(8f).FontColor("#4a5568");
                                        });

                                        table.Cell();

                                        table.Cell().Column(c =>
                                        {
                                            c.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor("#718096");
                                            c.Item().Text("Pelo Operador / Instituição").FontSize(8.5f).Bold();
                                            c.Item().Text(PadraoTimbradoComponent.NomeEmpresa).FontSize(8f).FontColor("#4a5568");
                                        });
                                    });
                                });
                            }));
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Relatorio_Despesas_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
    }
}
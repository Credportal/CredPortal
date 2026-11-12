using Analise.Data;
using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Helpers;
using System;
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
    public class ContaController : Controller
    {
        private readonly IContaRepositorio _contaRepositorio;
        private readonly IMovimentoInternoRepositorio _movimentoRepositorio;
        private readonly ISessao _sessao;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ContaController(IContaRepositorio contaRepositorio, 
            IMovimentoInternoRepositorio movimentoRepositorio, 
            ISessao sessao,
             IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment; 
            _contaRepositorio = contaRepositorio;
            _movimentoRepositorio = movimentoRepositorio;
            _sessao = sessao;
        }

        // GET: /Conta/Ver
        public async Task<IActionResult> Ver()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var viewModel = new ContaViewModel
            {
                ListaContas = await _contaRepositorio.BuscarTodasAsync()
            };

            return View(viewModel);
        }

        // GET: /Conta/Criar
        public IActionResult Criar()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            return View();
        }

        // POST: /Conta/Criar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(string banco, string conta, string nib, string titular)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(conta) ||
                string.IsNullOrWhiteSpace(nib) || string.IsNullOrWhiteSpace(titular))
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos obrigatórios da conta bancária.";
                var modelErro = new ContaModel { Banco = banco, Conta = conta, NIB = nib, Titular = titular };
                return View(modelErro);
            }

            var novaConta = new ContaModel
            {
                Banco = banco,
                Conta = conta,
                NIB = nib,
                Titular = titular,
                UsuarioId = usuarioLogado.Id,
                DataCadastro = DateTimeOffset.Now
            };

            await _contaRepositorio.AdicionarAsync(novaConta);
            TempData["MensagemSucesso"] = "Conta bancária registada com sucesso!";
            return RedirectToAction("Ver");
        }

        // GET: /Conta/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var conta = await _contaRepositorio.BuscarPorIdAsync(id);
            if (conta == null)
            {
                TempData["MensagemErro"] = "Conta bancária não encontrada.";
                return RedirectToAction("Ver");
            }

            return View(conta);
        }

        // POST: /Conta/Editar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string banco, string conta, string nib, string titular)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (string.IsNullOrWhiteSpace(banco) || string.IsNullOrWhiteSpace(conta) ||
                string.IsNullOrWhiteSpace(nib) || string.IsNullOrWhiteSpace(titular))
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos obrigatórios.";
                var modelErro = new ContaModel { Id = id, Banco = banco, Conta = conta, NIB = nib, Titular = titular };
                return View(modelErro);
            }

            var contaDB = await _contaRepositorio.BuscarPorIdAsync(id);
            if (contaDB == null)
            {
                TempData["MensagemErro"] = "Conta bancária não encontrada para atualização.";
                return RedirectToAction("Ver");
            }

            contaDB.Banco = banco;
            contaDB.Conta = conta;
            contaDB.NIB = nib;
            contaDB.Titular = titular;

            await _contaRepositorio.AtualizarAsync(contaDB);
            TempData["MensagemSucesso"] = "Conta bancária atualizada com sucesso!";
            return RedirectToAction("Ver");
        }

        // ==========================================
        // MÉTODOS DE OPERAÇÕES BANCÁRIAS (DEPÓSITOS E TRANSFERÊNCIAS)
        // ==========================================

        // GET: /Conta/Operacoes
        public async Task<IActionResult> Operacoes()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var viewModel = new OperacoesViewModel
            {
                ListaContas = await _contaRepositorio.BuscarTodasAsync(),
                ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync()
            };

            return View(viewModel);
        }

        // POST: /Conta/EfetuarDeposito
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EfetuarDeposito(OperacoesViewModel model)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (model.NovoDeposito == null || string.IsNullOrWhiteSpace(model.NovoDeposito.Descricao) ||
                model.NovoDeposito.ContaDestinoId <= 0 || model.NovoDeposito.Credito <= 0)
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos do depósito corretamente.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            var contaDestino = await _contaRepositorio.BuscarPorIdAsync(model.NovoDeposito.ContaDestinoId.Value);
            if (contaDestino == null)
            {
                TempData["MensagemErro"] = "Conta de destino não encontrada.";
                return RedirectToAction("Operacoes");
            }

            var movimento = new MovimentoInternoModel
            {
                Descricao = model.NovoDeposito.Descricao,
                Credito = model.NovoDeposito.Credito,
                Debito = 0,
                ContaOrigemId = model.NovoDeposito.ContaDestinoId,
                ContaDestinoId = model.NovoDeposito.ContaDestinoId,
                TipoOperacao = "Depósito",
                UsuarioId = usuarioLogado.Id,
                AgenciaId = usuarioLogado.AgenciaId,
                DataCadastro = DateTimeOffset.Now
            };
            await _movimentoRepositorio.AdicionarAsync(movimento);

            contaDestino.Credito += model.NovoDeposito.Credito;
            //contaDestino.Saldo = contaDestino.Credito - contaDestino.Debito;
            await _contaRepositorio.AtualizarAsync(contaDestino);

            TempData["MensagemSucesso"] = "Depósito efetuado com sucesso!";
            return RedirectToAction("Operacoes");
        }

        // POST: /Conta/EfetuarTransferencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EfetuarTransferencia(OperacoesViewModel model)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (model.NovaTransferencia == null || string.IsNullOrWhiteSpace(model.NovaTransferencia.Descricao) ||
                model.NovaTransferencia.ContaOrigemId <= 0 || model.NovaTransferencia.ContaDestinoId <= 0 ||
                model.NovaTransferencia.Credito <= 0)
            {
                TempData["MensagemErro"] = "Por favor, preencha todos os campos da transferência corretamente.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            if (model.NovaTransferencia.ContaOrigemId == model.NovaTransferencia.ContaDestinoId)
            {
                TempData["MensagemErro"] = "A conta de origem e a conta de destino não podem ser a mesma.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            // Buscar as contas envolvidas
            var contaOrigem = await _contaRepositorio.BuscarPorIdAsync(model.NovaTransferencia.ContaOrigemId.Value);
            var contaDestino = await _contaRepositorio.BuscarPorIdAsync(model.NovaTransferencia.ContaDestinoId.Value);

            if (contaOrigem == null || contaDestino == null)
            {
                TempData["MensagemErro"] = "Conta de origem ou destino não encontrada.";
                return RedirectToAction("Operacoes");
            }

            decimal valorTransferencia = model.NovaTransferencia.Credito;

            // Validação de segurança de saldo no backend
            if (contaOrigem.Saldo < valorTransferencia)
            {
                TempData["MensagemErro"] = "A conta de origem não possui saldo suficiente para esta transferência.";
                model.ListaContas = await _contaRepositorio.BuscarTodasAsync();
                model.ListaMovimentos = await _movimentoRepositorio.BuscarTodosAsync();
                return View("Operacoes", model);
            }

            // 1º Movimento: Registo do Débito na Conta Origem
            var movimentoDebito = new MovimentoInternoModel
            {
                Descricao = model.NovaTransferencia.Descricao,
                Debito = valorTransferencia,
                Credito = 0,
                ContaOrigemId = contaOrigem.Id,
                ContaDestinoId = contaDestino.Id,
                TipoOperacao = "Transferência (Débito)",
                UsuarioId = usuarioLogado.Id,
                AgenciaId = usuarioLogado.AgenciaId,
                DataCadastro = DateTimeOffset.Now
            };
            await _movimentoRepositorio.AdicionarAsync(movimentoDebito);

            // 2º Movimento: Registo do Crédito na Conta Destino
            var movimentoCredito = new MovimentoInternoModel
            {
                Descricao = model.NovaTransferencia.Descricao,
                Credito = valorTransferencia,
                Debito = 0,
                ContaOrigemId = contaOrigem.Id,
                ContaDestinoId = contaDestino.Id,
                TipoOperacao = "Transferência (Crédito)",
                UsuarioId = usuarioLogado.Id,
                AgenciaId = usuarioLogado.AgenciaId,
                DataCadastro = DateTimeOffset.Now
            };
            await _movimentoRepositorio.AdicionarAsync(movimentoCredito);

            // Atualizar Conta Origem (Aumenta o Débito, Recalcula o Saldo)
            contaOrigem.Debito += valorTransferencia;
            //contaOrigem.Saldo = contaOrigem.Credito - contaOrigem.Debito;
            await _contaRepositorio.AtualizarAsync(contaOrigem);

            // Atualizar Conta Destino (Aumenta o Crédito, Recalcula o Saldo)
            contaDestino.Credito += valorTransferencia;
            //contaDestino.Saldo = contaDestino.Credito - contaDestino.Debito;
            await _contaRepositorio.AtualizarAsync(contaDestino);

            TempData["MensagemSucesso"] = "Transferência efectuada com sucesso!";
            return RedirectToAction("Operacoes");
        }
        [HttpGet]
        public async Task<IActionResult> GerarRelatorioContasPdf()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            // Buscar a lista de contas bancárias utilizando o repositório correto
            var listaContas = await _contaRepositorio.BuscarTodasAsync();

            string operadorStr = usuarioLogado?.Login ?? "Administrador";
            string numeroRecibo = $"CTA-{DateTime.Now:yyyyMMdd}";

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
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9f).FontColor("#172b4d"));

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
                            "LISTAGEM DE CONTAS BANCÁRIAS",
                            numeroRecibo,
                            logoBytes,
                            marcaDaguaBytes,
                            conteudo =>
                            {
                                conteudo.Column(col =>
                                {
                                    col.Spacing(8);

                                    // --- TABELA DE CONTAS ---
                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(2); // Banco
                                            columns.RelativeColumn(2); // Número da Conta
                                            columns.RelativeColumn(2); // NIB
                                            columns.RelativeColumn(2.5f); // Titular
                                        });

                                        // Cabeçalho da Tabela
                                        table.Header(header =>
                                        {
                                            header.Cell().Background("#edf2f7").Padding(5).Text("Banco").Bold().FontSize(8.5f);
                                            header.Cell().Background("#edf2f7").Padding(5).Text("Número da Conta").Bold().FontSize(8.5f);
                                            header.Cell().Background("#edf2f7").Padding(5).Text("NIB").Bold().FontSize(8.5f);
                                            header.Cell().Background("#edf2f7").Padding(5).Text("Titular").Bold().FontSize(8.5f);
                                        });

                                        if (listaContas != null && listaContas.Any())
                                        {
                                            foreach (var conta in listaContas)
                                            {
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(conta.Banco ?? "-").FontSize(8.5f).Bold();
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(conta.Conta ?? "-").FontSize(8.5f);
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(conta.NIB ?? "-").FontSize(8.5f);
                                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(5).Text(conta.Titular ?? "-").FontSize(8.5f);
                                            }
                                        }
                                        else
                                        {
                                            table.Cell().ColumnSpan(4).BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(10).AlignCenter().Text("Nenhuma conta bancária registada até ao momento.").FontSize(9).Italic().FontColor("#718096");
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
            return File(bytes, "application/pdf", $"Listagem_Contas_Bancarias_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
    }
}
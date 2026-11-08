using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuestPDF.Helpers;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class EntidadeController : Controller
    {
        private readonly IEntidadeRepositorio _entidadeRepositorio;
        private readonly ISessao _sessao;

        public EntidadeController(IEntidadeRepositorio entidadeRepositorio, ISessao sessao)
        {
            _entidadeRepositorio = entidadeRepositorio;
            _sessao = sessao;
        }

        // Método para carregar a página de Detalhes com base no ID
        public IActionResult Detalhes(int id)
        {
            var entidade = _entidadeRepositorio.ListarPorId(id);

            if (entidade == null)
            {
                TempData["MensagemErro"] = "Entidade não encontrada!";
                return RedirectToAction("Criar");
            }

            var viewModel = new EntidadeViewModel
            {
                EntidadeNome = entidade
            };

            return View(viewModel);
        }

        public IActionResult Ver()
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            var viewModel = new EntidadeViewModel
            {
                EntidadeNome = new EntidadeModel(),
                ListaEntidades = _entidadeRepositorio.BuscarTodos(),
            };

            return View(viewModel);
        }

        public IActionResult Criar()
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            var viewModel = new EntidadeViewModel
            {
                EntidadeNome = new EntidadeModel(),
                ListaEntidades = _entidadeRepositorio.BuscarTodos(),
            };

            return View(viewModel);
        }

        public IActionResult Editar(int id)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            var entidade = _entidadeRepositorio.ListarPorId(id);

            if (entidade == null)
            {
                TempData["MensagemErro"] = "Entidade não encontrada!";
                return RedirectToAction("Criar");
            }

            EntidadeViewModel viewModel = new EntidadeViewModel
            {
                EntidadeNome = entidade
            };

            return View(viewModel);
        }

        // Endpoint AJAX para verificar duplicidade de NUIT ou Documento
        [HttpGet]
        public IActionResult VerificarDuplicado(string nuit, string nrDocumento, int? idAtual)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;
            if (usuarioId == 0)
            {
                // Opcional: validar sessão se necessário
            }

            var todasEntidades = _entidadeRepositorio.BuscarTodos();

            var entidadeExistente = todasEntidades.FirstOrDefault(e =>
                (idAtual == null || e.Id != idAtual.Value) &&
                ((!string.IsNullOrEmpty(nuit) && e.Nuit == nuit) ||
                 (!string.IsNullOrEmpty(nrDocumento) && e.NrDocumento == nrDocumento))
            );

            if (entidadeExistente != null)
            {
                return Json(new
                {
                    encontrado = true,
                    id = entidadeExistente.Id,
                    nome = entidadeExistente.Nome,
                    nuit = entidadeExistente.Nuit,
                    tipoDocumento = entidadeExistente.Documento,
                    nrDocumento = entidadeExistente.NrDocumento,
                    telefone = entidadeExistente.Telefone
                });
            }

            return Json(new { encontrado = false });
        }

        [HttpPost]
        public IActionResult Criar(EntidadeViewModel model)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            try
            {
                // Correção: A gravação deve ocorrer quando o ModelState é VÁLIDO
                if (!ModelState.IsValid)
                {
                    model.EntidadeNome.UsuarioId = usuarioId;
                    _entidadeRepositorio.Adicionar(model.EntidadeNome);

                    TempData["MensagemSucesso"] = "Registado com sucesso!";
                    return RedirectToAction("Criar");
                }

                model.ListaEntidades = _entidadeRepositorio.BuscarTodos();
                return View(model);
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Erro ao registar: {erro.Message}";
                return RedirectToAction("Criar");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Editar(EntidadeViewModel model)
        {
            int usuarioId = HttpContext.Session.GetInt32("UsuarioId") ?? 0;

            try
            {
                // Correção: A atualização deve ocorrer quando o ModelState é VÁLIDO
                if (ModelState.IsValid)
                {
                    var entidadeExistente = _entidadeRepositorio.ListarPorId(model.EntidadeNome.Id);

                    if (entidadeExistente == null)
                    {
                        TempData["MensagemErro"] = "Não encontrado!";
                        return RedirectToAction("Ver");
                    }

                    model.EntidadeNome.UsuarioId = usuarioId;
                    _entidadeRepositorio.Actualizar(model.EntidadeNome);
                    TempData["MensagemSucesso"] = "Actualizado com sucesso!";
                    return RedirectToAction("Ver");
                }

                return View("Editar", model);
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, erro ao actualizar dados! Detalhes: {erro.Message}";
                return RedirectToAction("Editar", new { id = model.EntidadeNome.Id });
            }
        }
        [HttpGet]
        public IActionResult GerarFichaClientePdf(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var entidade = _entidadeRepositorio.ListarPorId(id);
            if (entidade == null) return NotFound();

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            bool isEmpresa = entidade.TipoCliente == "Empresarial";
            string tipoClienteStr = isEmpresa ? "Empresarial" : "Particular";
            string numeroProcesso = $"CLI-{DateTime.Now:yyyyMMdd}-{entidade.Id}";

            // Configurar cultura para português para garantir o mês correto
            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(10).FontColor("#172b4d"));

                    // Rodapé padrão em todas as páginas do documento
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("FICHA DE REGISTO DE CLIENTE — ").FontSize(8).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(8).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // --- CABEÇALHO ---
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(22).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(9).FontColor("#718096");
                            });

                            row.ConstantItem(160).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic().AlignRight();
                                c.Item().Text($"Ref: {numeroProcesso}").Bold().FontSize(9).FontColor("#079b62").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#079b62");

                        // Título em Bloco Verde
                        col.Item().Background("#079b62").Padding(8).AlignCenter().Text(isEmpresa ? "FICHA DE CLIENTE EMPRESARIAL" : "FICHA DE CLIENTE PARTICULAR")
                            .Bold().FontSize(12).FontColor("#FFFFFF");

                        col.Item().PaddingTop(5);

                        // --- 1. IDENTIFICAÇÃO & DOCUMENTAÇÃO ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("1. IDENTIFICAÇÃO & DOCUMENTAÇÃO").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(8).Text(t => { t.Span(isEmpresa ? "Razão Social: " : "Nome Completo: ").Bold(); t.Span(entidade.Nome ?? "N/A"); });
                                row.RelativeItem(4).Text(t => { t.Span("Tipo: ").Bold(); t.Span(tipoClienteStr); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Nuit) ? "-" : entidade.Nuit); });
                                row.RelativeItem(6).Text(t => { t.Span("Estado: ").Bold(); t.Span(entidade.Ativo ? "Ativo" : "Inativo"); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span($"{entidade.Documento ?? "Documento"} nº: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.NrDocumento) ? "-" : entidade.NrDocumento); });
                                row.RelativeItem(6).Text(t => { t.Span("Local de Emissão: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.LocalEmissao) ? "-" : entidade.LocalEmissao); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("Data de Emissão: ").Bold(); t.Span(entidade.Emissao.HasValue ? entidade.Emissao.Value.ToString("dd/MM/yyyy") : "-"); });
                                row.RelativeItem(6).Text(t => { t.Span("Validade: ").Bold(); t.Span(entidade.Validade.HasValue ? entidade.Validade.Value.ToString("dd/MM/yyyy") : "-"); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span(isEmpresa ? "Data de Constituição: " : "Data de Nascimento: ").Bold(); t.Span(entidade.Aniversario.HasValue ? entidade.Aniversario.Value.ToString("dd/MM/yyyy") : "-"); });
                                row.RelativeItem(6).Text(t => { t.Span(isEmpresa ? "Cidade / Sede: " : "Naturalidade: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Naturalidade) ? "-" : entidade.Naturalidade); });
                            });

                            if (!isEmpresa)
                            {
                                inner.Item().PaddingTop(4).Row(row =>
                                {
                                    row.RelativeItem(6).Text(t => { t.Span("Género: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Genero) ? "-" : entidade.Genero); });
                                    row.RelativeItem(6).Text(t => { t.Span("Estado Civil: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.EstadoCivil) ? "-" : entidade.EstadoCivil); });
                                });
                            }
                        });

                        col.Item().PaddingTop(5);

                        // --- 2. DADOS PROFISSIONAIS (Se aplicável) ---
                        if (!isEmpresa)
                        {
                            col.Item().Background("#f5f7fb").Padding(5).Text("2. DADOS PROFISSIONAIS").Bold().FontSize(10).FontColor("#172b4d");

                            col.Item().PaddingTop(3).Column(inner =>
                            {
                                inner.Item().Row(row =>
                                {
                                    row.RelativeItem(6).Text(t => { t.Span("Profissão: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Profissao) ? "-" : entidade.Profissao); });
                                    row.RelativeItem(6).Text(t => { t.Span("Cargo: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Cargo) ? "-" : entidade.Cargo); });
                                });
                                inner.Item().PaddingTop(4).Text(t => { t.Span("Local de Trabalho: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.LocalTrabalho) ? "-" : entidade.LocalTrabalho); });
                            });

                            col.Item().PaddingTop(5);
                        }

                        // --- 3. FILIAÇÃO, CÔNJUGE OU REPRESENTAÇÃO LEGAL ---
                        col.Item().Background("#f5f7fb").Padding(5).Text(isEmpresa ? "3. REPRESENTAÇÃO LEGAL" : "3. FILIAÇÃO E CÔNJUGE").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            if (!isEmpresa)
                            {
                                inner.Item().Row(row =>
                                {
                                    row.RelativeItem(6).Text(t => { t.Span("Pai: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Pai) ? "-" : entidade.Pai); });
                                    row.RelativeItem(6).Text(t => { t.Span("Mãe: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Mae) ? "-" : entidade.Mae); });
                                });
                                inner.Item().PaddingTop(4);
                            }

                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span(isEmpresa ? "Representante Legal: " : "Cônjuge: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Conjuge) ? "-" : entidade.Conjuge); });
                                row.RelativeItem(6).Text(t => { t.Span("Telefone: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.TelefoneConjuge) ? "-" : entidade.TelefoneConjuge); });
                            });
                        });

                        col.Item().PaddingTop(5);

                        // --- 4. LOCALIZAÇÃO E CONTACTOS ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("4. LOCALIZAÇÃO E CONTACTOS").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            inner.Item().Text(t => { t.Span("Endereço Completo: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Endereco) ? "-" : entidade.Endereco); });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(4).Text(t => { t.Span("Principal: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Telefone) ? "-" : entidade.Telefone); });
                                row.RelativeItem(4).Text(t => { t.Span("Alternativo: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Alternativo) ? "-" : entidade.Alternativo); });
                                row.RelativeItem(4).Text(t => { t.Span("Email: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Email) ? "-" : entidade.Email); });
                            });
                        });

                        // --- LOCAL E DATA CENTRALIZADOS ---
                        col.Item().PaddingTop(20).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic();

                        col.Item().PaddingTop(15);

                        // --- ASSINATURAS ---
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(40);
                                columns.RelativeColumn();
                            });

                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Assinatura do Cliente / Representante").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(entidade.Nome ?? "N/A").FontSize(8.5f).FontColor("#4a5568");
                            });

                            table.Cell();

                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Pelo Atendimento / Operador").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(nomeEmpresa).FontSize(8.5f).FontColor("#4a5568");
                            });
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", "Ficha_Cliente_" + entidade.Id + ".pdf");
        }
    }
}
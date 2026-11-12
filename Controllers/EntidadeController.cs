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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EntidadeController(IEntidadeRepositorio entidadeRepositorio, 
            ISessao sessao,
             IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
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

            bool isEmpresa = entidade.TipoCliente == "Empresarial";
            string tipoClienteStr = isEmpresa ? "Empresarial" : "Particular";
            string numeroProcesso = $"CLI-{DateTime.Now:yyyyMMdd}-{entidade.Id}";
            string tituloFicha = isEmpresa ? "FICHA DE CLIENTE EMPRESARIAL" : "FICHA DE CLIENTE PARTICULAR";

            // Carregar imagens do disco
            byte[] logoBytes = System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png"))
                ? System.IO.File.ReadAllBytes(Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.png")) : null;

            byte[] marcaDaguaBytes = System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, "images", "MarcaAgua.png"))
                ? System.IO.File.ReadAllBytes(Path.Combine(_webHostEnvironment.WebRootPath, "images", "MarcaAgua.png")) : null;

            var documentoPdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(36);
                    page.MarginBottom(75);
                    page.MarginLeft(36);
                    page.MarginRight(36);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9.5f).FontColor("#172b4d"));

                    // Rodapé Oficial Institucional usando a constante da classe do timbrado
                    page.Footer().AlignCenter().Column(col =>
                    {
                        col.Item().LineHorizontal(1f).LineColor("#b89735");
                        col.Item().PaddingTop(4).Text(PadraoTimbradoComponent.RodapeTimbrado).FontSize(10.5f).FontColor("#079b62").Bold();
                    });

                    // Camadas (Marca d'água no fundo + Conteúdo principal à frente)
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
                            tituloFicha,
                            numeroProcesso,
                            logoBytes,
                            marcaDaguaBytes,
                            conteudo =>
                            {
                                conteudo.Column(col =>
                                {
                                    col.Spacing(8);

                                    // 1. IDENTIFICAÇÃO & DOCUMENTAÇÃO
                                    col.Item().Background("#f5f7fb").Padding(5).Text("1. IDENTIFICAÇÃO & DOCUMENTAÇÃO").Bold().FontSize(9.5f).FontColor("#172b4d");

                                    col.Item().PaddingTop(3).Column(inner =>
                                    {
                                        inner.Item().Row(row =>
                                        {
                                            row.RelativeItem(8).Text(t => { t.Span(isEmpresa ? "Razão Social: " : "Nome Completo: ").Bold(); t.Span(entidade.Nome ?? "N/A"); });
                                            row.RelativeItem(4).Text(t => { t.Span("Tipo: ").Bold(); t.Span(tipoClienteStr); });
                                        });
                                        inner.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Nuit) ? "-" : entidade.Nuit); });
                                            row.RelativeItem(6).Text(t => { t.Span("Estado: ").Bold(); t.Span(entidade.Ativo ? "Ativo" : "Inativo"); });
                                        });
                                        inner.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem(6).Text(t => { t.Span($"{entidade.Documento ?? "Documento"} nº: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.NrDocumento) ? "-" : entidade.NrDocumento); });
                                            row.RelativeItem(6).Text(t => { t.Span("Local de Emissão: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.LocalEmissao) ? "-" : entidade.LocalEmissao); });
                                        });
                                        inner.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem(6).Text(t => { t.Span("Data de Emissão: ").Bold(); t.Span(entidade.Emissao.HasValue ? entidade.Emissao.Value.ToString("dd/MM/yyyy") : "-"); });
                                            row.RelativeItem(6).Text(t => { t.Span("Validade: ").Bold(); t.Span(entidade.Validade.HasValue ? entidade.Validade.Value.ToString("dd/MM/yyyy") : "-"); });
                                        });
                                        inner.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem(6).Text(t => { t.Span(isEmpresa ? "Data de Constituição: " : "Data de Nascimento: ").Bold(); t.Span(entidade.Aniversario.HasValue ? entidade.Aniversario.Value.ToString("dd/MM/yyyy") : "-"); });
                                            row.RelativeItem(6).Text(t => { t.Span(isEmpresa ? "Cidade / Sede: " : "Naturalidade: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Naturalidade) ? "-" : entidade.Naturalidade); });
                                        });

                                        if (!isEmpresa)
                                        {
                                            inner.Item().PaddingTop(3).Row(row =>
                                            {
                                                row.RelativeItem(6).Text(t => { t.Span("Género: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Genero) ? "-" : entidade.Genero); });
                                                row.RelativeItem(6).Text(t => { t.Span("Estado Civil: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.EstadoCivil) ? "-" : entidade.EstadoCivil); });
                                            });
                                        }
                                    });

                                    col.Item().PaddingTop(4);

                                    // 2. DADOS PROFISSIONAIS (Se aplicável)
                                    if (!isEmpresa)
                                    {
                                        col.Item().Background("#f5f7fb").Padding(5).Text("2. DADOS PROFISSIONAIS").Bold().FontSize(9.5f).FontColor("#172b4d");

                                        col.Item().PaddingTop(3).Column(inner =>
                                        {
                                            inner.Item().Row(row =>
                                            {
                                                row.RelativeItem(6).Text(t => { t.Span("Profissão: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Profissao) ? "-" : entidade.Profissao); });
                                                row.RelativeItem(6).Text(t => { t.Span("Cargo: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Cargo) ? "-" : entidade.Cargo); });
                                            });
                                            inner.Item().PaddingTop(3).Text(t => { t.Span("Local de Trabalho: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.LocalTrabalho) ? "-" : entidade.LocalTrabalho); });
                                        });

                                        col.Item().PaddingTop(4);
                                    }

                                    // 3. FILIAÇÃO, CÔNJUGE OU REPRESENTAÇÃO LEGAL
                                    col.Item().Background("#f5f7fb").Padding(5).Text(isEmpresa ? "3. REPRESENTAÇÃO LEGAL" : "3. FILIAÇÃO E CÔNJUGE").Bold().FontSize(9.5f).FontColor("#172b4d");

                                    col.Item().PaddingTop(3).Column(inner =>
                                    {
                                        if (!isEmpresa)
                                        {
                                            inner.Item().Row(row =>
                                            {
                                                row.RelativeItem(6).Text(t => { t.Span("Pai: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Pai) ? "-" : entidade.Pai); });
                                                row.RelativeItem(6).Text(t => { t.Span("Mãe: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Mae) ? "-" : entidade.Mae); });
                                            });
                                        }

                                        inner.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem(6).Text(t => { t.Span(isEmpresa ? "Representante Legal: " : "Cônjuge: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Conjuge) ? "-" : entidade.Conjuge); });
                                            row.RelativeItem(6).Text(t => { t.Span("Telefone: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.TelefoneConjuge) ? "-" : entidade.TelefoneConjuge); });
                                        });
                                    });

                                    col.Item().PaddingTop(4);

                                    // 4. LOCALIZAÇÃO E CONTACTOS
                                    col.Item().Background("#f5f7fb").Padding(5).Text("4. LOCALIZAÇÃO E CONTACTOS").Bold().FontSize(9.5f).FontColor("#172b4d");

                                    col.Item().PaddingTop(3).Column(inner =>
                                    {
                                        inner.Item().Text(t => { t.Span("Endereço Completo: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Endereco) ? "-" : entidade.Endereco); });
                                        inner.Item().PaddingTop(3).Row(row =>
                                        {
                                            row.RelativeItem(4).Text(t => { t.Span("Principal: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Telefone) ? "-" : entidade.Telefone); });
                                            row.RelativeItem(4).Text(t => { t.Span("Alternativo: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Alternativo) ? "-" : entidade.Alternativo); });
                                            row.RelativeItem(4).Text(t => { t.Span("Email: ").Bold(); t.Span(string.IsNullOrEmpty(entidade.Email) ? "-" : entidade.Email); });
                                        });
                                    });

                                    // LOCAL E DATA CENTRALIZADOS
                                    var culturaPt = new System.Globalization.CultureInfo("pt-PT");
                                    col.Item().PaddingTop(15).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(8.5f).Italic();

                                    col.Item().PaddingTop(12);

                                    // ASSINATURAS (Utilizando a constante da empresa)
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
                                            c.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor("#718096");
                                            c.Item().Text("Assinatura do Cliente / Representante").FontSize(8.5f).Bold();
                                            c.Item().PaddingTop(2).Text(entidade.Nome ?? "N/A").FontSize(8f).FontColor("#4a5568");
                                        });

                                        table.Cell();

                                        table.Cell().Column(c =>
                                        {
                                            c.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor("#718096");
                                            c.Item().Text("Pelo Atendimento / Operator").FontSize(8.5f).Bold();
                                            c.Item().PaddingTop(2).Text(PadraoTimbradoComponent.NomeEmpresa).FontSize(8f).FontColor("#4a5568");
                                        });
                                    });
                                });
                            }));
                    });
                });
            });

            byte[] bytes = documentoPdf.GeneratePdf();
            return File(bytes, "application/pdf", "Ficha_Cliente_" + entidade.Id + ".pdf");
        }
    }
}
using Analise.Data;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Analise.Controllers
{
    public class PedidoController : Controller
    {
        private readonly IPedidoRepositorio _pedidoRepositorio;
        private readonly IEntidadeRepositorio _entidadeRepositorio;
        private readonly IConfiguracaoRepositorio _configuracaoRepositorio;
        private readonly IContaRepositorio _contaRepositorio;
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly BancoContext _bancoContext;
        private readonly ISessao _sessao;

        public PedidoController(
            IPedidoRepositorio pedidoRepositorio,
            IEntidadeRepositorio entidadeRepositorio,
            IConfiguracaoRepositorio configuracaoRepositorio,
            IUsuarioRepositorio usuarioRepositorio,
            IContaRepositorio contaRepositorio,
            BancoContext bancoContext,
            ISessao sessao)
        {
            _pedidoRepositorio = pedidoRepositorio;
            _entidadeRepositorio = entidadeRepositorio;
            _configuracaoRepositorio = configuracaoRepositorio;
            _usuarioRepositorio = usuarioRepositorio;
            _contaRepositorio = contaRepositorio;
            _bancoContext = bancoContext;
            _sessao = sessao;
        }

        public async Task<IActionResult> Ver()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedidos = await _pedidoRepositorio.BuscarTodosAsync();
            var viewModel = new PedidoViewModel { ListaPedidos = pedidos };
            return View(viewModel);
        }

        public async Task<IActionResult> Criar()
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var entidades = _entidadeRepositorio.BuscarTodos();
            var usuarios = _usuarioRepositorio.BuscarTodos();

            var pedidoModel = new PedidoModel
            {
                ListaEntidades = entidades.Select(e => new SelectListItem { Text = e.Nome, Value = e.Id.ToString() }).ToList(),
                ListaGestores = usuarios.Select(u => new SelectListItem { Text = u.Login, Value = u.Id.ToString() }).ToList(),
                AgenciaId = usuarioLogado.AgenciaId ?? 1,
                UsuarioId = usuarioLogado.Id
            };

            return View(pedidoModel);
        }

        [HttpGet]
        public async Task<IActionResult> VerificarPedidoAtivo(int entidadeId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return Unauthorized();

            var todosPedidos = await _pedidoRepositorio.BuscarTodosAsync();
            var pedidoAtivo = todosPedidos.FirstOrDefault(p => p.EntidadeId == entidadeId && (p.EstadoId == 1 || p.EstadoId == 2));

            if (pedidoAtivo != null)
            {
                return Json(new { possuiPedidoAtivo = true, pedidoId = pedidoAtivo.Id, estado = pedidoAtivo.EstadoId == 1 ? "Pendente" : "Aprovado" });
            }

            return Json(new { possuiPedidoAtivo = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(PedidoModel pedido)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var todosPedidos = await _pedidoRepositorio.BuscarTodosAsync();
            var pedidoExistente = todosPedidos.FirstOrDefault(p => p.EntidadeId == pedido.EntidadeId && (p.EstadoId == 1 || p.EstadoId == 2));

            if (pedidoExistente != null)
            {
                string estadoTexto = pedidoExistente.EstadoId == 1 ? "Pendente" : "Aprovado";
                TempData["MensagemErro"] = $"A entidade selecionada já possui um pedido {estadoTexto} (Nº #{pedidoExistente.Id}).";
                return RedirectToAction(nameof(Detalhes), new { id = pedidoExistente.Id });
            }

            ModelState.Remove("CRR");
            ModelState.Remove("Prestacao");
            ModelState.Remove("EstadoId");
            ModelState.Remove("AgenciaId");
            ModelState.Remove("DataCadastro");
            ModelState.Remove("Entidade");
            ModelState.Remove("Gestor");
            ModelState.Remove("Usuario");
            ModelState.Remove("ValorDecisao");
            ModelState.Remove("PrazoDecisao");
            ModelState.Remove("TaxaDecisao");
            ModelState.Remove("PrestacaoDecisao");
            ModelState.Remove("ListaEntidades");
            ModelState.Remove("ListaUsuarios");
            ModelState.Remove("ListaGestores");

            if (ModelState.IsValid)
            {
                pedido.UsuarioId = usuarioLogado.Id;
                pedido.AgenciaId = usuarioLogado.AgenciaId ?? 1;

                await _pedidoRepositorio.AdicionarAsync(pedido);
                TempData["MensagemSucesso"] = "Pedido registado com sucesso!";
                return RedirectToAction(nameof(Ver));
            }

            var entidades = _entidadeRepositorio.BuscarTodos();
            var usuarios = _usuarioRepositorio.BuscarTodos();

            pedido.ListaEntidades = entidades.Select(e => new SelectListItem { Text = e.Nome, Value = e.Id.ToString() }).ToList();
            pedido.ListaGestores = usuarios.Select(u => new SelectListItem { Text = u.Login, Value = u.Id.ToString() }).ToList();

            TempData["MensagemErro"] = "Por favor, verifique os campos preenchidos.";
            return View(pedido);
        }

        public async Task<IActionResult> Detalhes(int? id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");
            if (id == null) return NotFound();

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id.Value);
            if (pedido == null) return NotFound();

            return View(pedido);
        }

        // --- GESTÃO DE GARANTIAS (DENTRO DO PEDIDO REPOSITÓRIO) ---

        public async Task<IActionResult> Garantia(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            var garantiasDoPedido = await _pedidoRepositorio.BuscarGarantiasPorEntidadeECrrAsync(pedido.EntidadeId, pedido.CRR);

            var viewModel = new GarantiaViewModel
            {
                PedidoId = pedido.Id,
                EntidadeId = pedido.EntidadeId,
                NomeEntidade = pedido.Entidade != null ? pedido.Entidade.Nome : "Entidade #" + pedido.EntidadeId,
                Crr = pedido.CRR,
                NovaGarantia = new GarantiaModel
                {
                    EntidadeId = pedido.EntidadeId,
                    Crr = pedido.CRR
                },
                ListaGarantias = garantiasDoPedido
            };

            return View(viewModel); // Views/Pedido/Garantia.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarGarantia(GarantiaViewModel model, int pedidoId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            ModelState.Remove("NovaGarantia.Entidade");
            ModelState.Remove("NovaGarantia.Usuario");
            ModelState.Remove("NomeEntidade");

            if (ModelState.IsValid)
            {
                model.NovaGarantia.UsuarioId = usuarioLogado.Id;
                model.NovaGarantia.AgenciaId = usuarioLogado.AgenciaId ?? 1;
                model.NovaGarantia.DataCadastro = DateTime.Now;

                await _pedidoRepositorio.AdicionarGarantiaAsync(model.NovaGarantia);
                TempData["MensagemSucesso"] = "Garantia registada com sucesso!";

                return RedirectToAction(nameof(Garantia), new { id = pedidoId });
            }

            TempData["MensagemErro"] = "Por favor, preencha corretamente os campos da garantia.";
            return RedirectToAction(nameof(Garantia), new { id = pedidoId });
        }

        // --- GESTÃO DE INTERVENIENTES ---

        // GET: /Pedido/Intervenientes/5
        [ActionName("Interveniente")]
        public async Task<IActionResult> Intervenientes(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            // Buscar todos os intervenientes já associados a este pedido/CRR
            var intervenientesDoPedido = await _pedidoRepositorio.BuscarIntervenientesPorPedidoAsync(pedido.EntidadeId, pedido.CRR);

            // Obter IDs que devem ser excluídos da caixa de seleção:
            // 1. A entidade principal do pedido
            // 2. Entidades que já constam como intervenientes neste mesmo CRR
            var idsExcluidos = intervenientesDoPedido.Select(i => i.IntervenienteID).ToList();
            idsExcluidos.Add(pedido.EntidadeId);

            // Filtrar a lista de entidades disponíveis removendo as excluídas
            var entidadesDisponiveis = _entidadeRepositorio.BuscarTodos()
                .Where(e => !idsExcluidos.Contains(e.Id))
                .ToList();

            // Carregar o dicionário de garantias para cada interveniente neste CRR
            var garantiasMap = new Dictionary<int, List<GarantiaModel>>();
            foreach (var inter in intervenientesDoPedido)
            {
                var garantiasInter = await _pedidoRepositorio.BuscarGarantiasIntervenienteAsync(inter.IntervenienteID, pedido.CRR);
                garantiasMap[inter.IntervenienteID] = garantiasInter;
            }

            var viewModel = new IntervenienteViewModel
            {
                PedidoId = pedido.Id,
                EntidadeId = pedido.EntidadeId,
                NomeEntidade = pedido.Entidade != null ? pedido.Entidade.Nome : "Entidade #" + pedido.EntidadeId,
                Crr = pedido.CRR,
                NovoInterveniente = new IntervenienteModel
                {
                    EntidadeId = pedido.EntidadeId,
                    Crr = pedido.CRR
                },
                ListaEntidades = entidadesDisponiveis.Select(e => new SelectListItem
                {
                    Text = e.Nome,
                    Value = e.Id.ToString()
                }).ToList(),
                ListaIntervenientes = intervenientesDoPedido,
                GarantiasPorInterveniente = garantiasMap
            };

            return View("Interveniente", viewModel);
        }

        // POST: /Pedido/AdicionarInterveniente
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("AdicionarInterveniente")]
        public async Task<IActionResult> AdicionarInterveniente(IntervenienteViewModel model, int pedidoId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            // Remover do ModelState propriedades de navegação e auditoria que não vêm do formulário HTML
            ModelState.Remove("NovoInterveniente.Entidade");
            ModelState.Remove("NovoInterveniente.IntervenienteEntidade");
            ModelState.Remove("NovoInterveniente.Usuario");
            ModelState.Remove("NomeEntidade");
            ModelState.Remove("ListaEntidades");
            ModelState.Remove("NovaGarantiaInterveniente.Tipo");
            ModelState.Remove("NovaGarantiaInterveniente.Descricao");

            // Forçar a atribuição correta dos campos ocultos caso venham a nulo por binding
            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(pedidoId);
            if (pedido != null)
            {
                model.NovoInterveniente.EntidadeId = pedido.EntidadeId;
                model.NovoInterveniente.Crr = pedido.CRR;
            }

            // Validar se os campos essenciais foram realmente submetidos
            if (model.NovoInterveniente.IntervenienteID <= 0 || string.IsNullOrEmpty(model.NovoInterveniente.Tipo))
            {
                TempData["MensagemErro"] = "Por favor, selecione a entidade e o tipo de interveniente.";
                return RedirectToAction("Interveniente", new { id = pedidoId });
            }

            // Se chegou aqui, ignoramos erros secundários de validação das propriedades não mapeadas e gravamos
            model.NovoInterveniente.UsuarioId = usuarioLogado.Id;
            model.NovoInterveniente.AgenciaId = usuarioLogado.AgenciaId ?? 1;
            model.NovoInterveniente.DataCadastro = DateTime.Now;

            await _pedidoRepositorio.AdicionarIntervenienteAsync(model.NovoInterveniente);
            TempData["MensagemSucesso"] = "Interveniente adicionado com sucesso!";

            return RedirectToAction("Interveniente", new { id = pedidoId });
        }

        // POST: /Pedido/RemoverInterveniente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverInterveniente(int id, int pedidoId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            await _pedidoRepositorio.RemoverIntervenienteAsync(id);
            TempData["MensagemSucesso"] = "Interveniente removido com sucesso!";

            return RedirectToAction("Interveniente", new { id = pedidoId });
        }

        // POST: /Pedido/CriarGarantiaInterveniente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CriarGarantiaInterveniente(IntervenienteViewModel model, int pedidoId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            ModelState.Remove("NovaGarantiaInterveniente.Entidade");
            ModelState.Remove("NovaGarantiaInterveniente.Usuario");
            ModelState.Remove("NovoInterveniente.Entidade");
            ModelState.Remove("NovoInterveniente.IntervenienteEntidade");
            ModelState.Remove("NovoInterveniente.Usuario");
            ModelState.Remove("NovoInterveniente.Tipo");
            ModelState.Remove("NomeEntidade");
            ModelState.Remove("ListaEntidades");

            int intervenienteTargetId = model.NovaGarantiaInterveniente.EntidadeId;

            if (string.IsNullOrEmpty(model.NovaGarantiaInterveniente.Tipo) ||
                string.IsNullOrEmpty(model.NovaGarantiaInterveniente.Descricao))
            {
                TempData["MensagemErro"] = "Por favor, selecione o Tipo e preencha a Descrição da garantia.";
                TempData["OpenModalIntervenienteId"] = intervenienteTargetId; // Mantém a modal aberta
                return RedirectToAction("Interveniente", new { id = pedidoId });
            }

            model.NovaGarantiaInterveniente.UsuarioId = usuarioLogado.Id;
            model.NovaGarantiaInterveniente.AgenciaId = usuarioLogado.AgenciaId ?? 1;
            model.NovaGarantiaInterveniente.DataCadastro = DateTime.Now;

            await _pedidoRepositorio.AdicionarGarantiaIntervenienteAsync(model.NovaGarantiaInterveniente);
            TempData["MensagemSucesso"] = "Garantia do interveniente registada com sucesso!";
            TempData["OpenModalIntervenienteId"] = intervenienteTargetId; // Reabre a modal para continuar a visualizar/adicionar

            return RedirectToAction("Interveniente", new { id = pedidoId });
        }
        // GET: /Pedido/Decisao/5
        public async Task<IActionResult> Decisao(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            // Garantias do Cliente Principal
            var garantiasCliente = await _pedidoRepositorio.BuscarGarantiasClienteAsync(pedido.EntidadeId, pedido.CRR);
            pedido.TotalGarantiasCliente = garantiasCliente.Sum(g => g.ValorActual);

            // Intervenientes do Pedido e suas Garantias
            var intervenientes = await _pedidoRepositorio.BuscarIntervenientesPorPedidoAsync(pedido.EntidadeId, pedido.CRR);

            decimal totalGarantiasIntervenientes = 0;
            var garantiasMap = new Dictionary<int, List<GarantiaModel>>();

            foreach (var inter in intervenientes)
            {
                var garantiasInter = await _pedidoRepositorio.BuscarGarantiasIntervenienteAsync(inter.IntervenienteID, pedido.CRR);
                garantiasMap[inter.IntervenienteID] = garantiasInter;
                totalGarantiasIntervenientes += garantiasInter.Sum(g => g.ValorActual);
            }

            pedido.TotalGarantiasIntervenientes = totalGarantiasIntervenientes;

            // Dados auxiliares para renderizar na view
            ViewBag.GarantiasCliente = garantiasCliente;
            ViewBag.Intervenientes = intervenientes;
            ViewBag.GarantiasPorInterveniente = garantiasMap;

            return View("Decisao", pedido);
        }

        // POST: /Pedido/ProcessarDecisao
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarDecisao(int id, decimal? valorDecisao, int? prazoDecisao, decimal? taxaDecisao, decimal? prestacaoDecisao, string decisao)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            if (decisao == "Aprovar")
            {
                pedido.EstadoId = 2; // Aprovado
                pedido.ValorDecisao = valorDecisao;
                pedido.PrazoDecisao = prazoDecisao;
                pedido.TaxaDecisao = taxaDecisao;
                pedido.PrestacaoDecisao = prestacaoDecisao;

                TempData["MensagemSucesso"] = "Pedido aprovado com sucesso!";
            }
            else if (decisao == "Rejeitar")
            {
                pedido.EstadoId = 4; // Rejeitado
                pedido.ValorDecisao = valorDecisao;
                pedido.PrazoDecisao = prazoDecisao;
                pedido.TaxaDecisao = taxaDecisao;
                pedido.PrestacaoDecisao = prestacaoDecisao;

                TempData["MensagemSucesso"] = "Pedido rejeitado com sucesso.";
            }
            else
            {
                TempData["MensagemErro"] = "Opção de decisão inválida.";
                return RedirectToAction("Decisao", new { id });
            }

            await _pedidoRepositorio.AtualizarDecisaoPedidoAsync(pedido);

            return RedirectToAction("Ver", "Pedido");
        }
        // GET: /Pedido/Documentos/5
        public async Task<IActionResult> Documentos(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            // 1. Carregar garantias da Entidade Principal (Cliente)
            var garantiasCliente = await _pedidoRepositorio.BuscarGarantiasClienteAsync(pedido.EntidadeId, pedido.CRR);
            pedido.TotalGarantiasCliente = garantiasCliente.Sum(g => g.ValorActual);

            // 2. Carregar intervenientes do pedido/CRR
            var intervenientes = await _pedidoRepositorio.BuscarIntervenientesPorPedidoAsync(pedido.EntidadeId, pedido.CRR);

            decimal totalGarantiasIntervenientes = 0;
            var garantiasMap = new Dictionary<int, List<GarantiaModel>>();

            foreach (var inter in intervenientes)
            {
                var garantiasInter = await _pedidoRepositorio.BuscarGarantiasIntervenienteAsync(inter.IntervenienteID, pedido.CRR);
                garantiasMap[inter.IntervenienteID] = garantiasInter;
                totalGarantiasIntervenientes += garantiasInter.Sum(g => g.ValorActual);
            }

            pedido.TotalGarantiasIntervenientes = totalGarantiasIntervenientes;

            // 3. Carregar o Plano de Amortização gravado na tabela Amortizacoes
            var listaAmortizacoes = await _bancoContext.Amortizacoes
                .Where(a => a.PedidoId == id)
                .OrderBy(a => a.NR)
                .ToListAsync();

            // 4. Passar dados auxiliares via ViewBag para a View
            ViewBag.GarantiasCliente = garantiasCliente;
            ViewBag.Intervenientes = intervenientes;
            ViewBag.GarantiasPorInterveniente = garantiasMap;
            ViewBag.ListaAmortizacoes = listaAmortizacoes;

            return View("Documento", pedido);
        }
        // GET: /Pedido/GerarContratoPdf/5
        public async Task<IActionResult> GerarContratoPdf(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            var garantiasCliente = await _pedidoRepositorio.BuscarGarantiasClienteAsync(pedido.EntidadeId, pedido.CRR);
            var intervenientes = await _pedidoRepositorio.BuscarIntervenientesPorPedidoAsync(pedido.EntidadeId, pedido.CRR);

            var garantiasMap = new Dictionary<int, List<GarantiaModel>>();
            foreach (var inter in intervenientes)
            {
                garantiasMap[inter.IntervenienteID] = await _pedidoRepositorio.BuscarGarantiasIntervenienteAsync(inter.IntervenienteID, pedido.CRR);
            }

            // Dados fixos do operador conforme o modelo
            string nomeEmpresa = "Nael Microcrédito E.I.";
            string emailEmpresa = "naelmicrocredito@gmail.com";
            string contactosEmpresa = "862357628 / 842357628";
            string sedeEmpresa = "Bairro Agostinho Neto, na cidade de Maputo";
            string representanteLegal = "Velma Sara Frederico";
            string cargoRepresentante = "Administrador";

            string numeroContrato = pedido.CRR.HasValue ? pedido.CRR.Value.ToString("0") : "S/N";

            decimal valorFinal = pedido.ValorDesembolsado > 0 ? pedido.ValorDesembolsado : (pedido.ValorDecisao ?? pedido.Valor);
            int prazoFinal = pedido.PrazoDecisao ?? pedido.Prazo;
            decimal taxaFinal = pedido.TaxaDecisao ?? pedido.Taxa;
            decimal custosAdmValor = valorFinal <= 10000 ? 200m : (valorFinal * 0.02m);
            string textoPrazo = prazoFinal == 1 ? "1 mês" : $"{prazoFinal} meses";

            string valorPorExtenso = ExtensoHelper.ConverterParaExtenso(valorFinal);
            string custosExtenso = ExtensoHelper.ConverterParaExtenso(custosAdmValor);

            decimal totalGarantiasCli = garantiasCliente != null ? garantiasCliente.Sum(g => g.ValorActual) : 0;
            string totalGarantiasExtenso = ExtensoHelper.ConverterParaExtenso(totalGarantiasCli);

            // Geração do PDF com QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // Cabeçalho
                        col.Item().AlignCenter().Text(nomeEmpresa).Bold().FontSize(12);
                        col.Item().AlignCenter().Text($"E-mail: {emailEmpresa}\nCont. {contactosEmpresa} \nMaputo").FontSize(9);
                        col.Item().PaddingTop(5).AlignCenter().Text("CONTRATO DE EMPRÉSTIMO").Bold().FontSize(12);
                        col.Item().AlignCenter().Text($"Contrato N.º: {numeroContrato}").Bold().FontSize(10);
                        col.Item().PaddingBottom(5);

                        // Operador e Mutuário
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("\nEntre \n");
                            text.Span(nomeEmpresa).Bold();
                            text.Span($", com sede no {sedeEmpresa}, representado neste acto por ");
                            text.Span(representanteLegal).Bold();
                            text.Span($", na qualidade de ");
                            text.Span(cargoRepresentante).Bold();
                            text.Span(", doravante designado por Operador.\n");
                        });

                        var cli = pedido.Entidade;
                        string cliNaturalidade = cli?.Naturalidade ?? "Moçambique";
                        string cliResidencia = cli?.Endereco ?? "Maputo";
                        string cliDocTipo = cli?.Documento ?? "BI";
                        string cliDocNum = cli?.NrDocumento ?? "-";
                        string cliEmissao = cli?.Emissao?.ToString("dd-MM-yyyy") ?? "-";
                        string cliValidade = cli?.Validade?.ToString("dd-MM-yyyy") ?? "-";

                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("E\n");
                            text.Span(cli?.Nome ?? "Mutuário").Bold();
                            text.Span($", natural de {cliNaturalidade}, residente no {cliResidencia}, portador(a) de {cliDocTipo} n° {cliDocNum}, emitido aos {cliEmissao}, válido até {cliValidade}, doravante designado por Mutuário.\n");
                        });

                        if (intervenientes != null)
                        {
                            foreach (var inter in intervenientes)
                            {
                                var ent = inter.IntervenienteEntidade;
                                string interNome = ent?.Nome ?? "Interveniente";
                                string interNat = ent?.Naturalidade ?? "Moçambique";
                                string interRes = ent?.Endereco ?? "Maputo";
                                string interDocT = ent?.Documento ?? "BI";
                                string interDocN = ent?.NrDocumento ?? "-";
                                string interEmis = ent?.Emissao?.ToString("dd-MM-yyyy") ?? "-";
                                string interVal = ent?.Validade?.ToString("dd-MM-yyyy") ?? "-";

                                col.Item().Text(text =>
                                {
                                    text.Justify();
                                    text.Span("E\n");
                                    text.Span(interNome).Bold();
                                    text.Span($", natural de {interNat}, residente no {interRes}, portador(a) de {interDocT} n° {interDocN}, emitido aos {interEmis}, válido até {interVal}, doravante designado por ");
                                    text.Span(inter.Tipo).Bold();
                                    text.Span(".\n");
                                });
                            }
                        }

                        col.Item().PaddingTop(5);

                        // Cláusula Primeira
                        col.Item().AlignCenter().Text("PRIMEIRA\n(Valor do empréstimo)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Pelo presente, concede-se ao mutuário o valor de ");
                            text.Span($"{valorFinal:N2} MT").Bold();
                            text.Span($" ({valorPorExtenso}), a ser pago em ");
                            text.Span(textoPrazo).Bold();
                            text.Span(" prestação mensal sobre o capital em dívida.\n");
                        });

                        // Cláusula Segunda
                        col.Item().AlignCenter().Text("SEGUNDA\n(Taxas de juros)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("O empréstimo vence juros à taxa de ");
                            text.Span($"{taxaFinal:N2}%").Bold();
                            text.Span(" mensal sobre o capital.\n");
                        });

                        // Cláusula Terceira
                        col.Item().AlignCenter().Text("TERCEIRA\n(Modo e lugar de reembolso)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span($"1. O mutuário aceita expressamente devolver o crédito dentro de ");
                            text.Span(textoPrazo).Bold();
                            text.Span(", contados a partir da data do desembolso do crédito, bem como de acordo com o plano de pagamento em anexo no contrato.\n2. O mutuário compromete-se ainda, a efetuar os reembolsos do crédito nas contas enquadradas no plano de pagamento, apresentando o comprovativo do reembolso junto às instalações do operador, para que lhe seja passado o recibo que confirma a receção do valor pelo Operador.\n");
                        });

                        // Cláusula Quarta
                        col.Item().AlignCenter().Text("QUARTA\n(Custos administrativos)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Os custos administrativos associados à operação de crédito, no valor de ");
                            text.Span($"{custosAdmValor:N2} MT").Bold();
                            text.Span($" ({custosExtenso}), são da inteira responsabilidade do mutuário.\n");
                        });

                        // Cláusula Quinta
                        col.Item().AlignCenter().Text("QUINTA\n(Vencimento imediato)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Pode ser considerado imediatamente vencido o presente contrato exigindo-se de imediato, o pagamento de todo o valor de capital e juros, ainda em dívida, bem como todos os outros encargos devidos pelo mutuário ao Operador por força deste empréstimo em caso de falta de pagamento pontual ou infração das cláusulas.\n");
                        });

                        // Cláusula Sexta
                        col.Item().AlignCenter().Text("SEXTA\n(Garantias do mutuário)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("1. Para assegurar o reembolso do capital, juros e demais encargos inerentes ao empréstimo, o mutuário constitui garantias para o empréstimo recebido a favor de ");
                            text.Span(nomeEmpresa).Bold();
                            text.Span(", no valor total de ");
                            text.Span($"{totalGarantiasCli:N2} MT").Bold();
                            text.Span($" ({totalGarantiasExtenso}), que ficarão na posse do mutuário. Fazem parte integrante das garantias do cliente os seguintes bens:\n");
                        });

                        // Tabela de Garantias do Cliente
                        if (garantiasCliente != null && garantiasCliente.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(75);
                                    columns.RelativeColumn(25);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#D3D3D3").Padding(4).Text("DESCRIÇÃO").Bold();
                                    header.Cell().Background("#D3D3D3").Padding(4).AlignRight().Text("VALOR").Bold();
                                });

                                foreach (var g in garantiasCliente)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(4).Text(g.Descricao);
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(4).AlignRight().Text($"{g.ValorActual:N2} MT");
                                }
                            });
                        }

                        // Garantias dos Intervenientes
                        if (intervenientes != null)
                        {
                            foreach (var inter in intervenientes)
                            {
                                if (garantiasMap.ContainsKey(inter.IntervenienteID) && garantiasMap[inter.IntervenienteID].Any())
                                {
                                    var gList = garantiasMap[inter.IntervenienteID];
                                    string nomeIntervEnt = inter.IntervenienteEntidade?.Nome ?? "Interveniente";

                                    col.Item().PaddingTop(5).Text(text =>
                                    {
                                        text.Justify();
                                        text.Span("Ainda fazem parte integrante das garantias do crédito os seguintes bens do ");
                                        text.Span(inter.Tipo.ToLower()).Bold();
                                        text.Span(" (");
                                        text.Span(nomeIntervEnt).Bold();
                                        text.Span("):\n");
                                    });

                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn(75);
                                            columns.RelativeColumn(25);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Background("#D3D3D3").Padding(4).Text("DESCRIÇÃO").Bold();
                                            header.Cell().Background("#D3D3D3").Padding(4).AlignRight().Text("VALOR").Bold();
                                        });

                                        foreach (var g in gList)
                                        {
                                            table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(4).Text(g.Descricao);
                                            table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(4).AlignRight().Text($"{g.ValorActual:N2} MT");
                                        }
                                    });
                                }
                            }
                        }

                        // Cláusulas Finais
                        col.Item().PaddingTop(5).AlignCenter().Text("SÉTIMA\n(Arresto e venda dos bens)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("1. O presente penhor pode ser executado logo que, vencida qualquer obrigação que sirva de garantia, verifique-se mora no seu cumprimento.\n2. O Operador fica desde já autorizado a proceder à venda extrajudicial dos bens empenhados a quem melhor entender.\n");
                        });

                        col.Item().AlignCenter().Text("OITAVA\n(Sanções)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Caso se verifique falta de pagamento pontual das prestações, o mutuário aceita a aplicação de taxas de juros de mora diárias correspondentes a 2% sobre as prestações em atraso.\n");
                        });

                        col.Item().AlignCenter().Text("NONA\n(Lei do foro)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Quaisquer diferendos relacionados com o presente contrato serão resolvidos amigavelmente ou, caso contrário, no Tribunal Judicial da Cidade local.\n");
                        });

                        // Assinaturas
                        col.Item().PaddingTop(20);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().PaddingBottom(20).AlignCenter().Text("_______________________\nAssinatura do Operador");
                            table.Cell().PaddingBottom(20).AlignCenter().Text("_______________________\nAssinatura do Mutuário");

                            if (intervenientes != null)
                            {
                                foreach (var inter in intervenientes)
                                {
                                    string nomeInter = inter.IntervenienteEntidade?.Nome ?? inter.Tipo;
                                    table.Cell().PaddingBottom(20).AlignCenter().Text($"_______________________\n{inter.Tipo} ({nomeInter})");
                                }
                            }
                        });

                        col.Item().PaddingTop(10).AlignCenter().Text($"Maputo, {DateTime.Now:dd/MM/yyyy}");
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Contrato_Emprestimo_Pedido_{pedido.Id}.pdf");
        }
        // GET: /Pedido/Despacho (aceita opcionalmente id ou crr, ou ajustamos para aceitar id como ID do pedido)
        public async Task<IActionResult> Despacho(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            // Buscar o pedido pelo ID (mantendo a compatibilidade com a rota /Pedido/Despacho/1)
            var pedido = await _bancoContext.Pedidos
                .Include(p => p.Entidade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                TempData["MensagemErro"] = "Pedido não encontrado.";
                return RedirectToAction("Ver");
            }

            // 1. Verificar se os Custos Administrativos já foram pagos/registados para este pedido
            bool custoPago = await _bancoContext.CustosAdm.AnyAsync(c => c.PedidoId == id);
            ViewBag.CustoPago = custoPago;

            // Carregar a lista de contas bancárias para preencher o select de desembolso
            ViewBag.ListaContas = await _contaRepositorio.BuscarTodasAsync();

            return View(pedido);
        }

        // POST: /Pedido/SalvarCustosAdm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalvarCustosAdm(CustosAdmModel custos)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _bancoContext.Pedidos
                .Include(p => p.Entidade)
                .FirstOrDefaultAsync(p => p.Id == custos.PedidoId);

            if (pedido == null)
            {
                TempData["MensagemErro"] = "Pedido não encontrado.";
                return RedirectToAction("Ver");
            }

            decimal valorComissao = pedido.ValorDecisao <= 10000 ? 200m : (pedido.ValorDecisao ?? 0) * 0.02m;

            using var transaction = await _bancoContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Registar Custos Adm
                custos.CRR = pedido.CRR;
                custos.EntidadeId = pedido.EntidadeId;
                custos.Valor = valorComissao;                
                custos.UsuarioId = usuarioLogado.Id;
                custos.AgenciaId = usuarioLogado.AgenciaId;
                custos.DataCadastro = DateTimeOffset.Now;

                // Limpar propriedades de navegação para evitar conflitos de tracking no EF
                custos.Pedido = null;
                custos.Entidade = null;
                custos.Conta = null;
                custos.Usuario = null;

                _bancoContext.CustosAdm.Add(custos);

                string nomeCliente = pedido.Entidade?.Nome ?? "Cliente";
                string descricaoMovimento = $"Custos admin: {nomeCliente}";

                // 2. Movimento Interno
                var movimentoInterno = new MovimentoInternoModel
                {
                    Descricao = descricaoMovimento,
                    Credito = valorComissao,
                    ContaOrigemId = custos.ContaId,
                    ContaDestinoId = custos.ContaId,
                    TipoOperacao = "Custos Administrativos",
                    DataCadastro = DateTimeOffset.Now,
                    UsuarioId = usuarioLogado.Id,
                    AgenciaId = usuarioLogado.AgenciaId
                };
                _bancoContext.MovimentosInternos.Add(movimentoInterno);

                // 3. Movimento Externo
                var movimentoExterno = new MovimentoExternoModel
                {
                    Descricao = descricaoMovimento,
                    Debito = valorComissao,
                    ContaId = custos.ContaId,
                    EntidadeId = pedido.EntidadeId,
                    TipoOperacao = "Custos Administrativos",
                    DataCadastro = DateTimeOffset.Now,
                    UsuarioId = usuarioLogado.Id,
                    AgenciaId = usuarioLogado.AgenciaId
                };
                _bancoContext.MovimentosExternos.Add(movimentoExterno);

                // 4. Atualizar o Crédito da Conta Bancária selecionada
                var conta = await _bancoContext.Contas.FirstOrDefaultAsync(c => c.Id == custos.ContaId);
                if (conta != null)
                {
                    conta.Credito += valorComissao; // Atualiza a propriedade Credito da conta
                    _bancoContext.Contas.Update(conta);
                }

                await _bancoContext.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensagemSucesso"] = "Custos administrativos registados com sucesso!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Exibe a excepção interna exata no ecrã para sabermos qual o campo causador
                string detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["MensagemErro"] = "Erro detalhado: " + detalhe;
            }

            return RedirectToAction("Despacho", new { id = pedido.Id });
        }

        // Classes auxiliares para receber os dados do desembolso enviados pela view
        public class DesembolsoInputModel
        {
            public int Id { get; set; } // PedidoId
            public List<ItemDesembolsoDto> Desembolsos { get; set; }
        }

        public class ItemDesembolsoDto
        {
            public int ContaId { get; set; }
            public decimal Valor { get; set; }
        }
        // POST: /Pedido/ProcessarDesembolso
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarDesembolso(DesembolsoInputModel model)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _bancoContext.Pedidos
                .Include(p => p.Entidade)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (pedido == null)
            {
                TempData["MensagemErro"] = "Pedido não encontrado.";
                return RedirectToAction("Ver");
            }

            using var transaction = await _bancoContext.Database.BeginTransactionAsync();
            try
            {
                decimal valorTotalDesembolsado = 0;
                DateTime dataOperacao = DateTime.Today; // Data da operação / desembolso
                string nomeCliente = pedido.Entidade?.Nome ?? "Cliente";

                if (model.Desembolsos != null)
                {
                    foreach (var item in model.Desembolsos)
                    {
                        // 1. Validar e obter a Conta Bancária
                        var conta = await _bancoContext.Contas.FirstOrDefaultAsync(c => c.Id == item.ContaId);
                        if (conta == null)
                        {
                            throw new Exception("Uma das contas selecionadas não foi encontrada.");
                        }

                        // 2. Validação de segurança no Servidor: verifica se a conta tem saldo suficiente
                        decimal saldoAtualConta = conta.Credito - conta.Debito;
                        if (item.Valor > saldoAtualConta)
                        {
                            throw new Exception($"A conta {conta.Banco} - {conta.Conta} não possui saldo suficiente para este desembolso. Saldo disponível: MT {saldoAtualConta:N2}");
                        }

                        valorTotalDesembolsado += item.Valor;

                        // 3. Atualizar o Débito e o Saldo da Conta Bancária selecionada
                        conta.Debito += item.Valor;
                        _bancoContext.Contas.Update(conta);

                        string descricaoMovimento = $"Desembolso de crédito - {nomeCliente}";

                        // 4. Registar nos Movimentos Internos (com EntidadeId incluído)
                        var movimentoInterno = new MovimentoInternoModel
                        {
                            Descricao = descricaoMovimento,
                            Debito = item.Valor,
                            ContaOrigemId = item.ContaId,
                            ContaDestinoId = item.ContaId,
                            TipoOperacao = "Desembolso",
                            DataCadastro = DateTimeOffset.Now,
                            UsuarioId = usuarioLogado.Id,
                            AgenciaId = usuarioLogado.AgenciaId
                        };
                        _bancoContext.MovimentosInternos.Add(movimentoInterno);

                        // 5. Registar nos Movimentos Externos
                        var movimentoExterno = new MovimentoExternoModel
                        {
                            Descricao = descricaoMovimento,
                            Debito = item.Valor,
                            ContaId = item.ContaId,
                            EntidadeId = pedido.EntidadeId,
                            TipoOperacao = "Desembolso",
                            DataCadastro = DateTimeOffset.Now,
                            UsuarioId = usuarioLogado.Id,
                            AgenciaId = usuarioLogado.AgenciaId
                        };
                        _bancoContext.MovimentosExternos.Add(movimentoExterno);
                    }
                }

                // 6. Atualizar a tabela Pedidos (Estado Vigente = 3, Valor Desembolsado e Data do Desembolso)
                pedido.EstadoId = 3;
                pedido.ValorDesembolsado = valorTotalDesembolsado;
                pedido.DataDesembolso = dataOperacao;
                _bancoContext.Pedidos.Update(pedido);

                // 7. Gerar e Lançar o Plano de Amortização linha por linha na tabela Amortizacoes
                decimal valorAprovado = pedido.ValorDecisao ?? 0;
                decimal taxaMensal = (pedido.TaxaDecisao ?? 0) / 100m;
                int prazoMeses = pedido.PrazoDecisao ?? 0;

                decimal prestacaoFixa = 0;
                if (taxaMensal > 0)
                {
                    prestacaoFixa = valorAprovado * (taxaMensal * (decimal)Math.Pow(1 + (double)taxaMensal, prazoMeses)) / ((decimal)Math.Pow(1 + (double)taxaMensal, prazoMeses) - 1);
                }
                else
                {
                    prestacaoFixa = valorAprovado / prazoMeses;
                }

                decimal capitalEmDivida = valorAprovado;
                DateTime dataVencimento = dataOperacao;

                for (int i = 1; i <= prazoMeses; i++)
                {
                    dataVencimento = dataVencimento.AddMonths(1);
                    decimal juroMes = capitalEmDivida * taxaMensal;
                    decimal principalMes = prestacaoFixa - juroMes;
                    capitalEmDivida -= principalMes;

                    if (i == prazoMeses && capitalEmDivida < 0) capitalEmDivida = 0;

                    var amortizacao = new AmortizacaoModel
                    {
                        PedidoId = pedido.Id,
                        CRR = pedido.CRR,
                        NR = i,
                        Vencimento = dataVencimento,
                        Prestacao = Math.Round(prestacaoFixa, 2),
                        Principal = Math.Round(principalMes, 2),
                        Juro = Math.Round(juroMes, 2),
                        SaldoDevedor = Math.Round(Math.Max(0, capitalEmDivida), 2),
                        TotalPago = 0,
                        PrincipalPago = 0,
                        JuroPago = 0,
                        MultaPago = 0,
                        JuroAPerdoar = 0,
                        MultaAPerdoar = 0
                    };

                    _bancoContext.Amortizacoes.Add(amortizacao);
                }

                await _bancoContext.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["MensagemSucesso"] = "Desembolso concretizado com sucesso, movimentos registados e plano de amortização gerado!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["MensagemErro"] = "Erro ao processar desembolso: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Ver");
        }
        [HttpGet]
        public async Task<IActionResult> Pagamento(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            // Carregar o plano de amortização do pedido ordenado por número de prestação
            var listaAmortizacoes = await _bancoContext.Amortizacoes
                .Where(a => a.PedidoId == id)
                .OrderBy(a => a.NR)
                .ToListAsync();

            // Trazer TODAS as contas bancárias da empresa, independentemente do saldo
            var listaContas = await _bancoContext.Contas.ToListAsync();

            ViewBag.ListaAmortizacoes = listaAmortizacoes;
            ViewBag.ListaContas = listaContas;

            return View("Pagamento", pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessarPagamento(int pedidoId, int contaId, decimal valorDeposito, List<AmortizacaoModel> amortizacoes)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (valorDeposito <= 0)
            {
                TempData["MensagemErro"] = "O valor do depósito deve ser superior a zero.";
                return RedirectToAction("Pagamento", new { id = pedidoId });
            }

            var pedido = await _bancoContext.Pedidos
                .Include(p => p.Entidade)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                TempData["MensagemErro"] = "Pedido não encontrado.";
                return RedirectToAction("Ver");
            }

            var conta = await _bancoContext.Contas.FirstOrDefaultAsync(c => c.Id == contaId);
            if (conta == null)
            {
                TempData["MensagemErro"] = "A conta bancária selecionada é inválida.";
                return RedirectToAction("Pagamento", new { id = pedidoId });
            }

            using var transaction = await _bancoContext.Database.BeginTransactionAsync();
            try
            {
                // 1. Buscar as amortizações da base de dados ordenadas por antiguidade (NR)
                var amortizacoesDb = await _bancoContext.Amortizacoes
                    .Where(a => a.PedidoId == pedidoId)
                    .OrderBy(a => a.NR)
                    .ToListAsync();

                decimal saldoDisponivel = valorDeposito;
                decimal totalPrincipalPagoGeral = 0;
                decimal totalJuroPagoGeral = 0;
                decimal totalMultaPagoGeral = 0;

                // Atualizar os valores de perdão enviados pela View correspondentes a cada linha
                if (amortizacoes != null)
                {
                    foreach (var itemForm in amortizacoes)
                    {
                        var itemDb = amortizacoesDb.FirstOrDefault(a => a.Id == itemForm.Id);
                        if (itemDb != null)
                        {
                            itemDb.JuroAPerdoar = Math.Min(itemForm.JuroAPerdoar, itemDb.JuroPorPagar);
                            itemDb.MultaAPerdoar = Math.Min(itemForm.MultaAPerdoar, itemDb.MultaPorPagar);
                        }
                    }
                }

                // 2. Distribuir o valor depositado priorizando estritamente: Multa -> Juro -> Principal
                foreach (var item in amortizacoesDb)
                {
                    if (saldoDisponivel <= 0) break;

                    decimal multaLiquidaPendente = Math.Max(0, item.MultaPorPagar - item.MultaAPerdoar);
                    decimal juroLiquidoPendente = Math.Max(0, item.JuroPorPagar - item.JuroAPerdoar);
                    decimal principalPendente = item.PrincipalPorPagar;

                    // A) Abater Multa
                    decimal pagarMulta = Math.Min(saldoDisponivel, multaLiquidaPendente);
                    saldoDisponivel -= pagarMulta;
                    item.MultaPago += pagarMulta;
                    totalMultaPagoGeral += pagarMulta;

                    // B) Abater Juro
                    decimal pagarJuro = 0;
                    if (saldoDisponivel > 0)
                    {
                        pagarJuro = Math.Min(saldoDisponivel, juroLiquidoPendente);
                        saldoDisponivel -= pagarJuro;
                        item.JuroPago += pagarJuro;
                        totalJuroPagoGeral += pagarJuro;
                    }

                    // C) Abater Principal (Capital)
                    decimal pagarPrincipal = 0;
                    if (saldoDisponivel > 0)
                    {
                        pagarPrincipal = Math.Min(saldoDisponivel, principalPendente);
                        saldoDisponivel -= pagarPrincipal;
                        item.PrincipalPago += pagarPrincipal;
                        totalPrincipalPagoGeral += pagarPrincipal;
                    }

                    item.TotalPago = item.PrincipalPago + item.JuroPago + item.MultaPago;
                }

                decimal valorTotalEfetivamentePago = totalPrincipalPagoGeral + totalJuroPagoGeral + totalMultaPagoGeral;

                if (valorTotalEfetivamentePago <= 0)
                {
                    TempData["MensagemErro"] = "Nenhum valor foi alocado. Verifique os valores em dívida.";
                    return RedirectToAction("Pagamento", new { id = pedidoId });
                }

                // 3. Verificar se toda a dívida foi liquidada (Capital pendente total igual a 0 ou menor que uma margem infinitesimal)
                bool dividaTotalLiquidada = amortizacoesDb.All(a => a.PrincipalPorPagar <= 0);
                if (dividaTotalLiquidada)
                {
                    pedido.EstadoId = 5; // Estado Liquidado
                    pedido.CapitalPago = totalPrincipalPagoGeral;
                    _bancoContext.Pedidos.Update(pedido);
                }

                string nomeCliente = pedido.Entidade?.Nome ?? "Cliente";
                string descricaoMovimento = $"Reembolso de Crédito: {nomeCliente} (Pedido #{pedidoId})";

                // 4. Movimento Interno
                var movimentoInterno = new MovimentoInternoModel
                {
                    Descricao = descricaoMovimento,
                    Credito = valorTotalEfetivamentePago,
                    ContaOrigemId = conta.Id,
                    ContaDestinoId = conta.Id,
                    TipoOperacao = "Reembolso de Crédito",
                    DataCadastro = DateTimeOffset.Now,
                    UsuarioId = usuarioLogado.Id,
                    AgenciaId = usuarioLogado.AgenciaId
                };
                _bancoContext.MovimentosInternos.Add(movimentoInterno);

                // 5. Movimento Externo
                var movimentoExterno = new MovimentoExternoModel
                {
                    Descricao = descricaoMovimento,
                    Debito = valorTotalEfetivamentePago,
                    ContaId = conta.Id,
                    EntidadeId = pedido.EntidadeId,
                    TipoOperacao = "Reembolso de Crédito",
                    DataCadastro = DateTimeOffset.Now,
                    UsuarioId = usuarioLogado.Id,
                    AgenciaId = usuarioLogado.AgenciaId
                };
                _bancoContext.MovimentosExternos.Add(movimentoExterno);

                // 6. Atualizar o Crédito da Conta Bancária selecionada
                conta.Credito += valorTotalEfetivamentePago;
                _bancoContext.Contas.Update(conta);

                // 7. Atualizar as amortizações na base de dados
                _bancoContext.Amortizacoes.UpdateRange(amortizacoesDb);

                await _bancoContext.SaveChangesAsync();
                await transaction.CommitAsync();

                if (dividaTotalLiquidada)
                {
                    TempData["MensagemSucesso"] = $"Parabéns! O empréstimo foi totalmente liquidado. Pagamento registado: MT {valorTotalEfetivamentePago:N2}.";
                }
                else
                {
                    TempData["MensagemSucesso"] = $"Pagamento registado com sucesso! Valor distribuído: MT {valorTotalEfetivamentePago:N2} (Principal: {totalPrincipalPagoGeral:N2}, Juro: {totalJuroPagoGeral:N2}, Multa: {totalMultaPagoGeral:N2}).";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                string detalhe = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["MensagemErro"] = "Erro detalhado ao processar o pagamento: " + detalhe;
            }

            return RedirectToAction("Pagamento", new { id = pedidoId });
        }
    }
}
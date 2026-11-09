using Analise.Data;
using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
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
        [HttpGet]
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

            // 3. Tentar carregar o Plano de Amortização já gravado na tabela Amortizacoes
            var listaAmortizacoes = await _bancoContext.Amortizacoes
                .Where(a => a.PedidoId == id)
                .OrderBy(a => a.NR)
                .ToListAsync();

            // 4. SE NÃO EXISTIR PLANO GRAVADO (ex: Pedido Aprovado recentemente), GERAMOS NA HORA!
            if (listaAmortizacoes == null || !listaAmortizacoes.Any())
            {
                DateTime dataReferencia;

                // Regra de Negócio de Datas:
                // - Vigente (4) ou Liquidado (5) -> Usa DataDesembolso
                // - Aprovado (2) ou Outros -> Usa a Data Atual (DateTime.Today)
                if (pedido.EstadoId == 4 || pedido.EstadoId == 5)
                {
                    dataReferencia = pedido.DataDesembolso.HasValue ? pedido.DataDesembolso.Value.DateTime : DateTime.Today;
                }
                else
                {
                    dataReferencia = DateTime.Today;
                }

                // Chame o seu método/serviço que calcula as prestações (exemplo de lógica de cálculo ou simulação)
                // Se já tiver um simulador ou repositor de cálculo, invoque-o aqui para preencher a lista em memória:
                listaAmortizacoes = GerarPlanoAmortizacaoTemporario(pedido, dataReferencia);
            }

            // 5. Passar dados auxiliares via ViewBag para a View
            ViewBag.GarantiasCliente = garantiasCliente;
            ViewBag.Intervenientes = intervenientes;
            ViewBag.GarantiasPorInterveniente = garantiasMap;
            ViewBag.ListaAmortizacoes = listaAmortizacoes;

            return View("Documento", pedido);
        }

        // Método auxiliar para gerar o plano em memória caso não esteja gravado na BD
        private List<AmortizacaoModel> GerarPlanoAmortizacaoTemporario(PedidoModel pedido, DateTime dataInicio)
        {
            var lista = new List<AmortizacaoModel>();
            decimal valor = pedido.ValorDecisao > 0 ? pedido.ValorDecisao.Value : pedido.Valor;
            int prazo = (pedido.PrazoDecisao ?? 0) > 0 ? pedido.PrazoDecisao.Value : pedido.Prazo;
            decimal taxaMensal = (pedido.TaxaDecisao > 0 ? pedido.TaxaDecisao.Value : pedido.Taxa) / 100m;
            decimal prestacao = pedido.PrestacaoDecisao > 0 ? pedido.PrestacaoDecisao.Value : pedido.Prestacao;

            if (prestacao <= 0 && prazo > 0 && valor > 0)
            {
                // Fórmula Price básica se a prestação não estiver definida
                if (taxaMensal > 0)
                    prestacao = valor * (taxaMensal * (decimal)Math.Pow((double)(1 + taxaMensal), prazo)) / ((decimal)Math.Pow((double)(1 + taxaMensal), prazo) - 1);
                else
                    prestacao = valor / prazo;
            }

            decimal capitalEmDivida = valor;

            for (int i = 1; i <= prazo; i++)
            {
                decimal juro = capitalEmDivida * taxaMensal;
                decimal principal = prestacao - juro;
                capitalEmDivida -= principal;

                lista.Add(new AmortizacaoModel
                {
                    NR = i,
                    Vencimento = dataInicio.AddMonths(i),
                    Prestacao = prestacao,
                    Juro = juro,
                    Principal = principal,
                    SaldoDevedor = capitalEmDivida > 0 ? capitalEmDivida : 0,
                    TotalPago = 0
                    // Remova a linha 'TotalPorPagar = prestacao' se for apenas leitura no modelo
                });
            }

            return lista;
        }
        // GET: /Pedido/GerarContratoPdf/5
        [HttpGet]
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
            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string sedeEmpresa = "Maputo-Malanga, rua do Carpelo No. 77";
            string representanteLegal = "Jaime Manuel Nhabanga";
            string cargoRepresentante = "Representante Legal";

            string numeroContrato = pedido.CRR.HasValue ? pedido.CRR.Value.ToString("0") : "S/N";
            string numeroProcesso = pedido.CRR.HasValue ? $"CP-{DateTime.Now:yyyyMMdd}-{pedido.CRR.Value}" : $"CP-{DateTime.Now:yyyyMMdd}-{pedido.Id}";

            decimal valorFinal = pedido.ValorDesembolsado > 0 ? pedido.ValorDesembolsado : (pedido.ValorDecisao ?? pedido.Valor);
            int prazoFinal = pedido.PrazoDecisao ?? pedido.Prazo;
            decimal taxaFinal = pedido.TaxaDecisao ?? pedido.Taxa;
            decimal custosAdmValor = valorFinal <= 10000 ? 200m : (valorFinal * 0.02m);

            string textoPrazo = prazoFinal == 1 ? "1 mês" : $"{prazoFinal} meses";
            string textoPrestacao = prazoFinal == 1 ? "1 prestação mensal" : $"{prazoFinal} prestações mensais";

            string valorPorExtenso = ExtensoHelper.ConverterParaExtenso(valorFinal);
            string custosExtenso = ExtensoHelper.ConverterParaExtenso(custosAdmValor);

            // --- SOMA GLOBAL DE TODAS AS GARANTIAS (Cliente + Intervenientes) ---
            decimal totalGarantiasCli = garantiasCliente != null ? garantiasCliente.Sum(g => g.ValorActual) : 0;
            decimal totalGarantiasInterv = garantiasMap.Values.SelectMany(gList => gList).Sum(g => g.ValorActual);
            decimal totalGarantiasGeral = totalGarantiasCli + totalGarantiasInterv;
            string totalGarantiasGeralExtenso = ExtensoHelper.ConverterParaExtenso(totalGarantiasGeral);

            // Configurar cultura para português (meses corretos)
            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            // Geração do PDF com QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(10).FontColor("#172b4d"));

                    // Rodapé padrão em todas as páginas
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("DOCUMENTO OFICIAL DE CONTRATO — ").FontSize(8).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(8).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // --- CABEÇALHO COM O NOVO LAYOUT VISUAL ---
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(22).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(9).FontColor("#718096");
                            });

                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic().AlignRight();
                                c.Item().Text($"Nº Processo: {numeroProcesso}").Bold().FontSize(9).FontColor("#079b62").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#079b62");

                        // Título do Contrato em Bloco Verde
                        col.Item().Background("#079b62").Padding(8).AlignCenter().Text($"CONTRATO DE EMPRÉSTIMO N.º {numeroContrato}")
                            .Bold().FontSize(12).FontColor("#FFFFFF");

                        col.Item().PaddingTop(5);

                        // Operador e Mutuário
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Entre \n");
                            text.Span(nomeEmpresa).Bold();
                            text.Span($", com sede no {sedeEmpresa}, representado neste acto por ");
                            text.Span(representanteLegal).Bold();
                            text.Span($", na qualidade de ");
                            text.Span(cargoRepresentante).Bold();
                            text.Span(", doravante designado por Operador.");
                        });

                        var cli = pedido.Entidade;
                        string cliNaturalidade = cli?.Naturalidade ?? "Moçambique";
                        string cliResidencia = cli?.Endereco ?? "Maputo";
                        string cliDocTipo = cli?.Documento ?? "BI";
                        string cliDocNum = cli?.NrDocumento ?? "-";
                        string cliEmissao = cli?.Emissao?.ToString("dd-MM-yyyy") ?? "-";
                        string cliValidade = cli?.Validade?.ToString("dd-MM-yyyy") ?? "-";
                        string nomeCliente = cli?.Nome ?? "Mutuário";

                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("E\n");
                            text.Span(nomeCliente).Bold();
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
                            text.Span(textoPrestacao).Bold();
                            text.Span(" sobre o capital em dívida.\n");
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
                            text.Span($" ({custosExtenso}), encontram-se temporariamente isentos para o mutuário por decisão promocional e excepcional.\n");
                        });

                        // Cláusula Quinta
                        col.Item().AlignCenter().Text("QUINTA\n(Vencimento imediato)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("Pode ser considerado imediatamente vencido o presente contrato exigindo-se de imediato, o pagamento de todo o valor de capital e juros, ainda em dívida, bem como todos os outros encargos devidos pelo mutuário ao Operador por força deste empréstimo em caso de falta de pagamento pontual ou infração das cláusulas.\n");
                        });

                        // Cláusula Sexta (Com somatório global de todas as garantias)
                        col.Item().AlignCenter().Text("SEXTA\n(Garantias do mutuário)").Bold().FontSize(10);
                        col.Item().Text(text =>
                        {
                            text.Justify();
                            text.Span("1. Para assegurar o reembolso do capital, juros e demais encargos inerentes ao empréstimo, o mutuário constitui garantias para o empréstimo recebido a favor de ");
                            text.Span(nomeEmpresa).Bold();
                            text.Span(", no valor total de ");
                            text.Span($"{totalGarantiasGeral:N2} MT").Bold();
                            text.Span($" ({totalGarantiasGeralExtenso}), que ficarão na posse do mutuário. Fazem parte integrante das garantias do cliente os seguintes bens:\n");
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

                        // Local e Data centralizados antes das assinaturas
                        col.Item().PaddingTop(20).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic();

                        // Assinaturas
                        col.Item().PaddingTop(15);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                            });

                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(15).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Assinatura do Operador").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(nomeEmpresa).FontSize(8.5f).FontColor("#4a5568");
                            });

                            table.Cell();

                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(15).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Assinatura do Mutuário").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(nomeCliente).FontSize(8.5f).FontColor("#4a5568");
                            });

                            if (intervenientes != null)
                            {
                                foreach (var inter in intervenientes)
                                {
                                    string nomeInter = inter.IntervenienteEntidade?.Nome ?? inter.Tipo;
                                    table.Cell().Column(c =>
                                    {
                                        c.Item().PaddingTop(15).PaddingBottom(15).LineHorizontal(0.5f).LineColor("#718096");
                                        c.Item().Text($"{inter.Tipo}").FontSize(9).Bold();
                                        c.Item().PaddingTop(2).Text(nomeInter).FontSize(8.5f).FontColor("#4a5568");
                                    });
                                    table.Cell();
                                    table.Cell();
                                }
                            }
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Contrato_Emprestimo_Pedido_{pedido.Id}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> GerarFormularioPedidoPdf(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            var cli = pedido.Entidade;

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string numeroProcesso = pedido.CRR.HasValue ? $"CP-{DateTime.Now:yyyyMMdd}-{pedido.CRR.Value}" : $"CP-{DateTime.Now:yyyyMMdd}-{pedido.Id}";

            decimal valorSolicitado = pedido.Valor;
            int prazoMeses = pedido.Prazo;
            string finalidadeDestino = !string.IsNullOrEmpty(pedido.Destino) ? pedido.Destino : "Expansão de Negócio / Apoio Financeiro";

            string nomeCliente = cli?.Nome ?? "N/A";
            string nuitCliente = cli?.Nuit ?? "N/A";
            string biCliente = cli?.NrDocumento ?? "N/A";
            string contactoCliente = cli?.Telefone ?? cli?.Telefone ?? "N/A";
            string enderecoCliente = cli?.Endereco ?? "Maputo";

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
                        text.Span("DOCUMENTO PARA IMPRESSÃO E ASSINATURA — ").FontSize(8).Italic().FontColor("#a0aec0");
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

                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic().AlignRight();
                                c.Item().Text($"Nº Processo: {numeroProcesso}").Bold().FontSize(9).FontColor("#079b62").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#079b62");

                        // Título em Bloco Verde
                        col.Item().Background("#079b62").Padding(8).AlignCenter().Text("FORMULÁRIO DE PEDIDO DE CRÉDITO")
                            .Bold().FontSize(12).FontColor("#FFFFFF");

                        col.Item().PaddingTop(5);

                        // --- 1. DADOS DO CLIENTE (EM NEGRITO) ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("1. DADOS DO CLIENTE").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(7).Text(t => { t.Span("Nome Completo: ").Bold(); t.Span(nomeCliente); });
                                row.RelativeItem(5).Text(t => { t.Span("Contacto: ").Bold(); t.Span(contactoCliente); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(nuitCliente); });
                                row.RelativeItem(6).Text(t => { t.Span("BI nº: ").Bold(); t.Span(biCliente); });
                            });
                            inner.Item().PaddingTop(4).Text(t => { t.Span("Endereço: ").Bold(); t.Span(enderecoCliente); });
                        });

                        col.Item().PaddingTop(5);

                        // --- 2. DADOS DO CRÉDITO (EM NEGRITO) ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("2. DADOS DO CRÉDITO").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("Valor Solicitado: ").Bold(); t.Span($"{valorSolicitado:N2} MT").Bold().FontColor("#079b62"); });
                                row.RelativeItem(6).Text(t => { t.Span("Prazo: ").Bold(); t.Span($"{prazoMeses} Meses"); });
                            });
                            inner.Item().PaddingTop(4).Text(t => { t.Span("Finalidade: ").Bold(); t.Span(finalidadeDestino); });
                        });

                        col.Item().PaddingTop(5);

                        // --- 3. TERMOS E CONDIÇÕES (EM NEGRITO) ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("3. TERMOS E CONDIÇÕES").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Text(
                            "DECLARAÇÃO DO REQUERENTE: Declaro que as informações acima são verdadeiras, exactas e completas, comprometendo-me a actualizar os dados em caso de alteração.\n" +
                            "Autorizo a Cash Pronto Microcrédito E.I. a consultar os centros de registo de crédito e a validar os termos de conversão, complemento e demais informações úteis para a análise do requerente."
                        ).FontSize(8).Italic().FontColor("#4a5568");

                        // --- LOCAL E DATA CENTRALIZADOS ---
                        col.Item().PaddingTop(25).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic();

                        col.Item().PaddingTop(20);

                        // --- ASSINATURAS (Cliente à esquerda, Atendimento do lado direito) ---
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Coluna da Esquerda (Cliente)
                                columns.ConstantColumn(40); // Espaço entre colunas
                                columns.RelativeColumn(); // Coluna da Direita (Atendimento / Operador)
                            });

                            // Célula do Cliente (Esquerda)
                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Assinatura do Cliente").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(nomeCliente).FontSize(8.5f).FontColor("#4a5568");
                            });

                            // Célula de Espaçamento Vazio
                            table.Cell();

                            // Célula do Atendimento / Operador (Direita)
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
            return File(bytes, "application/pdf", $"Formulario_Pedido_Credito_{pedido.Id}.pdf");
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
        public async Task<IActionResult> ProcessarDesembolso(int id, DateTime? dataDesembolsoManual, List<DesembolsoItemInput> desembolsos)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _bancoContext.Pedidos
                .Include(p => p.Entidade)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                TempData["MensagemErro"] = "Pedido não encontrado.";
                return RedirectToAction("Ver");
            }

            using var transaction = await _bancoContext.Database.BeginTransactionAsync();
            try
            {
                decimal valorTotalDesembolsado = 0;

                // Se o utilizador preencheu a data manual, usa-la; caso contrário, assume a data de hoje
                DateTimeOffset dataOperacao = dataDesembolsoManual.HasValue
                    ? new DateTimeOffset(dataDesembolsoManual.Value)
                    : DateTimeOffset.Now;

                string nomeCliente = pedido.Entidade?.Nome ?? "Cliente";

                if (desembolsos != null)
                {
                    foreach (var item in desembolsos)
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

                        // 4. Registar nos Movimentos Internos
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
                            Credito = item.Valor,
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

                // 6. Atualizar a tabela Pedidos (Estado Vigente = 3, Valor Desembolsado e a Data escolhida/atual)
                pedido.EstadoId = 3;
                pedido.ValorDesembolsado = valorTotalDesembolsado;
                pedido.DataDesembolso = dataOperacao;
                _bancoContext.Pedidos.Update(pedido);

                // 7. Gerar e Lançar o Plano de Amortização linha por linha a partir da data de desembolso
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
                DateTime dataVencimento = dataOperacao.DateTime;

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

                TempData["MensagemSucesso"] = "Desembolso concretizado com sucesso!";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["MensagemErro"] = "Erro ao processar desembolso: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return RedirectToAction("Ver", new { id = id });
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

                // 3. Atualizar o Capital Pago no Pedido e verificar se a dívida foi totalmente liquidada
                if (totalPrincipalPagoGeral > 0)
                {
                    pedido.CapitalPago += totalPrincipalPagoGeral;
                }

                bool dividaTotalLiquidada = amortizacoesDb.All(a => a.PrincipalPorPagar <= 0);
                if (dividaTotalLiquidada)
                {
                    pedido.EstadoId = 5; // Estado Liquidado
                }

                _bancoContext.Pedidos.Update(pedido);

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

        [HttpGet]
        public async Task<IActionResult> GerarFolhaGarantiasPdf(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            var cli = pedido.Entidade;
            var garantiasCliente = await _pedidoRepositorio.BuscarGarantiasClienteAsync(pedido.EntidadeId, pedido.CRR);

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string numeroProcesso = pedido.CRR.HasValue ? $"CP-{DateTime.Now:yyyyMMdd}-{pedido.CRR.Value}" : $"CP-{DateTime.Now:yyyyMMdd}-{pedido.Id}";

            string nomeCliente = cli?.Nome ?? "N/A";
            string nuitCliente = cli?.Nuit ?? "N/A";
            string biCliente = cli?.NrDocumento ?? "N/A";
            string contactoCliente = cli?.Telefone ?? cli?.Telefone ?? "N/A";
            string enderecoCliente = cli?.Endereco ?? "Maputo";

            decimal totalGarantias = garantiasCliente != null ? garantiasCliente.Sum(g => g.ValorActual) : 0;
            string totalGarantiasExtenso = ExtensoHelper.ConverterParaExtenso(totalGarantias);

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
                        text.Span("GARANTIAS DO CRÉDITO — ").FontSize(8).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(8).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // --- CABEÇALHO PADRÃO ---
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(22).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(9).FontColor("#718096");
                            });

                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic().AlignRight();
                                c.Item().Text($"Nº Processo: {numeroProcesso}").Bold().FontSize(9).FontColor("#079b62").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#079b62");

                        // Título em Bloco Verde
                        col.Item().Background("#079b62").Padding(8).AlignCenter().Text("FOLHA DE RELAÇÃO E CONSTITUIÇÃO DE GARANTIAS")
                            .Bold().FontSize(12).FontColor("#FFFFFF");

                        col.Item().PaddingTop(5);

                        // --- 1. DADOS DO CLIENTE (EM NEGRITO) ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("1. DADOS DO CLIENTE").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(7).Text(t => { t.Span("Nome Completo: ").Bold(); t.Span(nomeCliente); });
                                row.RelativeItem(5).Text(t => { t.Span("Contacto: ").Bold(); t.Span(contactoCliente); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(nuitCliente); });
                                row.RelativeItem(6).Text(t => { t.Span("BI nº: ").Bold(); t.Span(biCliente); });
                            });
                            inner.Item().PaddingTop(4).Text(t => { t.Span("Endereço: ").Bold(); t.Span(enderecoCliente); });
                        });

                        col.Item().PaddingTop(5);

                        // --- 2. RELAÇÃO DE BENS EM GARANTIA (EM NEGRITO) ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("2. RELAÇÃO DE BENS EM GARANTIA").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Text(text =>
                        {
                            text.Justify();
                            text.Span("Para assegurar o reembolso pontual do capital, juros e demais encargos do crédito concedido, o mutuário entrega e vincula a favor da ");
                            text.Span(nomeEmpresa).Bold();
                            text.Span($", sob a forma de garantia, os seguintes bens avaliados no valor global de ");
                            text.Span($"{totalGarantias:N2} MT").Bold().FontColor("#079b62");
                            text.Span($" ({totalGarantiasExtenso}):");
                        });

                        col.Item().PaddingTop(5);

                        // Tabela de Garantias (Com colunas: Tipo, Descrição, Valor Atual - Sem Valor de Aquisição)
                        if (garantiasCliente != null && garantiasCliente.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(25); // Tipo
                                    columns.RelativeColumn(55); // Descrição
                                    columns.RelativeColumn(20); // Valor Atual
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#079b62").Padding(6).Text("TIPO").Bold().FontSize(9).FontColor("#FFFFFF");
                                    header.Cell().Background("#079b62").Padding(6).Text("DESCRIÇÃO").Bold().FontSize(9).FontColor("#FFFFFF");
                                    header.Cell().Background("#079b62").Padding(6).AlignRight().Text("VALOR ATUAL (MT)").Bold().FontSize(9).FontColor("#FFFFFF");
                                });

                                foreach (var g in garantiasCliente)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(6).Text(g.Tipo).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(6).Text(g.Descricao).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(6).AlignRight().Text($"{g.ValorActual:N2} MT").FontSize(9).Bold().FontColor("#079b62");
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(5).AlignCenter().Text("Nenhuma garantia registada para este pedido.").FontSize(9).Italic().FontColor("#718096");
                        }

                        col.Item().PaddingTop(10);

                        // --- 3. DECLARAÇÃO DE RESPONSABILIDADE (EM NEGRITO) ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("3. DECLARAÇÃO DE RESPONSABILIDADE").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Text(text =>
                        {
                            text.Justify();
                            text.Span("Declaro por minha honra que os bens acima descritos são inteiramente minha propriedade, encontram-se livres de quaisquer ónus ou encargos, e autorizo a ");
                            text.Span(nomeEmpresa);
                            text.Span(" a proceder à sua retenção ou execução em caso de incumprimento das obrigações decorrentes do crédito.");
                        });

                        // --- LOCAL E DATA CENTRALIZADOS ---
                        col.Item().PaddingTop(25).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic();

                        col.Item().PaddingTop(30);

                        // --- ASSINATURA APENAS DO MUTUÁRIO (Centralizada) ---
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(30); // Espaço à esquerda para centrar
                            row.RelativeItem(40).Column(c =>
                            {
                                c.Item().PaddingBottom(15).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().AlignCenter().Text("Assinatura do Mutuário").FontSize(9).Bold();
                                c.Item().PaddingTop(2).AlignCenter().Text(nomeCliente).FontSize(8.5f).FontColor("#4a5568");
                            });
                            row.RelativeItem(30); // Espaço à direita para centrar
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Folha_Garantias_Pedido_{pedido.Id}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> GerarFolhaGarantiasIntervenientePdf(int intervenienteId, int pedidoId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(pedidoId);
            if (pedido == null) return NotFound();

            var intervenientes = await _pedidoRepositorio.BuscarIntervenientesPorPedidoAsync(pedido.EntidadeId, pedido.CRR);
            var inter = intervenientes.FirstOrDefault(i => i.IntervenienteID == intervenienteId || i.Id == intervenienteId);
            if (inter == null) return NotFound();

            var entidadeInterveniente = inter.IntervenienteEntidade;
            var garantiasInterveniente = await _pedidoRepositorio.BuscarGarantiasIntervenienteAsync(inter.IntervenienteID, pedido.CRR);

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string numeroProcesso = pedido.CRR.HasValue ? $"CP-{DateTime.Now:yyyyMMdd}-{pedido.CRR.Value}" : $"CP-{DateTime.Now:yyyyMMdd}-{pedido.Id}";
            var cli = pedido.Entidade;
            string nomeCliente = cli?.Nome ?? "Mutuário Principal"; // <--- ADICIONADO AQUI

            string nomeInterveniente = entidadeInterveniente?.Nome ?? "N/A";
            string nuitInterveniente = entidadeInterveniente?.Nuit ?? "N/A";
            string biInterveniente = entidadeInterveniente?.NrDocumento ?? "N/A";
            string contactoInterveniente = entidadeInterveniente?.Telefone ?? entidadeInterveniente?.Telefone ?? "N/A";
            string enderecoInterveniente = entidadeInterveniente?.Endereco ?? "Maputo";
            string tipoInterveniente = inter.Tipo ?? "Garant";

            decimal totalGarantias = garantiasInterveniente != null ? garantiasInterveniente.Sum(g => g.ValorActual) : 0;
            string totalGarantiasExtenso = ExtensoHelper.ConverterParaExtenso(totalGarantias);

            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(10).FontColor("#172b4d"));

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"TERMO DE CONSTITUIÇÃO DE GARANTIAS ({tipoInterveniente.ToUpper()}) — ").FontSize(8).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(8).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // Cabeçalho
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(22).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(9).FontColor("#718096");
                            });

                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic().AlignRight();
                                c.Item().Text($"Nº Processo: {numeroProcesso}").Bold().FontSize(9).FontColor("#079b62").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#079b62");

                        col.Item().Background("#079b62").Padding(8).AlignCenter().Text($"FOLHA DE GARANTIAS - {tipoInterveniente.ToUpper()}")
                            .Bold().FontSize(12).FontColor("#FFFFFF");

                        col.Item().PaddingTop(5);

                        // Dados do Interveniente
                        col.Item().Background("#f5f7fb").Padding(5).Text($"1. DADOS DO {tipoInterveniente.ToUpper()}").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(7).Text(t => { t.Span("Nome Completo: ").Bold(); t.Span(nomeInterveniente); });
                                row.RelativeItem(5).Text(t => { t.Span("Contacto: ").Bold(); t.Span(contactoInterveniente); });
                            });
                            inner.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(nuitInterveniente); });
                                row.RelativeItem(6).Text(t => { t.Span("BI nº: ").Bold(); t.Span(biInterveniente); });
                            });
                            inner.Item().PaddingTop(4).Text(t => { t.Span("Endereço: ").Bold(); t.Span(enderecoInterveniente); });
                        });

                        col.Item().PaddingTop(5);

                        // Relação de Bens
                        col.Item().Background("#f5f7fb").Padding(5).Text("2. RELAÇÃO DE BENS EM GARANTIA").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Text(text =>
                        {
                            text.Justify();
                            text.Span($"Para assegurar o cumprimento das obrigações do crédito associado a este processo em nome de ");
                            text.Span(nomeCliente).Bold();
                            text.Span($", o(a) {tipoInterveniente.ToLower()} entrega e vincula a favor da ");
                            text.Span(nomeEmpresa).Bold();
                            text.Span($", sob a forma de garantia, os seguintes bens avaliados no valor global de ");
                            text.Span($"{totalGarantias:N2} MT").Bold().FontColor("#079b62");
                            text.Span($" ({totalGarantiasExtenso}):");
                        });

                        col.Item().PaddingTop(5);

                        if (garantiasInterveniente != null && garantiasInterveniente.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(25);
                                    columns.RelativeColumn(55);
                                    columns.RelativeColumn(20);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#079b62").Padding(6).Text("TIPO").Bold().FontSize(9).FontColor("#FFFFFF");
                                    header.Cell().Background("#079b62").Padding(6).Text("DESCRIÇÃO").Bold().FontSize(9).FontColor("#FFFFFF");
                                    header.Cell().Background("#079b62").Padding(6).AlignRight().Text("VALOR ATUAL (MT)").Bold().FontSize(9).FontColor("#FFFFFF");
                                });

                                foreach (var g in garantiasInterveniente)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(6).Text(g.Tipo).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(6).Text(g.Descricao).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor("#E0E0E0").Padding(6).AlignRight().Text($"{g.ValorActual:N2} MT").FontSize(9).Bold().FontColor("#079b62");
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(5).AlignCenter().Text("Nenhuma garantia registada para este interveniente.").FontSize(9).Italic().FontColor("#718096");
                        }

                        col.Item().PaddingTop(10);

                        // Declaração
                        col.Item().Background("#f5f7fb").Padding(5).Text("3. DECLARAÇÃO DE RESPONSABILIDADE").Bold().FontSize(10).FontColor("#172b4d");

                        col.Item().PaddingTop(3).Text(text =>
                        {
                            text.Justify();
                            text.Span("Declaro por minha honra que os bens acima descritos são inteiramente minha propriedade, encontram-se livres de quaisquer ónus ou encargos, e autorizo a ");
                            text.Span(nomeEmpresa);
                            text.Span($" a proceder à sua retenção ou execução nos termos legais aplicáveis na qualidade de {tipoInterveniente.ToLower()}.");
                        });

                        col.Item().PaddingTop(25).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic();

                        col.Item().PaddingTop(30);

                        // Assinatura individual do Interveniente
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(30);
                            row.RelativeItem(40).Column(c =>
                            {
                                c.Item().PaddingBottom(15).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().AlignCenter().Text($"Assinatura do {tipoInterveniente}").FontSize(9).Bold();
                                c.Item().PaddingTop(2).AlignCenter().Text(nomeInterveniente).FontSize(8.5f).FontColor("#4a5568");
                            });
                            row.RelativeItem(30);
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Folha_Garantias_{tipoInterveniente}_{inter.IntervenienteID}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> GerarReciboPagamentoPdf(int pedidoId, int? movimentoId, decimal? valorPago, int? contaId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(pedidoId);
            if (pedido == null) return NotFound();

            var cli = pedido.Entidade;

            decimal valorRealPago = 0;
            int? contaRealId = contaId;

            if (movimentoId.HasValue && movimentoId.Value > 0)
            {
                var movimento = await _bancoContext.MovimentosExternos
                    .FirstOrDefaultAsync(m => m.Id == movimentoId.Value);

                if (movimento != null)
                {
                    valorRealPago = movimento.Debito > 0 ? movimento.Debito : movimento.Credito;
                    contaRealId = movimento.ContaId;
                }
            }
            else if (valorPago.HasValue && valorPago.Value > 0)
            {
                valorRealPago = valorPago.Value;
            }
            else
            {
                var ultimoMovimento = await _bancoContext.MovimentosExternos
                    .Where(m => m.EntidadeId == pedido.EntidadeId && m.TipoOperacao.Contains("Reembolso"))
                    .OrderByDescending(m => m.Id)
                    .FirstOrDefaultAsync();

                if (ultimoMovimento != null)
                {
                    valorRealPago = ultimoMovimento.Debito > 0 ? ultimoMovimento.Debito : ultimoMovimento.Credito;
                    contaRealId = ultimoMovimento.ContaId;
                }
                else
                {
                    valorRealPago = 0;
                }
            }

            string nomeContaStr = "Conta Principal";
            string formaPagamento = "Depósito/Transferência";

            if (contaRealId.HasValue && contaRealId.Value > 0)
            {
                var contaModel = await _bancoContext.Contas.FindAsync(contaRealId.Value);
                if (contaModel != null)
                {
                    nomeContaStr = $"{contaModel.Banco} - {contaModel.Conta} ({contaModel.Titular})";

                    if (!string.IsNullOrEmpty(contaModel.Banco) && contaModel.Banco.IndexOf("caixa", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        formaPagamento = "Numerário";
                    }
                    else if (!string.IsNullOrEmpty(contaModel.Conta) && contaModel.Conta.IndexOf("caixa", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        formaPagamento = "Numerário";
                    }
                }
            }

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string numeroProcesso = pedido.CRR.HasValue ? $"CRR-{pedido.CRR.Value:0}" : $"CP-{pedido.Id}";
            string numeroRecibo = $"REC-{DateTime.Now:yyyyMMdd}-{pedido.Id}-{new Random().Next(100, 999)}";

            string nomeCliente = cli?.Nome ?? "N/A";
            string nuitCliente = cli?.Nuit ?? "N/A";
            string biCliente = cli?.NrDocumento ?? "N/A";
            string contactoCliente = cli?.Telefone ?? "N/A";
            string operadorStr = usuarioLogado?.Login ?? "Administrador";

            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            var pdfDocument = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f).FontColor("#172b4d"));

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("DOCUMENTO PROCESSADO POR COMPUTADOR — ").FontSize(7).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(7).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // --- FUNÇÃO AUXILIAR PARA GERAR CADA CÓPIA DO RECIBO ---
                        void RenderizarViaRecibo(string viaTitulo)
                        {
                            col.Item().Column(reciboCol =>
                            {
                                reciboCol.Spacing(4);

                                // Cabeçalho com a Indicação da Via (Original / Duplicado)
                                reciboCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Cash Pronto").Bold().FontSize(16).FontColor("#079b62");
                                        c.Item().Text($"{nomeEmpresa} — {viaTitulo}").FontSize(7.5f).FontColor("#718096");
                                    });

                                    row.ConstantItem(160).AlignRight().Column(c =>
                                    {
                                        c.Item().Text($"Data: {DateTime.Now.ToString("dd/MM/yyyy HH:mm", culturaPt)}").FontSize(7.5f).Italic().AlignRight();
                                        c.Item().Text($"Nº Recibo: {numeroRecibo}").Bold().FontSize(8).FontColor("#079b62").AlignRight();
                                        c.Item().Text($"Ref: {numeroProcesso}").FontSize(8).FontColor("#718096").AlignRight();
                                    });
                                });

                                reciboCol.Item().PaddingVertical(2).LineHorizontal(0.8f).LineColor("#079b62");

                                reciboCol.Item().Background("#079b62").Padding(4).AlignCenter().Text("RECIBO DE PAGAMENTO")
                                    .Bold().FontSize(10).FontColor("#FFFFFF");

                                // 1. Dados do Mutuário
                                reciboCol.Item().Background("#f5f7fb").Padding(3).Text("1. DADOS DO MUTUÁRIO").Bold().FontSize(8.5f).FontColor("#172b4d");
                                reciboCol.Item().Column(inner =>
                                {
                                    inner.Item().Row(row =>
                                    {
                                        row.RelativeItem(7).Text(t => { t.Span("Nome: ").Bold(); t.Span(nomeCliente); });
                                        row.RelativeItem(5).Text(t => { t.Span("Contacto: ").Bold(); t.Span(contactoCliente); });
                                    });
                                    inner.Item().Row(row =>
                                    {
                                        row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(nuitCliente); });
                                        row.RelativeItem(6).Text(t => { t.Span("BI: ").Bold(); t.Span(biCliente); });
                                    });
                                });

                                // 2. Detalhes do Pagamento
                                reciboCol.Item().Background("#f5f7fb").Padding(3).Text("2. DETALHES DO PAGAMENTO").Bold().FontSize(8.5f).FontColor("#172b4d");
                                reciboCol.Item().Column(inner =>
                                {
                                    inner.Item().Row(row =>
                                    {
                                        row.RelativeItem(6).Text(t => { t.Span("Valor Recebido: ").Bold(); t.Span($"{valorRealPago:N2} MT").Bold().FontColor("#079b62"); });
                                        row.RelativeItem(6).Text(t => { t.Span("Conta Destino: ").Bold(); t.Span(nomeContaStr); });
                                    });
                                    inner.Item().Row(row =>
                                    {
                                        row.RelativeItem(6).Text(t => { t.Span("Operador: ").Bold(); t.Span(operadorStr); });
                                        row.RelativeItem(6).Text(t => { t.Span("Forma Pagamento: ").Bold(); t.Span(formaPagamento); });
                                    });
                                });

                                // 3. Observações Curtas
                                reciboCol.Item().Background("#f5f7fb").Padding(3).Text("3. OBSERVAÇÕES").Bold().FontSize(8.5f).FontColor("#172b4d");
                                reciboCol.Item().Text(
                                    "Quantia recebida para amortização de prestações e encargos do crédito, nos termos regulamentares."
                                ).FontSize(7.5f).Italic().FontColor("#4a5568");

                                reciboCol.Item().PaddingTop(10);

                                // Assinaturas
                                reciboCol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(30);
                                        columns.RelativeColumn();
                                    });

                                    table.Cell().Column(c =>
                                    {
                                        c.Item().PaddingBottom(3).LineHorizontal(0.5f).LineColor("#718096");
                                        c.Item().Text("Assinatura do Cliente").FontSize(8).Bold();
                                    });

                                    table.Cell();

                                    table.Cell().Column(c =>
                                    {
                                        c.Item().PaddingBottom(3).LineHorizontal(0.5f).LineColor("#718096");
                                        c.Item().Text("Pelo Operador / Tesouraria").FontSize(8).Bold();
                                    });
                                });
                            });
                        }

                        // --- RENDERIZAR CÓPIA 1 (ORIGINAL) ---
                        RenderizarViaRecibo("1ª VIA — ORIGINAL");

                        // --- LINHA DIVISÓRIA / CORTE ENTRE AS CÓPIAS ---
                        col.Item().PaddingVertical(10).Row(r =>
                        {
                            r.RelativeItem().LineHorizontal(0.8f).LineColor("#cbd5e0");
                        });

                        // --- RENDERIZAR CÓPIA 2 (DUPLICADO) ---
                        RenderizarViaRecibo("2ª VIA — DUPLICADO");
                    });
                });
            });

            byte[] bytes = pdfDocument.GeneratePdf();
            return File(bytes, "application/pdf", $"Recibo_Duplicado_{pedido.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> GerarReciboDesembolsoPdf(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            var cli = pedido.Entidade;

            // --- BUSCAR VALOR E CONTAS REAIS DO DESEMBOLSO DIRETAMENTE DA BASE DE DADOS ---
            decimal valorRealDesembolsado = pedido.ValorDesembolsado > 0 ? pedido.ValorDesembolsado : (pedido.ValorDecisao ?? pedido.Valor);

            // Tentar obter a descrição da(s) conta(s) associada(s) ao desembolso (caso tenha uma tabela de registo de transações ou histórico de contas)
            string informacaoContas = "Conta Principal";
            string formaPagamento = "Depósito/Transferência";

            // Exemplo: se tiver uma tabela ou relação de movimentos/desembolsos por conta, pode consultar aqui. 
            // Caso contrário, garantimos que o valor reflete fielmente o que foi gravado no pedido.

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string numeroProcesso = pedido.CRR.HasValue ? $"CRR-{pedido.CRR.Value:0}" : $"CP-{pedido.Id}";
            string numeroRecibo = $"DES-{DateTime.Now:yyyyMMdd}-{pedido.Id}";

            string nomeCliente = cli?.Nome ?? "N/A";
            string nuitCliente = cli?.Nuit ?? "N/A";
            string biCliente = cli?.NrDocumento ?? "N/A";
            string contactoCliente = cli?.Telefone ?? "N/A";
            string operadorStr = usuarioLogado?.Login ?? "Administrador";

            decimal taxaDecisao = pedido.TaxaDecisao ?? pedido.Taxa;
            int prazoDecisao = pedido.PrazoDecisao ?? pedido.Prazo;
            decimal prestacaoDecisao = pedido.PrestacaoDecisao ?? pedido.Prestacao;

            // Configurar cultura para português para garantir o mês correto
            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9.5f).FontColor("#172b4d"));

                    // Rodapé padrão
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("DOCUMENTO DE DESEMBOLSO DE CRÉDITO — ").FontSize(8).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(8).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // --- CABEÇALHO ---
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(22).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(9).FontColor("#718096");
                            });

                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", culturaPt)}").FontSize(8.5f).Italic().AlignRight();
                                c.Item().Text($"Nº Recibo: {numeroRecibo}").Bold().FontSize(9).FontColor("#079b62").AlignRight();
                                c.Item().Text($"Ref. Crédito: {numeroProcesso}").FontSize(9).FontColor("#718096").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#079b62");

                        // Título em Bloco Verde
                        col.Item().Background("#079b62").Padding(8).AlignCenter().Text("COMPROVATIVO DE DESEMBOLSO DE CRÉDITO")
                            .Bold().FontSize(12).FontColor("#FFFFFF");

                        col.Item().PaddingTop(4);

                        // --- 1. DADOS DO MUTUÁRIO ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("1. DADOS DO MUTUÁRIO").Bold().FontSize(9.5f).FontColor("#172b4d");

                        col.Item().PaddingTop(2).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(7).Text(t => { t.Span("Nome Completo: ").Bold(); t.Span(nomeCliente); });
                                row.RelativeItem(5).Text(t => { t.Span("Contacto: ").Bold(); t.Span(contactoCliente); });
                            });
                            inner.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("NUIT: ").Bold(); t.Span(nuitCliente); });
                                row.RelativeItem(6).Text(t => { t.Span("BI nº: ").Bold(); t.Span(biCliente); });
                            });
                        });

                        col.Item().PaddingTop(4);

                        // --- 2. CONDIÇÕES DO CRÉDITO E DESEMBOLSO ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("2. DETALHES DO DESEMBOLSO E CONDIÇÕES").Bold().FontSize(9.5f).FontColor("#172b4d");

                        col.Item().PaddingTop(2).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("Valor Desembolsado: ").Bold(); t.Span($"{valorRealDesembolsado:N2} MT").Bold().FontColor("#079b62"); });
                                row.RelativeItem(6).Text(t => { t.Span("Taxa de Juro: ").Bold(); t.Span($"{taxaDecisao:N2}% ao mês"); });
                            });
                            inner.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("Prazo: ").Bold(); t.Span($"{prazoDecisao} Meses"); });
                                row.RelativeItem(6).Text(t => { t.Span("Prestação Mensal: ").Bold(); t.Span($"{prestacaoDecisao:N2} MT"); });
                            });
                            inner.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem(6).Text(t => { t.Span("Emitido por: ").Bold(); t.Span(operadorStr); });
                                row.RelativeItem(6).Text(t => { t.Span("Forma / Origem: ").Bold(); t.Span("Tesouraria / Conta Bancária"); });
                            });
                        });

                        col.Item().PaddingTop(4);

                        // --- 3. DECLARAÇÃO ---
                        col.Item().Background("#f5f7fb").Padding(5).Text("3. DECLARAÇÃO DE RECEPÇÃO").Bold().FontSize(9.5f).FontColor("#172b4d");

                        col.Item().PaddingTop(2).Text(
                            "Declaro que recebi da Cash Pronto Microcrédito E.I. o valor total desembolsado especificado neste documento, " +
                            "comprometendo-me a cumprir rigorosamente o plano de amortização e as condições contratuais estabelecidas."
                        ).FontSize(8.5f).Italic().FontColor("#4a5568");

                        // --- LOCAL E DATA ---
                        col.Item().PaddingTop(15).AlignCenter().Text($"Maputo, aos {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", culturaPt)}").FontSize(9).Italic();

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
                                c.Item().Text("Assinatura do Mutuário").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(nomeCliente).FontSize(8.5f).FontColor("#4a5568");
                            });

                            table.Cell();

                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(5).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Pelo Operador / Tesouraria").FontSize(9).Bold();
                                c.Item().PaddingTop(2).Text(nomeEmpresa).FontSize(8.5f).FontColor("#4a5568");
                            });
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Recibo_Desembolso_{pedido.Id}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> GerarPlanoAmortizacaoPdf(int id)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var pedido = await _pedidoRepositorio.BuscarPorIdAsync(id);
            if (pedido == null) return NotFound();

            var cli = pedido.Entidade;

            // 1. Tentar carregar o Plano de Amortização já gravado na tabela Amortizacoes
            var listaAmortizacoes = await _bancoContext.Amortizacoes
                .Where(a => a.PedidoId == id)
                .OrderBy(a => a.NR)
                .ToListAsync();

            // 2. SE NÃO EXISTIR PLANO GRAVADO, GERAMOS NA HORA USANDO A MESMA REGRA DA VIEW!
            if (listaAmortizacoes == null || !listaAmortizacoes.Any())
            {
                DateTime dataReferencia;
                if (pedido.EstadoId == 4 || pedido.EstadoId == 5)
                {
                    dataReferencia = pedido.DataDesembolso.HasValue ? pedido.DataDesembolso.Value.DateTime : DateTime.Today;
                }
                else
                {
                    dataReferencia = DateTime.Today;
                }

                listaAmortizacoes = GerarPlanoAmortizacaoTemporario(pedido, dataReferencia);
            }

            string nomeEmpresa = "Cash Pronto Microcrédito E.I.";
            string numeroProcesso = pedido.CRR.HasValue ? $"-{pedido.CRR.Value:0}" : $"CP-{pedido.Id}";
            string numeroRecibo = $"PLN-{DateTime.Now:yyyyMMdd}-{pedido.Id}";

            string nomeCliente = cli?.Nome ?? "N/A";
            string nuitCliente = cli?.Nuit ?? "N/A";
            string contactoCliente = cli?.Telefone ?? "N/A";

            decimal valorEmprestimo = pedido.ValorDesembolsado > 0 ? pedido.ValorDesembolsado : (pedido.ValorDecisao ?? pedido.Valor);
            int prazoMeses = pedido.PrazoDecisao ?? pedido.Prazo;
            decimal taxaJuro = pedido.TaxaDecisao ?? pedido.Taxa;
            decimal prestacaoValor = pedido.PrestacaoDecisao ?? pedido.Prestacao;

            // Configurar cultura para português para garantir o mês correto
            var culturaPt = new System.Globalization.CultureInfo("pt-PT");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // CONFIGURAÇÃO DE PÁGINA HORIZONTAL (LANDSCAPE) PARA CABER TODAS AS COLUNAS
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9).FontColor("#172b4d"));

                    // Rodapé padrão
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("PLANO DE AMORTIZAÇÃO DE CRÉDITO (DOCUMENTO OFICIAL) — ").FontSize(7.5f).Italic().FontColor("#a0aec0");
                        text.Span(nomeEmpresa).FontSize(7.5f).Bold().FontColor("#718096");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(8);

                        // --- CABEÇALHO ---
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Cash Pronto").Bold().FontSize(20).FontColor("#079b62");
                                c.Item().Text(nomeEmpresa).FontSize(8.5f).FontColor("#718096");
                            });

                            row.ConstantItem(220).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Data: {DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy, HH:mm", culturaPt)}").FontSize(8f).Italic().AlignRight();
                                c.Item().Text($"Ref: {numeroRecibo}").Bold().FontSize(8.5f).FontColor("#079b62").AlignRight();
                                c.Item().Text($"Código CRR: {numeroProcesso}").FontSize(8.5f).FontColor("#718096").AlignRight();
                            });
                        });

                        col.Item().PaddingVertical(3).LineHorizontal(1).LineColor("#079b62");

                        // Título em Bloco Verde
                        col.Item().Background("#079b62").Padding(6).AlignCenter().Text("PLANO DE AMORTIZAÇÃO DE CRÉDITO")
                            .Bold().FontSize(11).FontColor("#FFFFFF");

                        col.Item().PaddingTop(3);

                        // --- 1. RESUMO DO MUTUÁRIO E CONDIÇÕES ---
                        col.Item().Background("#f5f7fb").Padding(6).Column(inner =>
                        {
                            inner.Item().Row(row =>
                            {
                                row.RelativeItem(5).Text(t => { t.Span("Mutuário: ").Bold(); t.Span(nomeCliente); });
                                row.RelativeItem(3).Text(t => { t.Span("NUIT: ").Bold(); t.Span(nuitCliente); });
                                row.RelativeItem(4).Text(t => { t.Span("Contacto: ").Bold(); t.Span(contactoCliente); });
                            });
                            inner.Item().PaddingTop(3).Row(row =>
                            {
                                row.RelativeItem(3).Text(t => { t.Span("Valor Empréstimo: ").Bold(); t.Span($"{valorEmprestimo:N2} MT").FontColor("#079b62"); });
                                row.RelativeItem(3).Text(t => { t.Span("Prazo: ").Bold(); t.Span($"{prazoMeses} Meses"); });
                                row.RelativeItem(3).Text(t => { t.Span("Taxa de Juro: ").Bold(); t.Span($"{taxaJuro:N2}%"); });
                                row.RelativeItem(3).Text(t => { t.Span("Prestação: ").Bold(); t.Span($"{prestacaoValor:N2} MT"); });
                            });
                        });

                        col.Item().PaddingTop(5);

                        // --- 2. TABELA DO PLANO DE AMORTIZAÇÃO (HORIZONTAL) ---
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);   // Nr
                                columns.RelativeColumn(2);  // Vencimento
                                columns.RelativeColumn(2);  // Prestação
                                columns.RelativeColumn(2);  // Juro
                                columns.RelativeColumn(2);  // Amortização
                                columns.RelativeColumn(2.5f); // Capital em Dívida
                                columns.RelativeColumn(2);  // Valor Pago
                                columns.RelativeColumn(2);  // Valor a Pagar
                            });

                            // Cabeçalho da Tabela
                            table.Header(header =>
                            {
                                header.Cell().Background("#edf2f7").Padding(4).Text("Nr").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).Text("Vencimento").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Prestação (mt)").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Juro (mt)").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Amortização (mt)").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Capital em Dívida").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor Pago").Bold().FontSize(8.5f);
                                header.Cell().Background("#edf2f7").Padding(4).AlignRight().Text("Valor a Pagar").Bold().FontSize(8.5f);
                            });

                            if (listaAmortizacoes != null && listaAmortizacoes.Any())
                            {
                                foreach (var item in listaAmortizacoes)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(item.NR.ToString()).FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(item.Vencimento.ToString("dd/MM/yyyy")).FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{item.Prestacao:N2}").Bold().FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{item.Juro:N2}").FontColor("#c53030").FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{item.Principal:N2}").FontColor("#079b62").FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{item.PrincipalPorPagar:N2}").FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{item.TotalPago:N2}").FontColor("#2b6cb0").FontSize(8.5f);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{item.TotalPorPagar:N2}").Bold().FontColor("#dd6b20").FontSize(8.5f);
                                }
                            }
                        });

                        // --- LOCAL E ASSINATURAS ---
                        col.Item().PaddingTop(15);

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
                                c.Item().Text("Assinatura do Mutuário").FontSize(8.5f).Bold();
                                c.Item().Text(nomeCliente).FontSize(8f).FontColor("#4a5568");
                            });

                            table.Cell();

                            table.Cell().Column(c =>
                            {
                                c.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor("#718096");
                                c.Item().Text("Pelo Operador / Instituição").FontSize(8.5f).Bold();
                                c.Item().Text(nomeEmpresa).FontSize(8f).FontColor("#4a5568");
                            });
                        });
                    });
                });
            });

            byte[] bytes = document.GeneratePdf();
            return File(bytes, "application/pdf", $"Plano_Amortizacao_{pedido.Id}.pdf");
        }
        [HttpPost]
        public async Task<IActionResult> RemoverGarantia(int id, int pedidoId)
        {
            var garantia = await _bancoContext.Garantias.FindAsync(id);
            if (garantia != null)
            {
                _bancoContext.Garantias.Remove(garantia);
                await _bancoContext.SaveChangesAsync();
                TempData["MensagemSucesso"] = "Garantia removida com sucesso!";
            }
            else
            {
                TempData["MensagemErro"] = "Garantia não encontrada.";
            }

            return RedirectToAction("Garantias", new { id = pedidoId }); // Ajuste o nome da action se necessário
        }

        [HttpPost]
        public async Task<IActionResult> EditarGarantia(GarantiaModel model, int pedidoId)
        {
            var garantiaDb = await _bancoContext.Garantias.FindAsync(model.Id);
            if (garantiaDb != null)
            {
                garantiaDb.Tipo = model.Tipo;
                garantiaDb.ValorAquisicao = model.ValorAquisicao;
                garantiaDb.ValorActual = model.ValorActual;
                garantiaDb.Descricao = model.Descricao;

                _bancoContext.Garantias.Update(garantiaDb);
                await _bancoContext.SaveChangesAsync();
                TempData["MensagemSucesso"] = "Garantia atualizada com sucesso!";
            }
            else
            {
                TempData["MensagemErro"] = "Erro ao atualizar garantia.";
            }

            return RedirectToAction("Garantias", new { id = pedidoId }); // Ajuste o nome da action se necessário
        }
    }
}
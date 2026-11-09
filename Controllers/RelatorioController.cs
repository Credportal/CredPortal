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

        public RelatorioController(
            IPedidoRepositorio pedidoRepositorio,
            IEntidadeRepositorio entidadeRepositorio,
            BancoContext bancoContext,
            ISessao sessao)
        {
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

      
    }
}
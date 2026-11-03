using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    public class RelatorioController : Controller
    {
        private readonly IPedidoRepositorio _pedidoRepositorio;
        private readonly IAgenciaRepositorio _agenciaRepositorio; // Injetado para popular as agências
        private readonly ISessao _sessao;

        public RelatorioController(
            IPedidoRepositorio pedidoRepositorio,
            IAgenciaRepositorio agenciaRepositorio,
            ISessao sessao)
        {
            _pedidoRepositorio = pedidoRepositorio;
            _agenciaRepositorio = agenciaRepositorio;
            _sessao = sessao;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string tipoRelatorio = "Pendentes",
            DateTime? dataInicial = null,
            DateTime? dataFinal = null,
            int? gestorId = null,
            string agencia = null,
            int? entidadeId = null,
            int? contaId = null,
            int? clienteId = null)
        {
            // Validação de Sessão ativa
            if (_sessao.BuscarSessaoDoUsuario() == null)
            {
                return RedirectToAction("Index", "Login");
            }

            try
            {
                // Buscar todos os pedidos com as relações necessárias (Entidade, Gestor, etc.)
                var pedidos = await _pedidoRepositorio.BuscarTodosAsync() ?? new List<PedidoModel>();

                // --- 1. PREENCHER LISTAS PARA OS DROPDOWNS DE FILTROS (Com base em todos os dados originais) ---
                ViewBag.ListaGestores = pedidos.Where(p => p.Gestor != null).Select(p => p.Gestor).Distinct().ToList();
                ViewBag.ListaEntidades = pedidos.Where(p => p.Entidade != null).Select(p => p.Entidade).Distinct().ToList();
                ViewBag.ListaAgencias = _agenciaRepositorio.BuscarTodos() ?? new List<AgenciaModel>(); // Carregado diretamente do repositório de agências

                // --- 2. APLICAR FILTROS GLOBAIS ---
                if (gestorId.HasValue && gestorId.Value > 0)
                {
                    pedidos = pedidos.Where(p => p.GestorId == gestorId.Value).ToList();
                }

                if (!string.IsNullOrEmpty(agencia) && agencia != "Todas")
                {
                    if (int.TryParse(agencia, out int agenciaIdInt))
                    {
                        pedidos = pedidos.Where(p => p.Entidade?.AgenciaId == agenciaIdInt).ToList();
                    }
                }

                if (entidadeId.HasValue && entidadeId.Value > 0)
                {
                    pedidos = pedidos.Where(p => p.EntidadeId == entidadeId.Value).ToList();
                }

                if (dataInicial.HasValue)
                {
                    pedidos = pedidos.Where(p => p.DataCadastro >= dataInicial.Value).ToList();
                }

                if (dataFinal.HasValue)
                {
                    pedidos = pedidos.Where(p => p.DataCadastro <= dataFinal.Value.AddDays(1)).ToList();
                }

                // --- 3. SEPARAR POR CATEGORIAS DE RELATÓRIO DE CARTEIRA ---
                // EstadoId: 1 = Pendente, 2 = Aprovado, 3 = Rejeitado, 4 = Vigente/Desembolsado, 5 = Liquidado, 6 = Mora
                ViewBag.RelatorioTipo = tipoRelatorio;

                ViewBag.Pendentes = pedidos.Where(p => p.EstadoId == 1).ToList();
                ViewBag.Aprovados = pedidos.Where(p => p.EstadoId == 2).ToList();
                ViewBag.Rejeitados = pedidos.Where(p => p.EstadoId == 3).ToList();
                ViewBag.Vigentes = pedidos.Where(p => p.EstadoId == 4).ToList();
                ViewBag.Liquidados = pedidos.Where(p => p.EstadoId == 5).ToList();
                ViewBag.Mora = pedidos.Where(p => p.EstadoId == 6).ToList();

                // Clientes (Entidades) únicos filtrados
                ViewBag.Clientes = pedidos.Select(p => p.Entidade).Where(e => e != null).Distinct().ToList();

                // Dados de Pagamentos e Movimentos (prontos para ligação aos respetivos repositórios futuramente)
                ViewBag.PagamentosPrevistos = new List<object>();
                ViewBag.PagamentosFeitos = new List<object>();
                ViewBag.PagamentosPorFazer = new List<object>();
                ViewBag.MovimentosInternos = new List<object>();
                ViewBag.MovimentosExternos = new List<object>();
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao gerar relatórios: {ex.Message}";

                // Garantir listas vazias em caso de erro para evitar NullReferenceException na View
                ViewBag.Pendentes = new List<PedidoModel>();
                ViewBag.Aprovados = new List<PedidoModel>();
                ViewBag.Rejeitados = new List<PedidoModel>();
                ViewBag.Vigentes = new List<PedidoModel>();
                ViewBag.Liquidados = new List<PedidoModel>();
                ViewBag.Mora = new List<PedidoModel>();
                ViewBag.Clientes = new List<EntidadeModel>();
                ViewBag.ListaGestores = new List<UsuarioModel>();
                ViewBag.ListaEntidades = new List<EntidadeModel>();
                ViewBag.ListaAgencias = new List<AgenciaModel>();
            }

            return View();
        }
    }
}
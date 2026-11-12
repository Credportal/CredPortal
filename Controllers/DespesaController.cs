using Analise.Enuns;
using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    [PermissaoPerfil(PerfilEnum.Admin, PerfilEnum.Super, PerfilEnum.Gerente)]
    public class DespesaController : Controller
    {
        private readonly IDespesaRepositorio _despesaRepositorio;
        private readonly ISessao _sessao;

        public DespesaController(IDespesaRepositorio despesaRepositorio, ISessao sessao)
        {
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
    }
}
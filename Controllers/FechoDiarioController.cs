using Analise.Filters;
using Analise.Helper;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class FechoDiarioController : Controller
    {
        private readonly IFechoDiarioRepositorio _fechoRepo;
        private readonly ISessao _sessao;

        public FechoDiarioController(IFechoDiarioRepositorio fechoRepo, ISessao sessao)
        {
            _fechoRepo = fechoRepo;
            _sessao = sessao;
        }

        [HttpGet]
        public async Task<IActionResult> RelatorioFecho(DateTime? data)
        {
            DateTime dataRef = data ?? DateTime.Now.Date;

            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var viewModel = await _fechoRepo.ObterDadosFechoDiarioAsync(dataRef);

            // Caminho físico explícito para garantir que o ASP.NET Core encontra a view
            return View("~/Views/FechoDiario/RelatorioFecho.cshtml", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FecharDia()
        {
            try
            {
                var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
                if (usuarioLogado == null) return RedirectToAction("Index", "Login");

                // Força sempre a data de hoje, ignorando o filtro do relatório
                DateTime dataHoje = DateTime.Now.Date;

                await _fechoRepo.FecharDiaEspecificoAsync(dataHoje, usuarioLogado.Id);

                TempData["MensagemSucesso"] = $"O dia de hoje ({dataHoje:dd/MM/yyyy}) foi fechado com sucesso e gravado na base de dados.";

                // Redireciona mantendo a visualização na data de hoje ou na data filtrada
                return RedirectToAction("RelatorioFecho", new { data = dataHoje.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                TempData["MensagemErro"] = $"Erro ao fechar o dia: {ex.Message}";
                return RedirectToAction("RelatorioFecho");
            }
        }
    }
}
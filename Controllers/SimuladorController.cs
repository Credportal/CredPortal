using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class SimuladorController : Controller
    {
        private readonly ISessao _sessao;
        private readonly IEntidadeRepositorio _entidadeRepositorio; // Injetar o repositório de entidades

        public SimuladorController(ISessao sessao, IEntidadeRepositorio entidadeRepositorio)
        {
            _sessao = sessao;
            _entidadeRepositorio = entidadeRepositorio;
        }

        public IActionResult Index(int? entidadeId)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Busca todas as entidades para preencher a lista de busca no simulador
            var entidades = _entidadeRepositorio.BuscarTodos();
            var viewModel = new EntidadeViewModel { ListaEntidades = entidades };

            return View(viewModel);
        }
    }
}
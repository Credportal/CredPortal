using Analise.Filters;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    
    [PaginaRestritaSomenteAdmin]
    public class EstadoController : Controller
    {
        private readonly IEstadoRepositorio _estadoRepositorio;
        public EstadoController(IEstadoRepositorio estadoRepositorio)
        {
            _estadoRepositorio = estadoRepositorio;
        }
        public IActionResult Criar()
        {
            var viewModel = new EstadoViewModel
            {
                EstadoNome = new EstadoModel(),
                ListaEstados = _estadoRepositorio.BuscarTodos()
            };

            return View(viewModel);
        }

        // GET: Editar
        public IActionResult Editar(int id)
        {
            var cargoSelecionado = _estadoRepositorio.ListarPorId(id);

            if (cargoSelecionado == null)
            {
                TempData["MensagemErro"] = "Cargo não encontrado.";
                return RedirectToAction("Index");
            }

            var viewModel = new EstadoViewModel
            {
                EstadoNome = cargoSelecionado,
                ListaEstados = _estadoRepositorio.BuscarTodos()
            };

            return View(viewModel);
        }
        [HttpPost]
        public IActionResult Criar(EstadoViewModel viewModel)
        {
            try
            {
                Console.WriteLine("Iniciando criação de cargo...");
                Console.WriteLine("Valor recebido: " + viewModel?.EstadoNome?.Estado);

                if (ModelState.IsValid)
                {
                    Console.WriteLine("ModelState inválido. Erros:");
                    foreach (var campo in ModelState)
                    {
                        foreach (var erro in campo.Value.Errors)
                        {
                            Console.WriteLine($"Erro no campo {campo.Key}: {erro.ErrorMessage}");
                        }
                    }

                    viewModel.ListaEstados = _estadoRepositorio.BuscarTodos();
                    TempData["MensagemErro"] = "Dados inválidos! Verifique os campos e tente novamente.";
                    return View(viewModel);
                }

                _estadoRepositorio.Adicionar(viewModel.EstadoNome);
                TempData["MensagemSucesso"] = "Cargo registado com sucesso!";
                return RedirectToAction("Criar");
            }
            catch (Exception erro)
            {
                Console.WriteLine("Erro ao criar cargo: " + erro.Message);
                viewModel.ListaEstados = _estadoRepositorio.BuscarTodos();
                TempData["MensagemErro"] = $"Erro ao registrar cargo: {erro.Message}";
                return View(viewModel);
            }
        }

        // POST: Editar
        [HttpPost]
        public IActionResult Editar(EstadoViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _estadoRepositorio.Actualizar(viewModel.EstadoNome);
                    TempData["MensagemSucesso"] = "Actualizado com sucesso!";
                    return RedirectToAction("Criar");
                }

                viewModel.ListaEstados = _estadoRepositorio.BuscarTodos();
                return View(viewModel);
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, erro ao actualizar. Detalhes: {erro.Message}";
                return RedirectToAction("Criar");
            }
        }
    }
}

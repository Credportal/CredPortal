using Analise.Filters;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;

namespace Analise.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class CargoController : Controller
    {
        private readonly ICargoRepositorio _cargoRepositorio;
        public CargoController(ICargoRepositorio cargoRepositorio)
        {
            _cargoRepositorio = cargoRepositorio;
        }

        // GET: Criar
        public IActionResult Criar()
        {
            var viewModel = new CargoViewModel
            {
                CargoNome = new CargoModel(),
                ListaCargos = _cargoRepositorio.BuscarTodos()
            };

            return View(viewModel);
        }

        // GET: Editar
        public IActionResult Editar(int id)
        {
            var cargoSelecionado = _cargoRepositorio.ListarPorId(id);

            if (cargoSelecionado == null)
            {
                TempData["MensagemErro"] = "Cargo não encontrado.";
                return RedirectToAction("Criar");
            }

            var viewModel = new CargoViewModel
            {
                CargoNome = cargoSelecionado,
                ListaCargos = _cargoRepositorio.BuscarTodos()
            };

            return View(viewModel);
        }

        // POST: Criar
        [HttpPost]
        public IActionResult Criar(CargoViewModel viewModel)
        {
            try
            {
                // CORRIGIDO: Se o modelo NÃO for válido, exibe os erros
                if (!ModelState.IsValid)
                {
                    viewModel.ListaCargos = _cargoRepositorio.BuscarTodos();
                    TempData["MensagemErro"] = "Dados inválidos! Verifique os campos e tente novamente.";
                    return View(viewModel);
                }

                _cargoRepositorio.Adicionar(viewModel.CargoNome);
                TempData["MensagemSucesso"] = "Cargo registado com sucesso!";
                return RedirectToAction("Criar");
            }
            catch (Exception erro)
            {
                viewModel.ListaCargos = _cargoRepositorio.BuscarTodos();
                TempData["MensagemErro"] = $"Erro ao registrar cargo: {erro.Message}";
                return View(viewModel);
            }
        }

        // POST: Editar
        [HttpPost]
        public IActionResult Editar(CargoViewModel viewModel)
        {
            try
            {
                // CORRIGIDO: Só actualiza se o ModelState for válido
                if (ModelState.IsValid)
                {
                    _cargoRepositorio.Actualizar(viewModel.CargoNome);
                    TempData["MensagemSucesso"] = "Actualizado com sucesso!";
                    return RedirectToAction("Criar");
                }

                viewModel.ListaCargos = _cargoRepositorio.BuscarTodos();
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
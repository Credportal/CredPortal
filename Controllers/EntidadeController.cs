using Analise.Filters;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    public class EntidadeController : Controller
    {
        private readonly IEntidadeRepositorio _entidadeRepositorio;

        public EntidadeController(IEntidadeRepositorio entidadeRepositorio)
        {
            _entidadeRepositorio = entidadeRepositorio;
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
    }
}
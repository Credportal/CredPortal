using Analise.Filters;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class AgenciaController : Controller
    {
        private readonly IAgenciaRepositorio _agenciaRepositorio;
        public AgenciaController(IAgenciaRepositorio agenciaRepositorio)
        {
            _agenciaRepositorio = agenciaRepositorio;
        }
        public IActionResult Index()
        {
            return View();
        }       
        public IActionResult Ver()
        {
            List<AgenciaModel> agencias = _agenciaRepositorio.BuscarTodos();
            return View(agencias);
        }
        public IActionResult Criar()
        {
            return View();
        }
        public IActionResult Editar(int id)
        {
            AgenciaModel agencia = _agenciaRepositorio.ListarPorId(id);
            return View(agencia);
        }
        [HttpPost]
        public async Task<IActionResult> Criar(AgenciaModel agencia)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Salvar no banco
                    agencia = _agenciaRepositorio.Adicionar(agencia);

                    TempData["MensagemSucesso"] = "Registado com sucesso!";
                    return RedirectToAction("Index");
                }

                return View("Index");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, erro ao registar a nova Tabernáculo! Detalhes: {erro.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> Editar(AgenciaModel agencia)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Buscar os dados atuais no banco para preservar o arquivo antigo
                    var agenciaExistente = _agenciaRepositorio.ListarPorId(agencia.Id);

                    if (agenciaExistente == null)
                    {
                        TempData["MensagemErro"] = "Não encontrada!";
                        return RedirectToAction("Ver");
                    }

                    _agenciaRepositorio.Actualizar(agencia);
                    TempData["MensagemSucesso"] = "Actualizado com sucesso!";
                    return RedirectToAction("Ver");
                }

                return View("Editar", agencia);
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops, erro ao actualizar Tabernáculo! Detalhes: {erro.Message}";
                return RedirectToAction("Editar", new { id = agencia.Id });
            }
        }
    }
}

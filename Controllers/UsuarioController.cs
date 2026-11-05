using Analise.Filters;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Analise.Controllers
{
    [PaginaRestritaSomenteAdmin]
    public class UsuarioController : Controller
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly IAgenciaRepositorio _agenciaRepositorio;
        private readonly ICargoRepositorio _cargoRepositorio;

        public UsuarioController(IUsuarioRepositorio usuarioRepositorio, IAgenciaRepositorio agenciaRepositorio, ICargoRepositorio cargoRepositorio)
        {
            _usuarioRepositorio = usuarioRepositorio;
            _agenciaRepositorio = agenciaRepositorio;
            _cargoRepositorio = cargoRepositorio;
        }
        public IActionResult Index()
        {
            List<UsuarioModel> usuarios = _usuarioRepositorio.BuscarTodos();
            return View(usuarios);
        }
        public IActionResult Criar()
        {
                var agencias = _agenciaRepositorio.BuscarTodos()
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Nome
            }).ToList();

            var cargos = _cargoRepositorio.BuscarTodos()
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Cargo
            }).ToList();

            var viewModel = new UsuarioModel
            {
                    ListaAgencias = agencias,
                    ListaCargos = cargos
            };

                return View(viewModel);
        }
        public IActionResult Teste()
        {
            return View();
        }
       
        public IActionResult Editar(int id)
        {
            UsuarioModel usuario = _usuarioRepositorio.ListarPorId(id);

            if (usuario == null)
            {
                TempData["MensagemErro"] = "Usuário não encontrado.";
                return RedirectToAction("Index");
            }

            // Buscar as agências e preencher no model
            var agencias = _agenciaRepositorio.BuscarTodos()
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Nome
                }).ToList();

            var cargos = _cargoRepositorio.BuscarTodos()
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Cargo
            }).ToList();

            usuario.ListaAgencias = agencias;
            usuario.ListaCargos = cargos;

            return View(usuario);
        }
        public IActionResult ApagarConfirmacao(int id)
        {
            UsuarioModel usuario = _usuarioRepositorio.ListarPorId(id);
            return View(usuario);
        }
        public IActionResult Apagar(int id)
        {
            try
            {
                bool apagado = _usuarioRepositorio.Apagar(id);

                if (apagado)
                {
                    TempData["MensagemSucesso"] = "Apagado com sucesso!";
                }
                else
                {
                    TempData["MensagemErro"] = $"Ops,erro ao apagar o usuário!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops,erro ao apagar o usuário! Mais detalhes do erro: {erro.Message}";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public IActionResult Criar(UsuarioModel usuario)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    usuario = _usuarioRepositorio.Adicionar(usuario);
                    TempData["MensagemSucesso"] = "Registado com sucesso!";
                    return RedirectToAction("Index");
                }

                return View(usuario);
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops,erro ao registar usuário, tente novamente! Detalhes: {erro.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult Editar(UsuarioModel usuarioSemSenha)
        {
            try
            {
                UsuarioModel usuario = null;

                if (!ModelState.IsValid)
                {
                    usuario = new UsuarioModel()
                    {
                        Id = usuarioSemSenha.Id,
                        Nome = usuarioSemSenha.Nome,
                        Login = usuarioSemSenha.Login,
                        Email = usuarioSemSenha.Email,
                        Perfil = usuarioSemSenha.Perfil,
                        CargoId = usuarioSemSenha.CargoId,

                        Criar = usuarioSemSenha.Criar,
                        Editar = usuarioSemSenha.Editar,
                        Visualizar = usuarioSemSenha.Visualizar,
                        Administrar = usuarioSemSenha.Administrar
                    };
                    usuario = _usuarioRepositorio.Actualizar(usuario);
                    TempData["MensagemSucesso"] = "Actualizado com sucesso!";
                    return RedirectToAction("Index");
                }

                return View("Editar", usuario);
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = $"Ops,erro ao actualizar o usuário, tente novamente! Detalhes: {erro.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}

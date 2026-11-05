using Analise.Filters;
using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class ConfiguracaoController : Controller
    {
        private readonly IConfiguracaoRepositorio _configuracaoRepositorio;
        private readonly ISessao _sessao; // Certifique-se de ajustar de acordo com a sua gestão de sessões

        public ConfiguracaoController(IConfiguracaoRepositorio configuracaoRepositorio, ISessao sessao)
        {
            _configuracaoRepositorio = configuracaoRepositorio;
            _sessao = sessao;
        }

        // GET: /Configuracao/Index
        public async Task<IActionResult> Index()
        {
            // Validação de sessão opcional, conforme o padrão do seu projeto
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            var config = await _configuracaoRepositorio.ObterConfiguracaoAsync();
            return View(config);
        }

        // POST: /Configuracao/Salvar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Salvar(ConfiguracaoModel model)
        {
            var usuarioLogado = _sessao.BuscarSessaoDoUsuario();
            if (usuarioLogado == null) return RedirectToAction("Index", "Login");

            if (ModelState.IsValid)
            {
                await _configuracaoRepositorio.AtualizarAsync(model);
                TempData["MensagemSucesso"] = "Configurações e esqueleto de contrato atualizados com sucesso!";
                return RedirectToAction("Index");
            }

            TempData["MensagemErro"] = "Ocorreu um erro ao atualizar as configurações. Verifique os campos.";
            return View("Index", model);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Analise.Enuns;
using Analise.Helper;
using System.Linq; // <--- ESTA LINHA RESOLVE O ERRO DO CONTAINS

namespace Analise.Filters
{
    public class PermissaoPerfilAttribute : ActionFilterAttribute
    {
        private readonly PerfilEnum[] _perfisPermitidos;

        public PermissaoPerfilAttribute(params PerfilEnum[] perfisPermitidos)
        {
            _perfisPermitidos = perfisPermitidos;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var sessao = context.HttpContext.RequestServices.GetService<ISessao>();
            var usuario = sessao?.BuscarSessaoDoUsuario();

            if (usuario == null)
            {
                context.Result = new RedirectToActionResult("Index", "Login", null);
                return;
            }

            // Se o perfil do utilizador não estiver na lista de permitidos, bloqueia o acesso
            if (!_perfisPermitidos.Any(p => p == usuario.Perfil))
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Result = new ContentResult
                {
                    Content = "Acesso negado: Não possui permissão para executar esta operação.",
                    ContentType = "text/plain; charset=utf-8"
                };
            }

            base.OnActionExecuting(context);
        }
    }
}
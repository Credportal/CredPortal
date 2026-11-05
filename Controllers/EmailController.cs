using Analise.Filters;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace Analise.Controllers
{
    [PaginaParaUsuarioLogado]
    public class EmailController : Controller
    {
        private readonly EmailService _emailService;

        public EmailController(EmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Email()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Email(EmailModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    _emailService.EnviarEmail(model.Para, model.Assunto, model.Mensagem);
                    ViewBag.Mensagem = "✅ Email enviado com sucesso!";
                }
                catch (Exception ex)
                {
                    ViewBag.Mensagem = "❌ Erro ao enviar: " + ex.Message;
                }
            }

            return View(model);
        }
    }
}

using Analise.Helper;
using Analise.Models;
using Analise.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Analise.Controllers
{
    public class LoginController : Controller
    {
        private readonly IUsuarioRepositorio _usuarioRepositorio;
        private readonly ISessao _sessao;
        private readonly IEmail _email;
        private readonly IAgenciaRepositorio _agenciaRepositorio;
        private readonly EmailService _emailService;

        public LoginController(IUsuarioRepositorio usuarioRepositorio, 
            ISessao sessao,IEmail email, 
            IAgenciaRepositorio agenciaRepositorio, 
            EmailService emailService) 
        {
            _usuarioRepositorio = usuarioRepositorio;
            _sessao=sessao;
            _email = email;
            _agenciaRepositorio = agenciaRepositorio;
            _emailService = emailService;
        }
        [HttpGet]
        public IActionResult ObterEmailPorLogin(string login)
        {
            if (string.IsNullOrEmpty(login))
                return Json(new { sucesso = false, mensagem = "Login inválido" });

            var usuario = _usuarioRepositorio.BuscarLogin(login);

            if (usuario != null)
                return Json(new { sucesso = true, email = usuario.Email });

            return Json(new { sucesso = false, mensagem = "Usuário não encontrado" });
        }
        public IActionResult Index()
        {
            if (_sessao.BuscarSessaoDoUsuario() != null) return RedirectToAction("Index", "Home");
            //Se usuário estiver logado, redirecionar para Home

            return View();
        }
        [HttpGet]
        public IActionResult Email()
        {
            return View();
        }
        public IActionResult RedefinirSenha()
        {
            return View();
        }
       
        [HttpPost]
        public IActionResult Entrar(LoginModel loginModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    UsuarioModel usuario = _usuarioRepositorio.BuscarLogin(loginModel.Login);
                    if (usuario != null)
                    {
                        if (usuario.SenhaValida(loginModel.Senha))
                        {
                            _sessao.CriarSessaoDoUsuario(usuario);

                            // 🔹 Salvar ID da Agência na sessão
                            HttpContext.Session.SetInt32("UsuarioId", usuario.Id);
                            HttpContext.Session.SetInt32("AgenciaId", usuario.AgenciaId ?? 0); // Se for null, assume 0

                            
                            return RedirectToAction("Index", "Home");
                        }
                        TempData["MensagemErro"] = "A senha do usuário é inválida. Por favor, tente novamente.";
                    }
                    TempData["MensagemErro"] = "Usuário e/ou senha inválido(s). Por favor, tente novamente.";
                }
                return View("Index");
            }
            catch (Exception erro)
            {
                TempData["MensagemErro"] = "A senha do usuário é inválida. Por favor, tente novamente.";
                //TempData["MensagemErro"] = $"Ops, erro ao aceder o sistema: {erro.Message}";
                return RedirectToAction("Index");
            }
        }
        //[HttpPost] 
        //public IActionResult EnviarLinkParaRedefinirSenha(LoginModel redefinirSenhaModel)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            UsuarioModel usuario = _usuarioRepositorio.BuscarPorEmailELogin(redefinirSenhaModel.Email,redefinirSenhaModel.Login);
        //            if (usuario != null)
        //            {
        //                string novaSenha=usuario.GerarNovaSenha();                       
        //                string mensagem = $"Sua nova senha é: {novaSenha}";

        //                bool emailEnviado = _email.Enviar(usuario.Email, "Casy Portal - Nova Senha", mensagem);

        //                if (emailEnviado)
        //                {
        //                    _usuarioRepositorio.Actualizar(usuario);
        //                    TempData["MensagemSucesso"] = $"Enviamos uma nova senha para o seu e-mail associado.";
        //                }
        //                else
        //                {
        //                    TempData["MensagemErro"] = $"Erro ao enviar e-mail. Por favor, tente novamente.";                           
        //                }
        //                Debug.WriteLine($"emailEnviado: {emailEnviado}");
        //                return RedirectToAction("Index", "Login");
        //            }
        //            TempData["MensagemErro"] = $"Erro ao redefinir a sua senha. Por favor, verifique os dados fornecidos.";
        //        }
        //        return View("Index");
        //    }
        //    catch (Exception erro)
        //    {
        //        TempData["MensagemErro"] = $"Ops,erro ao redefinir a sua senha, tente novamente! Mais detalhes do erro: {erro.Message}";
        //        return RedirectToAction("Index");
        //    }
        //}

        //[HttpPost]
        //public IActionResult EnviarLinkParaRedefinirSenha(LoginModel redefinirSenhaModel)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            // Buscar usuário pelo login/email
        //            UsuarioModel usuario = _usuarioRepositorio.BuscarPorEmailELogin(redefinirSenhaModel.Email, redefinirSenhaModel.Login);
        //            if (usuario != null)
        //            {
        //                // Gerar nova senha aleatória
        //                string novaSenha = usuario.GerarNovaSenha();

        //                // Criar mensagem incluindo a senha
        //                //string mensagem = $"Sua nova senha é: <b>{novaSenha}</b><br><br>{redefinirSenhaModel.Mensagem}<br><br>Por questões de segurança, recomendamos que altere a senha após o acesso!";
        //                // Criar mensagem HTML incluindo a nova senha
        //                string mensagem = $@"
        //                    <!DOCTYPE html>
        //                    <html>
        //                    <head>
        //                        <meta charset='utf-8'>
        //                        <style>
        //                            body {{
        //                                font-family: Arial, sans-serif;
        //                                background-color: #f6f6f6;
        //                                padding: 20px;
        //                            }}
        //                            .email-container {{
        //                                background-color: #ffffff;
        //                                border-radius: 8px;
        //                                padding: 20px;
        //                                max-width: 600px;
        //                                margin: auto;
        //                                box-shadow: 0 0 10px rgba(0,0,0,0.1);
        //                            }}
        //                            .header {{
        //                                text-align: center;
        //                                font-size: 24px;
        //                                color: #1a73e8;
        //                                margin-bottom: 20px;
        //                            }}
        //                            .content {{
        //                                font-size: 16px;
        //                                line-height: 1.5;
        //                                color: #333333;
        //                            }}
        //                            .senha {{
        //                                font-weight: bold;
        //                                font-size: 18px;
        //                                color: #000;
        //                                background-color: #f1f1f1;
        //                                padding: 8px;
        //                                border-radius: 4px;
        //                                display: inline-block;
        //                                margin: 10px 0;
        //                            }}
        //                            .footer {{
        //                                font-size: 12px;
        //                                color: #777777;
        //                                margin-top: 20px;
        //                                text-align: center;
        //                            }}
        //                            a {{
        //                                color: #1a73e8;
        //                                text-decoration: none;
        //                            }}
        //                        </style>
        //                    </head>
        //                    <body>
        //                        <div class='email-container'>
        //                            <div class='header'>Casy Portal - Nova Senha</div>
        //                            <div class='content'>
        //                                Olá <b>{redefinirSenhaModel.Login}</b>,<br><br>
        //                                Sua nova senha é:
        //                                <div class='senha'>{novaSenha}</div>
        //                                {redefinirSenhaModel.Mensagem}<br><br>
        //                                Por questões de segurança, recomendamos que altere a senha após o acesso!
        //                            </div>
        //                            <div class='footer'>
        //                                Este é um e-mail automático, por favor não responda.<br>
        //                                © 2025 Casy Portal
        //                            </div>
        //                        </div>
        //                    </body>
        //                    </html>"
        //                ;


        //                // Enviar email
        //                _emailService.EnviarEmail(usuario.Email, redefinirSenhaModel.Assunto ?? "Casy Portal - Nova Senha", mensagem);

        //                _usuarioRepositorio.Actualizar(usuario);
        //                TempData["MensagemSucesso"] = "✅ Enviamos uma nova senha para o seu e-mail associado.";
                       
        //                // Redireciona para Login após enviar
        //                return RedirectToAction("Index", "Login");
        //            }

        //            TempData["MensagemErro"] = "❌ Usuário não encontrado. Verifique os dados fornecidos.";
        //        }
        //        else
        //        {
        //            TempData["MensagemErro"] = "❌ Preencha todos os campos corretamente.";
        //        }

        //        // Se der erro, retorna para a mesma view para corrigir
        //        return View("RedefinirSenha");
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["MensagemErro"] = $"❌ Ops, erro ao redefinir a senha! Detalhes: {ex.Message}";
        //        return RedirectToAction("Index", "Login");
        //    }
        //}


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

        //[HttpPost]
        //public IActionResult EnviarLinkParaRedefinirSenha(LoginModel redefinirSenhaModel)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //        {
        //            // Buscar usuário pelo login/email
        //            UsuarioModel usuario = _usuarioRepositorio.BuscarPorEmailELogin(redefinirSenhaModel.Email, redefinirSenhaModel.Login);
        //            if (usuario != null)
        //            {
        //                // Gerar nova senha aleatória
        //                string novaSenha = usuario.GerarNovaSenha();

        //                // Criar mensagem HTML
        //                string mensagem = $@"
        //            <!DOCTYPE html>
        //            <html>
        //            <head>
        //                <meta charset='utf-8'>
        //                <style>
        //                    body {{ font-family: Arial, sans-serif; background-color: #f6f6f6; padding: 20px; }}
        //                    .email-container {{ background-color: #fff; border-radius: 8px; padding: 20px; max-width: 600px; margin: auto; box-shadow: 0 0 10px rgba(0,0,0,0.1); }}
        //                    .header {{ text-align: center; font-size: 24px; color: #1a73e8; margin-bottom: 20px; }}
        //                    .content {{ font-size: 16px; line-height: 1.5; color: #333; }}
        //                    .senha {{ font-weight: bold; font-size: 18px; color: #000; background-color: #f1f1f1; padding: 8px; border-radius: 4px; display: inline-block; margin: 10px 0; }}
        //                    .footer {{ font-size: 12px; color: #777; margin-top: 20px; text-align: center; }}
        //                    a {{ color: #1a73e8; text-decoration: none; }}
        //                </style>
        //            </head>
        //            <body>
        //                <div class='email-container'>
        //                    <div class='header'>Casy Portal - Nova Senha</div>
        //                    <div class='content'>
        //                        Olá <b>{redefinirSenhaModel.Login}</b>,<br><br>
        //                        Sua nova senha é:
        //                        <div class='senha'>{novaSenha}</div>
        //                        {redefinirSenhaModel.Mensagem}<br><br>
        //                        Por questões de segurança, recomendamos que altere a senha após o acesso!
        //                    </div>
        //                    <div class='footer'>
        //                        Este é um e-mail automático, por favor não responda.<br>
        //                        © 2025 Casy Portal
        //                    </div>
        //                </div>
        //            </body>
        //            </html>";

        //                //// Enviar email
        //                //_emailService.EnviarEmail(usuario.Email, redefinirSenhaModel.Assunto ?? "Casy Portal - Nova Senha", mensagem);

        //                //// Atualizar usuário (se necessário, ex.: senha hash)
        //                _usuarioRepositorio.Actualizar(usuario);

        //                //// ✅ Registrar ticket na base de dados
        //                //TicketModel ticket = new TicketModel
        //                //{
        //                //    Usuario = usuario.Login,
        //                //    Email = usuario.Email,
        //                //    Solucao = novaSenha,
        //                //    Assunto = "Redefinir Senha",                                // definido no controller
        //                //    Problema = "Já consegui gerar uma senha, peço para partilhar comigo!", // definido no controller
        //                //    Estado = "Processo",
        //                //    DataCriacao = DateTime.Now
        //                //};
        //                //_ticketRepositorio.Adicionar(ticket);

        //                TempData["MensagemSucesso"] = "✅ Enviamos uma nova senha para o seu e-mail associado e registramos o ticket.";

        //                // Redireciona para Login após enviar
        //                return RedirectToAction("Index", "Login");
        //            }

        //            TempData["MensagemErro"] = "❌ Usuário não encontrado. Verifique os dados fornecidos.";
        //        }
        //        else
        //        {
        //            TempData["MensagemErro"] = "❌ Preencha todos os campos corretamente.";
        //        }

        //        // Se der erro, retorna para a mesma view para corrigir
        //        return View("RedefinirSenha");
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["MensagemErro"] = $"❌ Ops, erro ao redefinir a senha! Detalhes: {ex.Message}";
        //        return RedirectToAction("Index", "Login");
        //    }
        //}

        [HttpPost]
        public IActionResult EnviarLinkParaRedefinirSenha(LoginModel model)
        {
            // Busca o utilizador pelo login utilizando o repositório
            var usuario = _usuarioRepositorio.BuscarLogin(model.Login);

            if (usuario == null)
            {
                TempData["MensagemErro"] = "Utilizador não encontrado.";
                return RedirectToAction("RedefinirSenha");
            }

            // 🔹 Gera a nova senha encriptada/SHA através do método do modelo
            string novaSenha = usuario.GerarNovaSenha();

            // Atualiza e guarda o registo na base de dados
            _usuarioRepositorio.Actualizar(usuario);

            // Passa a senha em texto limpo para o TempData para que possa vê-la no ecrã (enquanto o hash/SHA fica guardado na BD)
            TempData["MensagemSucesso"] = $"Senha redefinida com sucesso! A nova senha do utilizador é: {novaSenha}";

            return RedirectToAction("RedefinirSenha");
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Opcional, mas recomendado por segurança
        public IActionResult TerminarSessao()
        {
            _sessao.RemoverSessaoDoUsuario();
            return RedirectToAction("Index", "Login");
        }
    }
}

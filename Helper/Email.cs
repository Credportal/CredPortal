using Analise.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Net;
//using System.Net.Mail;
using System;


namespace Analise.Helper
{
    public class Email : IEmail
    {
        private readonly IConfiguration _configuration;

        public Email(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool Enviar(string para, string assunto, string mensagem)
        {
            try
            {
                string host = _configuration.GetValue<string>("SMTP:Host");
                string nome = _configuration.GetValue<string>("SMTP:Nome");
                string userName = _configuration.GetValue<string>("SMTP:UserName");
                string senha = _configuration.GetValue<string>("SMTP:Senha");
                int porta = _configuration.GetValue<int>("SMTP:Porta");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(nome, userName));
                email.To.Add(MailboxAddress.Parse(para));
                email.Subject = assunto;
                email.Body = new TextPart("html") { Text = mensagem };

                using (var smtp = new SmtpClient())
                {
                    smtp.Connect(host, porta, SecureSocketOptions.SslOnConnect); // Gmail usa 465 com SSL
                    smtp.Authenticate(userName, senha);
                    smtp.Send(email);
                    smtp.Disconnect(true);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Gravar log do erro
                Console.WriteLine("Erro ao enviar email: " + ex.Message);
                return false;
            }
        }
    }
}

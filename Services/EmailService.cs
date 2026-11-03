using Analise.Enuns;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;

namespace Analise.Models
{
    public class EmailService
    {
        private readonly SmtpSettings _smtp;

        public EmailService(IOptions<SmtpSettings> smtp)
        {
            _smtp = smtp.Value;
        }

        public void EnviarEmail(string para, string assunto, string corpo)
        {
            try
            {
                // Criar o email
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("Casy Portal", _smtp.From));
                email.To.Add(MailboxAddress.Parse(para));
                email.Subject = assunto;
                email.Body = new TextPart("html") { Text = corpo };

                // Enviar usando MailKit
                using (var smtp = new SmtpClient())
                {
                    smtp.Connect(_smtp.Host, _smtp.Port, SecureSocketOptions.SslOnConnect);
                    smtp.Authenticate(_smtp.UserName, _smtp.Password);
                    smtp.Send(email);
                    smtp.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao enviar email: {ex.Message}");
            }
        }
    }
}
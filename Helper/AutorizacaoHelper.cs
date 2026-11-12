using Analise.Enuns;
using Analise.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
//using System.Net.Mail;
using System;
using System.Net;


namespace Analise.Helper
{
    public static class AutorizacaoHelper
    {
        // Gestão de Utilizadores (Apenas Admin e Super)
        public static bool PodeGerirUtilizadores(UsuarioModel usuario)
        {
            return usuario != null && (usuario.Perfil == PerfilEnum.Admin || usuario.Perfil == PerfilEnum.Super);
        }

        // Decisão / Aprovação de Créditos (Admin, Super, Gerente)
        public static bool PodeAprovarCredito(UsuarioModel usuario)
        {
            return usuario != null && (usuario.Perfil == PerfilEnum.Admin || usuario.Perfil == PerfilEnum.Super || usuario.Perfil == PerfilEnum.Gerente);
        }

        // Realizar o Fecho do Dia (Admin, Super, Gerente)
        public static bool PodeFazerFecho(UsuarioModel usuario)
        {
            return usuario != null && (usuario.Perfil == PerfilEnum.Admin || usuario.Perfil == PerfilEnum.Super || usuario.Perfil == PerfilEnum.Gerente);
        }

        // Movimentos de Caixa e Despachos (Admin, Super, Gerente, Caixa, Administrativo)
        public static bool PodeMovimentarCaixaOuDespachar(UsuarioModel usuario)
        {
            return usuario != null && (usuario.Perfil == PerfilEnum.Admin ||
                                     usuario.Perfil == PerfilEnum.Super ||
                                     usuario.Perfil == PerfilEnum.Gerente ||
                                     usuario.Perfil == PerfilEnum.Caixa ||
                                     usuario.Perfil == PerfilEnum.Administrativo);
        }
    }
}

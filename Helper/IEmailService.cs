using DotNetEnv;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace APIseverino.Helpers;

public interface IEmailService
{
    Task EnviarCodigo(string destinatario, string codigo, string funcao, string titulo);
}

public class EmailService : IEmailService
{
    public async Task EnviarCodigo(string destinatario, string codigo, string funcao, string titulo)
    {
        if (File.Exists(".env.test"))
            Env.Load(".env.test");
        else
            Env.Load(".env");

        var clientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET");
        var refreshToken = Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
            Scopes = new[] { GmailService.Scope.GmailSend },
            DataStore = null
        });

        // Credencial usando Refresh Token
        var credential = new UserCredential(flow, "user", new TokenResponse
        {
            RefreshToken = refreshToken
        });

        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "APIseverino"
        });

        // Codifica o Subject em UTF8 corretamente
        var tituloCodificado = Convert.ToBase64String(Encoding.UTF8.GetBytes(titulo));

        String segprgf = "Esse código expira em 30 minutos. Se você não solicitou, ignore este email.";

        // Monta mensagem RFC 822
        string mensagemRaw =
            $"To: {destinatario}\r\n" +
            $"Subject: =?UTF-8?B?{tituloCodificado}?=\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: text/html; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\n" +
            $"<p>{funcao} {codigo}<br><br>" +
            $"{segprgf}</p>";

        // UTF8 sem BOM para evitar problemas de encoding
        var bytes = new UTF8Encoding(false).GetBytes(mensagemRaw);

        var msg = new Message
        {
            Raw = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "")
        };

        // Envia email
        await service.Users.Messages.Send(msg, "me").ExecuteAsync();
    }
}
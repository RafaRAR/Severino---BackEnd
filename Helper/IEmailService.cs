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
    Task EnviarCodigo(String destinatario, String codigo, String funcao, String titulo);
}
public class EmailService : IEmailService
{
    public async Task EnviarCodigo(String destinatario, String codigo, String funcao, String titulo)
    {
        // Carrega o arquivo de teste especificamente
        if (File.Exists(".env.test"))
        {
            Env.Load(".env.test");
        }
        else
        {
            Env.Load(".env");
        }
            // 1. Pega as chaves das variáveis de ambiente do Render
            var clientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET");
        var refreshToken = Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN");

        Console.WriteLine($"DEBUG RENDER - ID presente: {!string.IsNullOrEmpty(clientId)}");
        Console.WriteLine($"DEBUG RENDER - Secret presente: {!string.IsNullOrEmpty(clientSecret)}");
        Console.WriteLine($"DEBUG RENDER - Token presente: {!string.IsNullOrEmpty(refreshToken)}");

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

        // Aqui criamos a credencial DIRETAMENTE com o refresh token do Render
        var credential = new UserCredential(flow, "user", new TokenResponse
        {
            RefreshToken = refreshToken
        });

        // O SDK do Google vai renovar o Access Token (3600s) automaticamente usando o Refresh Token
        var service = new GmailService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "APIseverino"
        });

        // 4. Monta a mensagem (Formato RFC 822)
        string mensagemRaw = $"To: {destinatario}\r\n" +
                            $"Subject: {titulo}\r\n" +
                            "Content-Type: text/html; charset=utf-8\r\n\r\n" +
                            $"<p>{funcao} {codigo}<br><br>\r\n\r\nEsse código expira em 30 minutos. Se você não solicitou, ignore este email.</p>";

        var msg = new Message
        {
            Raw = Convert.ToBase64String(Encoding.UTF8.GetBytes(mensagemRaw))
                  .Replace('+', '-').Replace('/', '_').Replace("=", "")
        };

        // 5. Envia de fato
        await service.Users.Messages.Send(msg, "me").ExecuteAsync();
    }
}
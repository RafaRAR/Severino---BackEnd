using MailKit.Net.Smtp;
using MimeKit;

namespace APIseverino.Helpers;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string to, string code);
    Task SendPasswordResetCodeAsync(string to, string code);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendVerificationCodeAsync(string to, string code)
    {
        var message = new MimeMessage();
        var fromName = _config["Smtp:FromName"] ?? "NoReply";
        var fromEmail = _config["Smtp:FromEmail"] ?? _config["Smtp:User"];
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = "Código de verificação";

        var bodyText = $"Seu código de verificação é: {code}\n\nEsse código expira em 30 minutos.";
        message.Body = new TextPart("plain") { Text = bodyText };

        using var client = new SmtpClient();
        var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host não configurado");
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var user = _config["Smtp:User"];
        var pass = _config["Smtp:Pass"];

        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
        if (!string.IsNullOrEmpty(user))
            await client.AuthenticateAsync(user, pass ?? string.Empty);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public async Task SendPasswordResetCodeAsync(string to, string code)
    {
        var message = new MimeMessage();
        var fromName = _config["Smtp:FromName"] ?? "NoReply";
        var fromEmail = _config["Smtp:FromEmail"] ?? _config["Smtp:User"];
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = "Código para reset de senha";

        var bodyText = $"Você solicitou redefinição de senha. Use este código para alterar sua senha: {code}\n\nEsse código expira em 30 minutos. Se você não solicitou, ignore este email.";
        message.Body = new TextPart("plain") { Text = bodyText };

        using var client = new SmtpClient();
        var host = _config["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host não configurado");
        var port = int.Parse(_config["Smtp:Port"] ?? "587");
        var user = _config["Smtp:User"];
        var pass = _config["Smtp:Pass"];

        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
        if (!string.IsNullOrEmpty(user))
            await client.AuthenticateAsync(user, pass ?? string.Empty);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
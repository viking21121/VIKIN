using System.Net;
using System.Net.Mail;

namespace VikingMobile.Application.Auth;

public sealed class GmailVerificationCodeSender
{
    private readonly string _sender;
    private readonly string _appPassword;

    public GmailVerificationCodeSender(string sender, string? appPassword)
    {
        _sender = sender;
        _appPassword = appPassword ?? "";
    }

    public async Task EnviarAsync(string destino, string codigo)
    {
        if (string.IsNullOrWhiteSpace(_appPassword))
            throw new InvalidOperationException("Falta la contraseña de aplicación de Gmail.");

        using var message = new MailMessage(_sender, destino)
        {
            Subject = "Código de verificación Viking Mobile",
            Body = $"Tu código de verificación Viking Mobile es: {codigo}\n\nEste código vence en 10 minutos.",
            IsBodyHtml = false
        };

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_sender, _appPassword),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        await client.SendMailAsync(message);
    }
}

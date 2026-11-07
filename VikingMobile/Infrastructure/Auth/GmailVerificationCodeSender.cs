using System.Net;
using System.Net.Mail;
using VikingMobile.Application.Auth;

namespace VikingMobile.Infrastructure.Auth;

public sealed class GmailVerificationCodeSender : IVerificationCodeSender
{
    public const string SenderEmail = "avendanoalejandro841@gmail.com";
    private const string PasswordKey = "viking_gmail_app_password";

    public async Task EnviarAsync(string destino, string codigo)
    {
        string password = await SecureStorage.Default.GetAsync(PasswordKey) ?? "";
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Falta configurar la contraseña de aplicación de Gmail.");

        using var message = new MailMessage(SenderEmail, destino)
        {
            Subject = "Código de verificación — Viking Mobile",
            Body = $"Tu código de verificación de Viking Mobile es: {codigo}\n\nCaduca en 10 minutos. Si no solicitaste este código, ignora este mensaje.",
            IsBodyHtml = false
        };
        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(SenderEmail, password)
        };
        await client.SendMailAsync(message);
    }
}

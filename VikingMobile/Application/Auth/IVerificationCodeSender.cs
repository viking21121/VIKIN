namespace VikingMobile.Application.Auth;

public interface IVerificationCodeSender
{
    Task EnviarAsync(string destino, string codigo);
}

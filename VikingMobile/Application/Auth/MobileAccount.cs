namespace VikingMobile.Application.Auth;

public enum MobileAccountRole
{
    Cliente,
    Administrador
}

public sealed class MobileAccount
{
    public MobileAccountRole Rol { get; set; }
    public string Usuario { get; set; } = "";
    public string Correo { get; set; } = "";
    public string PinHash { get; set; } = "";
    public bool CorreoVerificado { get; set; }
}

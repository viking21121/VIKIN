namespace VikingMobile.Application.Cliente;

public static class ClienteSession
{
    public static string CodigoAlumno { get; private set; } = "";
    public static CuentaClienteResponse? Cuenta { get; private set; }
    public static bool EstaActiva => Cuenta != null;

    public static void Establecer(string codigoAlumno, CuentaClienteResponse cuenta)
    {
        CodigoAlumno = codigoAlumno.Trim().ToUpperInvariant();
        Cuenta = cuenta;
    }

    public static void ActualizarCuenta(CuentaClienteResponse cuenta) => Cuenta = cuenta;

    public static void CerrarSesion()
    {
        CodigoAlumno = "";
        Cuenta = null;
    }
}

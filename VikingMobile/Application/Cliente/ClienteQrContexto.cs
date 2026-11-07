namespace VikingMobile.Application.Cliente;

/// <summary>
/// Igual que <see cref="ClienteSession"/>: pasa el código de alumno
/// leído por la cámara en <c>ClienteEscanearQrPage</c> de vuelta a
/// <c>ClienteLoginPage</c>, sin depender de parámetros de navegación.
/// </summary>
public static class ClienteQrContexto
{
    public static string? CodigoEscaneado { get; private set; }

    public static void Establecer(string codigo) => CodigoEscaneado = codigo;

    public static string? RecuperarYLimpiar()
    {
        string? valor = CodigoEscaneado;
        CodigoEscaneado = null;
        return valor;
    }
}

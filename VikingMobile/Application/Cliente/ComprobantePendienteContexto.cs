namespace VikingMobile.Application.Cliente;

/// <summary>
/// Igual que <see cref="ClienteSession"/>: mantiene en memoria la
/// imagen que el alumno acaba de elegir (cámara o galería) para
/// pasarla a <c>ConfirmarComprobantePage</c> sin depender de
/// parámetros de navegación.
/// </summary>
public static class ComprobantePendienteContexto
{
    public static long SolicitudId { get; private set; }

    public static byte[]? Bytes { get; private set; }

    public static string ContentType { get; private set; } = "image/png";

    public static void Establecer(long solicitudId, byte[] bytes, string contentType)
    {
        SolicitudId = solicitudId;
        Bytes = bytes;
        ContentType = contentType;
    }

    public static void Limpiar()
    {
        SolicitudId = 0;
        Bytes = null;
        ContentType = "image/png";
    }
}

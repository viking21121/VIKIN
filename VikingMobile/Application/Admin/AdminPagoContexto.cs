namespace VikingMobile.Application.Admin;

/// <summary>
/// Igual que <see cref="AdminAlumnoContexto"/>: mantiene en memoria la
/// solicitud de pago que el administrador seleccionó en
/// <c>AdminPagosPendientesPage</c> para pasarla a
/// <c>AdminPagoRevisionPage</c> sin depender de parámetros de navegación.
/// </summary>
public static class AdminPagoContexto
{
    public static SolicitudPagoAdminResumen? SolicitudSeleccionada { get; private set; }

    public static void Seleccionar(SolicitudPagoAdminResumen solicitud) =>
        SolicitudSeleccionada = solicitud;

    public static void Limpiar() => SolicitudSeleccionada = null;
}

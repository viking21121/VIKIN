namespace VikingMobile.Application.Admin;

/// <summary>
/// Igual que <see cref="Cliente.ClienteSession"/>/<see cref="AdminSession"/>:
/// mantiene en memoria el alumno que el administrador seleccionó en
/// <c>AdminAlumnosPage</c> para pasarlo a <c>AdminAlumnoDetallePage</c>
/// sin depender de parámetros de navegación.
/// </summary>
public static class AdminAlumnoContexto
{
    public static AlumnoAdminResumen? AlumnoSeleccionado { get; private set; }

    public static void Seleccionar(AlumnoAdminResumen alumno) =>
        AlumnoSeleccionado = alumno;

    public static void Limpiar() => AlumnoSeleccionado = null;
}

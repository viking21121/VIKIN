namespace VikingMobile.Application.Activities;

public static class ActividadContexto
{
    public static int? ActividadId { get; private set; }

    public static string NombreActividad { get; private set; } = "";

    public static void Establecer(ActividadMobile actividad)
    {
        ActividadId = actividad.Id;
        NombreActividad = actividad.Nombre;
    }

    public static void Limpiar()
    {
        ActividadId = null;
        NombreActividad = "";
    }
}
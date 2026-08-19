using AsistenciaNatacion.Application.App;

namespace AsistenciaNatacion;

public class GuardarConfiguracion
{
    private readonly IConfiguracionRepository repository;

    public GuardarConfiguracion(IConfiguracionRepository repository)
    {
        this.repository = repository;
    }

    public void Ejecutar(
        ConfiguracionEstablecimiento configuracion,
        int? actividadId = null)
    {
        if (string.IsNullOrWhiteSpace(configuracion.Nombre))
            throw new ArgumentException(
                "El nombre del establecimiento es obligatorio.");

        configuracion.Nombre = configuracion.Nombre.Trim();
        configuracion.Identificacion =
            configuracion.Identificacion?.Trim() ?? "";
        configuracion.TipoEstablecimiento =
            configuracion.TipoEstablecimiento?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(
                configuracion.TipoEstablecimiento))
        {
            configuracion.TipoEstablecimiento = "Otro";
        }

        int id = actividadId ?? ContextoActividad.ActividadId;

        if (id <= 0)
            throw new InvalidOperationException(
                "No hay una actividad seleccionada.");

        repository.Guardar(id, configuracion);
    }
}

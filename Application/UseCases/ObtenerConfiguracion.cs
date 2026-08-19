using AsistenciaNatacion.Application.App;

namespace AsistenciaNatacion;

public class ObtenerConfiguracion
{
    private readonly IConfiguracionRepository repository;

    public ObtenerConfiguracion(IConfiguracionRepository repository)
    {
        this.repository = repository;
    }

    public ConfiguracionEstablecimiento Ejecutar(int? actividadId = null)
    {
        int id = actividadId ?? ContextoActividad.ActividadId;

        if (id <= 0)
            throw new InvalidOperationException(
                "No hay una actividad seleccionada.");

        return repository.Obtener(id);
    }
}

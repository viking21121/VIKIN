using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerIdsAsistieronHoy
{
    private readonly IAasistenciaRepository repository;
    public ObtenerIdsAsistieronHoy(IAasistenciaRepository repository) => this.repository = repository;
    public HashSet<int> Ejecutar(int actividadId) => repository.ObtenerIdsAsistieronHoy(actividadId);
}

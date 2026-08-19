using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerDashboard
{
    private readonly IEstadoAlumnoRepository repository;
    public ObtenerDashboard(IEstadoAlumnoRepository repository) => this.repository = repository;
    public List<EstadoAlumnoMes> Ejecutar(int anio, int mes, int actividadId) => repository.ObtenerDashboard(anio, mes, actividadId);
}

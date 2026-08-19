using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerEstadoAlumno
{
    private readonly IEstadoAlumnoRepository repository;
    public ObtenerEstadoAlumno(IEstadoAlumnoRepository repository) => this.repository = repository;
    public EstadoAlumnoMes? Ejecutar(int alumnoId, int anio, int mes) => repository.ObtenerEstado(alumnoId, anio, mes);
}

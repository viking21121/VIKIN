using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerAlumnoPorCodigo
{
    private readonly IAasistenciaRepository repository;
    public ObtenerAlumnoPorCodigo(IAasistenciaRepository repository) => this.repository = repository;
    public Alumno? Ejecutar(string codigo, int actividadId) => repository.ObtenerAlumnoPorCodigo(codigo, actividadId);
}

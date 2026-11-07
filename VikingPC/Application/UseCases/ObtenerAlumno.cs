using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerAlumno
{
    private readonly IAlumnoRepository repository;
    public ObtenerAlumno(IAlumnoRepository repository) => this.repository = repository;

    public Alumno? Ejecutar(int alumnoId) => repository.ObtenerPorId(alumnoId);
}

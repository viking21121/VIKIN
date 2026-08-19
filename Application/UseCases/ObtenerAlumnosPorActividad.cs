using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerAlumnosPorActividad
{
    private readonly IAlumnoRepository repository;

    public ObtenerAlumnosPorActividad(IAlumnoRepository repository)
    {
        this.repository = repository;
    }

    public List<Alumno> Ejecutar(int actividadId)
    {
        return repository.ObtenerPorActividad(actividadId);
    }
}
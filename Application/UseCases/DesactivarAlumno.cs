using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class DesactivarAlumno
{
    private readonly IAlumnoRepository repository;
    private readonly IAuditoriaRepository? auditoriaRepository;

    public DesactivarAlumno(IAlumnoRepository repository, IAuditoriaRepository? auditoriaRepository = null)
    {
        this.repository = repository;
        this.auditoriaRepository = auditoriaRepository;
    }

    public void Ejecutar(int alumnoId, string usuario = "Sistema")
    {
        if (alumnoId <= 0)
            throw new ArgumentException("El alumno seleccionado no es válido.");

        repository.Desactivar(alumnoId);

        try
        {
            auditoriaRepository?.Registrar(
                "Dar de baja alumno",
                $"AlumnoId={alumnoId}; Se conserva el historial.",
                usuario);
        }
        catch
        {
            // La auditoría no debe impedir la baja lógica.
        }
    }
}

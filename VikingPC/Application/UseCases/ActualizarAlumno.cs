using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ActualizarAlumno
{
    private readonly IAlumnoRepository repository;
    private readonly IAuditoriaRepository? auditoriaRepository;

    public ActualizarAlumno(IAlumnoRepository repository, IAuditoriaRepository? auditoriaRepository = null)
    {
        this.repository = repository;
        this.auditoriaRepository = auditoriaRepository;
    }

    public void Ejecutar(int alumnoId, int planId, string telefono, string usuario = "Sistema")
    {
        if (alumnoId <= 0)
            throw new ArgumentException("El alumno seleccionado no es válido.");

        if (planId <= 0)
            throw new ArgumentException("El plan seleccionado no es válido.");

        repository.ActualizarPlanYTelefono(alumnoId, planId, telefono?.Trim() ?? string.Empty);

        try
        {
            auditoriaRepository?.Registrar(
                "Actualizar alumno",
                $"AlumnoId={alumnoId}; PlanId={planId}; Se actualizó plan y teléfono.",
                usuario);
        }
        catch
        {
            // La auditoría no debe impedir que una actualización válida se complete.
        }
    }
}

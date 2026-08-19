using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class RegistrarAlumno
{
    private readonly IAlumnoRepository repository;

    public RegistrarAlumno(IAlumnoRepository repository)
        => this.repository = repository;

    public int Ejecutar(
        string nombre,
        string? telefono,
        string? cedula,
        string codigo,
        int planId,
        int actividadId,
        DateTime fechaInscripcion,
        DateTime fechaProximoPago)
    {
        if (fechaProximoPago.Date <= fechaInscripcion.Date)
            throw new ArgumentException(
                "La fecha del próximo pago debe ser posterior a la fecha de inscripción.");

        return repository.Crear(
            nombre,
            telefono,
            cedula,
            codigo,
            planId,
            actividadId,
            fechaInscripcion.Date,
            fechaProximoPago.Date);
    }
}

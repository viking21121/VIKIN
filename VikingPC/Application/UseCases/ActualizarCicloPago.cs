using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ActualizarCicloPago
{
    private readonly ICicloPagoRepository repository;
    private readonly IAuditoriaRepository? auditoriaRepository;

    public ActualizarCicloPago(
        ICicloPagoRepository repository,
        IAuditoriaRepository? auditoriaRepository = null)
    {
        this.repository = repository;
        this.auditoriaRepository = auditoriaRepository;
    }

    public void Ejecutar(
        int alumnoId,
        DateTime fechaInicio,
        DateTime fechaProximoPago,
        string usuario = "Sistema")
    {
        if (alumnoId <= 0)
            throw new ArgumentException("El alumno no es válido.");

        if (fechaProximoPago.Date <= fechaInicio.Date)
            throw new ArgumentException(
                "La fecha del próximo pago debe ser posterior a la fecha de inicio del ciclo.");

        repository.ConfigurarCiclo(
            alumnoId,
            fechaInicio.Date,
            fechaProximoPago.Date);

        try
        {
            auditoriaRepository?.Registrar(
                "Actualizar ciclo de pago",
                $"AlumnoId={alumnoId}; Inicio={fechaInicio:yyyy-MM-dd}; PróximoPago={fechaProximoPago:yyyy-MM-dd}.",
                usuario);
        }
        catch
        {
            // La auditoría no debe impedir una actualización válida.
        }
    }
}

using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class RenovarMembresia
{
    private readonly ICicloPagoRepository repository;

    public RenovarMembresia(ICicloPagoRepository repository)
        => this.repository = repository;

    public void Ejecutar(int alumnoId, DateTime fechaPago)
    {
        if (alumnoId <= 0)
            throw new ArgumentException("El alumno no es válido.");

        repository.RenovarSiCorresponde(alumnoId, fechaPago);
    }
}

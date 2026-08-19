using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class CrearRespaldoAutomatico
{
    private readonly IRespaldoRepository repository;
    public CrearRespaldoAutomatico(IRespaldoRepository repository) => this.repository = repository;
    public void Ejecutar() => repository.CrearAutomatico();
}

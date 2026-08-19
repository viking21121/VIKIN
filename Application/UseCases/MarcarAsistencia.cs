using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class MarcarAsistencia
{
    private readonly IAasistenciaRepository repository;
    public MarcarAsistencia(IAasistenciaRepository repository) => this.repository = repository;
    public void Ejecutar(int alumnoId, DateTime fecha, bool presente) => repository.Marcar(alumnoId, fecha, presente);
}

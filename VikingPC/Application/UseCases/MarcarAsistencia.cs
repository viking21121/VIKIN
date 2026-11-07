using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class MarcarAsistencia
{
    private readonly IAasistenciaRepository repository;
    public MarcarAsistencia(IAasistenciaRepository repository) => this.repository = repository;
    public void Ejecutar(int alumnoId, DateTime fecha, bool presente) => repository.Marcar(alumnoId, fecha, presente);

    /// <returns>true si la operación era nueva; false si ya estaba registrada.</returns>
    public bool EjecutarIdempotente(string idOperacion, int alumnoId, DateTime fecha, bool presente)
    {
        if (string.IsNullOrWhiteSpace(idOperacion))
            throw new ArgumentException("El identificador de operación es obligatorio.", nameof(idOperacion));

        return repository.MarcarIdempotente(idOperacion.Trim(), alumnoId, fecha, presente);
    }
}

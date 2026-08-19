using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class CargarAsistenciasDesdeArchivo
{
    private readonly IAasistenciaRepository repository;
    public CargarAsistenciasDesdeArchivo(IAasistenciaRepository repository) => this.repository = repository;
    public (int exito, int fallidos) Ejecutar(string rutaArchivo, DateTime fecha, int actividadId)
        => repository.CargarDesdeArchivo(rutaArchivo, fecha, actividadId);
}

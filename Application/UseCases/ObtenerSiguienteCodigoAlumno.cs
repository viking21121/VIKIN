using AsistenciaNatacion.Application.Ports;

namespace AsistenciaNatacion.Application.UseCases;

public class ObtenerSiguienteCodigoAlumno
{
    private readonly IAlumnoRepository repository;

    public ObtenerSiguienteCodigoAlumno(IAlumnoRepository repository)
        => this.repository = repository;

    public string EjecutarGlobal()
        => repository.SiguienteCodigoGlobal();
}

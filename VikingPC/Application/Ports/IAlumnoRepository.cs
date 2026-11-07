using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.Ports;

public interface IAlumnoRepository
{
    int Crear(
        string nombre,
        string? telefono,
        string? cedula,
        string codigo,
        int planId,
        int actividadId,
        DateTime fechaInscripcion,
        DateTime fechaProximoPago);
    string SiguienteCodigoGlobal();
    List<Alumno> ObtenerPorActividad(int actividadId);
    Alumno? ObtenerPorId(int alumnoId);
    void ActualizarPlanYTelefono(int alumnoId, int planId, string telefono);
    void Desactivar(int alumnoId);
}

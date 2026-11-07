using System;
using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.Mobile;

public sealed class RegistrarAsistenciaMovil : IRegistrarAsistenciaMovil
{
    private readonly ObtenerAlumnoPorCodigo obtenerAlumnoPorCodigo;
    private readonly MarcarAsistencia marcarAsistencia;
    private readonly ObtenerEstadoAlumno obtenerEstadoAlumno;
    private readonly ObtenerCarnetPorId obtenerCarnet;

    public static event Action<CarnetAlumno>? AsistenciaConCarnetRegistrada;

    public RegistrarAsistenciaMovil(
        ObtenerAlumnoPorCodigo obtenerAlumnoPorCodigo,
        MarcarAsistencia marcarAsistencia,
        ObtenerEstadoAlumno obtenerEstadoAlumno,
        ObtenerCarnetPorId? obtenerCarnet = null)
    {
        this.obtenerAlumnoPorCodigo = obtenerAlumnoPorCodigo;
        this.marcarAsistencia = marcarAsistencia;
        this.obtenerEstadoAlumno = obtenerEstadoAlumno;
        this.obtenerCarnet = obtenerCarnet ?? AppServices.CrearObtenerCarnetPorId();
    }

    public MobileAttendanceResponse Ejecutar(
        MobileAttendanceRequest request)
    {
        string codigo = request.CodigoAlumno?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(codigo))
        {
            return MobileAttendanceResponse.Fail(
                "No se recibió el código del alumno.");
        }

        int actividadId = request.ActividadId
            ?? ContextoActividad.ActividadId;

        if (actividadId <= 0)
        {
            return MobileAttendanceResponse.Fail(
                "No se ha especificado una actividad válida.");
        }

        DateTime fechaMarcado = request.FechaHora ?? DateTime.Today;

        var alumno = obtenerAlumnoPorCodigo
            .Ejecutar(codigo, actividadId);

        if (alumno == null)
        {
            var alumnoGlobal = obtenerAlumnoPorCodigo
                .EjecutarGlobal(codigo);

            if (alumnoGlobal != null)
            {
                return MobileAttendanceResponse.Fail(
                    $"El alumno {alumnoGlobal.Nombre} no pertenece a la actividad seleccionada.");
            }

            return MobileAttendanceResponse.Fail(
                $"No se encontró el alumno con código {codigo}.");
        }

        // Con identificador de operación, un reenvío del mismo escaneo
        // (por ejemplo, tras perderse la respuesta) no se vuelve a procesar.
        string idOperacion = request.IdOperacion?.Trim() ?? "";
        bool esNueva = true;

        if (string.IsNullOrEmpty(idOperacion))
        {
            marcarAsistencia.Ejecutar(
                alumno.Id,
                fechaMarcado,
                true);
        }
        else
        {
            esNueva = marcarAsistencia.EjecutarIdempotente(
                idOperacion,
                alumno.Id,
                fechaMarcado,
                true);
        }

        var estado = obtenerEstadoAlumno.Ejecutar(
            alumno.Id,
            fechaMarcado.Year,
            fechaMarcado.Month);

        // Evaluar carné con semáforo
        CarnetAlumno? carnet = null;
        try
        {
            carnet = obtenerCarnet.Ejecutar(alumno.Id);
            if (carnet != null && esNueva)
            {
                AsistenciaConCarnetRegistrada?.Invoke(carnet);
            }
        }
        catch (Exception ex)
        {
            AsistenciaNatacion.Infrastructure.Logging.Logger.Error(
                $"No se pudo preparar el carnet del alumno {alumno.Id}.", ex);
        }

        return MobileAttendanceResponse.Success(
            esNueva
                ? $"Asistencia registrada correctamente para {alumno.Nombre}."
                : $"La asistencia de {alumno.Nombre} ya estaba registrada.",
            alumno.Nombre,
            alumno.Id,
            estado?.PlanNombre,
            estado?.FechaVencimientoPlan,
            carnet?.EstadoSemaforo.ToString().ToLowerInvariant() ?? "verde",
            carnet?.TituloEstado ?? "SOLVENTE",
            carnet?.SubtituloEstado ?? "",
            carnet?.TieneFoto ?? false);
    }
}
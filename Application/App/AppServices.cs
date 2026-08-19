using System.IO;
﻿using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Application.Ports;

using AsistenciaNatacion.Infrastructure.SQLite;

namespace AsistenciaNatacion;

public static class AppServices
{
    public static RegistrarPago CrearRegistrarPago()
    {
        var repository = new SqlitePagoRepository(
            Database.ObtenerConnectionString());

        var auditoriaRepository =
            new SqliteAuditoriaRepository(
                Database.ObtenerConnectionString());

        ICicloPagoRepository cicloPagoRepository =
            new SqliteCicloPagoRepository(
                Database.ObtenerConnectionString());

        return new RegistrarPago(
            repository,
            auditoriaRepository,
            cicloPagoRepository);
    }

    public static ObtenerPagosAlumnoPeriodo CrearObtenerPagosAlumnoPeriodo()
    {
        var repository = new SqlitePagoRepository(
            Database.ObtenerConnectionString());

        return new ObtenerPagosAlumnoPeriodo(repository);
    }

    public static ObtenerPagoPorFolio CrearObtenerPagoPorFolio()
    {
        var repository = new SqlitePagoRepository(
            Database.ObtenerConnectionString());

        return new ObtenerPagoPorFolio(repository);
    }

    public static ObtenerConfiguracionOperador CrearObtenerConfiguracionOperador()
    {
        var repository = new SqliteConfiguracionOperadorRepository(
            Database.ObtenerConnectionString());

        return new ObtenerConfiguracionOperador(repository);
    }

    public static GuardarConfiguracionOperador CrearGuardarConfiguracionOperador()
    {
        var repository = new SqliteConfiguracionOperadorRepository(
            Database.ObtenerConnectionString());

        return new GuardarConfiguracionOperador(repository);
    }

    public static ObtenerConfiguracion CrearObtenerConfiguracion()
    {
        var repository = new SqliteConfiguracionRepository(
            Database.ObtenerConnectionString());

        return new ObtenerConfiguracion(repository);
    }

    public static GuardarConfiguracion CrearGuardarConfiguracion()
    {
        var repository = new SqliteConfiguracionRepository(
            Database.ObtenerConnectionString());

        return new GuardarConfiguracion(repository);
    }

    public static ObtenerActividades CrearObtenerActividades()
    {
        var repository = new SqliteActividadRepository(
            Database.ObtenerConnectionString());

        return new ObtenerActividades(repository);
    }

    public static CrearActividad CrearCrearActividad()
    {
        var repository = new SqliteActividadRepository(
            Database.ObtenerConnectionString());

        return new CrearActividad(repository);
    }

    public static ObtenerMetricasRapidas CrearObtenerMetricasRapidas()
    {
        var repository = new SqliteMetricasRepository(
            Database.ObtenerConnectionString());

        return new ObtenerMetricasRapidas(repository);
    }

    public static ObtenerPlanesPorActividad CrearObtenerPlanesPorActividad()
    {
        var repository = new SqlitePlanActividadRepository(
            Database.ObtenerConnectionString());

        return new ObtenerPlanesPorActividad(repository);
    }

    public static CrearPlanActividad CrearCrearPlanActividad()
    {
        var repository = new SqlitePlanActividadRepository(
            Database.ObtenerConnectionString());

        return new CrearPlanActividad(repository);
    }

    public static ActualizarPlanActividad CrearActualizarPlanActividad()
    {
        var repository = new SqlitePlanActividadRepository(
            Database.ObtenerConnectionString());

        return new ActualizarPlanActividad(repository);
    }

    public static ObtenerOpcionesPlanActividad CrearObtenerOpcionesPlanActividad()
    {
        var repository = new SqliteOpcionPlanActividadRepository(Database.ObtenerConnectionString());
        return new ObtenerOpcionesPlanActividad(repository);
    }

    public static PrepararCatalogoPlanActividad CrearPrepararCatalogoPlanActividad()
    {
        var repository = new SqliteOpcionPlanActividadRepository(Database.ObtenerConnectionString());
        return new PrepararCatalogoPlanActividad(repository);
    }

    public static CrearOpcionPlanActividad CrearCrearOpcionPlanActividad()
    {
        var repository = new SqliteOpcionPlanActividadRepository(Database.ObtenerConnectionString());
        return new CrearOpcionPlanActividad(repository);
    }

    public static ActualizarOpcionPlanActividad CrearActualizarOpcionPlanActividad()
    {
        var repository = new SqliteOpcionPlanActividadRepository(Database.ObtenerConnectionString());
        return new ActualizarOpcionPlanActividad(repository);
    }

    public static DesactivarOpcionPlanActividad CrearDesactivarOpcionPlanActividad()
    {
        var repository = new SqliteOpcionPlanActividadRepository(Database.ObtenerConnectionString());
        return new DesactivarOpcionPlanActividad(repository);
    }

    public static DesactivarPlanActividad CrearDesactivarPlanActividad()
    {
        var repository = new SqlitePlanActividadRepository(
            Database.ObtenerConnectionString());

        return new DesactivarPlanActividad(repository);
    }

    public static ObtenerAlumnosPorActividad CrearObtenerAlumnosPorActividad()
    {
        var repository = new SqliteAlumnoRepository(
            Database.ObtenerConnectionString());

        return new ObtenerAlumnosPorActividad(repository);
    }

    public static ObtenerAlumno CrearObtenerAlumno()
    {
        var repository = new SqliteAlumnoRepository(Database.ObtenerConnectionString());
        return new ObtenerAlumno(repository);
    }

    public static RegistrarAlumno CrearRegistrarAlumno()
    {
        var repository = new SqliteAlumnoRepository(Database.ObtenerConnectionString());
        return new RegistrarAlumno(repository);
    }

    public static ActualizarCicloPago CrearActualizarCicloPago()
    {
        var repository =
            new SqliteCicloPagoRepository(Database.ObtenerConnectionString());

        var auditoriaRepository =
            new SqliteAuditoriaRepository(Database.ObtenerConnectionString());

        return new ActualizarCicloPago(
            repository,
            auditoriaRepository);
    }

    public static ObtenerSiguienteCodigoAlumno CrearObtenerSiguienteCodigoAlumno()
    {
        var repository = new SqliteAlumnoRepository(Database.ObtenerConnectionString());
        return new ObtenerSiguienteCodigoAlumno(repository);
    }

    public static ObtenerAlumnoPorCodigo CrearObtenerAlumnoPorCodigo()
    {
        var repository = new SqliteAsistenciaRepository(Database.ObtenerConnectionString());
        return new ObtenerAlumnoPorCodigo(repository);
    }

    public static MarcarAsistencia CrearMarcarAsistencia()
    {
        return new MarcarAsistencia(new SqliteAsistenciaRepository(Database.ObtenerConnectionString()));
    }

    public static ObtenerEstadoAlumno CrearObtenerEstadoAlumno()
    {
        return new ObtenerEstadoAlumno(new SqliteEstadoAlumnoRepository(Database.ObtenerConnectionString()));
    }

    public static ObtenerDashboard CrearObtenerDashboard()
    {
        return new ObtenerDashboard(new SqliteEstadoAlumnoRepository(Database.ObtenerConnectionString()));
    }

    public static ObtenerIdsAsistieronHoy CrearObtenerIdsAsistieronHoy()
    {
        return new ObtenerIdsAsistieronHoy(new SqliteAsistenciaRepository(Database.ObtenerConnectionString()));
    }

    public static CargarAsistenciasDesdeArchivo CrearCargarAsistenciasDesdeArchivo()
    {
        return new CargarAsistenciasDesdeArchivo(new SqliteAsistenciaRepository(Database.ObtenerConnectionString()));
    }

    public static CrearRespaldoAutomatico CrearCrearRespaldoAutomatico()
    {
        return new CrearRespaldoAutomatico(new SqliteRespaldoRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "asistencia_natacion.db")));
    }

    public static ObtenerPrecioPlan CrearObtenerPrecioPlan() => new ObtenerPrecioPlan(new SqlitePrecioPlanRepository(Database.ObtenerConnectionString()));
    public static ActualizarPrecioPlan CrearActualizarPrecioPlan() => new ActualizarPrecioPlan(new SqlitePrecioPlanRepository(Database.ObtenerConnectionString()));
    public static ExisteReferenciaPago CrearExisteReferenciaPago() => new ExisteReferenciaPago(new SqlitePagoRepository(Database.ObtenerConnectionString()));
    public static ObtenerAuditoria CrearObtenerAuditoria() => new ObtenerAuditoria(new SqliteAuditoriaConsultaRepository(Database.ObtenerConnectionString()));
    public static ObtenerPlanActividadPorId CrearObtenerPlanActividadPorId() => new ObtenerPlanActividadPorId(new SqlitePlanActividadRepository(Database.ObtenerConnectionString()));

    public static DesactivarAlumno CrearDesactivarAlumno()
    {
        var repository = new SqliteAlumnoRepository(Database.ObtenerConnectionString());
        var auditoriaRepository = new SqliteAuditoriaRepository(Database.ObtenerConnectionString());
        return new DesactivarAlumno(repository, auditoriaRepository);
    }

    public static ActualizarAlumno CrearActualizarAlumno()
    {
        var repository = new SqliteAlumnoRepository(Database.ObtenerConnectionString());
        var auditoriaRepository = new SqliteAuditoriaRepository(Database.ObtenerConnectionString());
        return new ActualizarAlumno(repository, auditoriaRepository);
    }

    public static DesactivarActividad CrearDesactivarActividad()
    {
        var repository = new SqliteActividadRepository(
            Database.ObtenerConnectionString());

        return new DesactivarActividad(repository);
    }

}
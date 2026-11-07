using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Infrastructure.SQLite
{
    public class SqliteEstadoAlumnoRepository : IEstadoAlumnoRepository
    {
        private readonly string connectionString;

        public SqliteEstadoAlumnoRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public EstadoAlumnoMes? ObtenerEstado(
            int alumnoId,
            int anio,
            int mes)
        {
            var alumnoRepo =
                new SqliteAlumnoRepository(connectionString);

            var alumno =
                alumnoRepo.ObtenerPorId(alumnoId);

            if (alumno == null)
                return null;

            var plan =
                new SqlitePlanActividadRepository(connectionString)
                    .ObtenerPorId(alumno.PlanId);

            int corresponden =
                plan?.ClasesMes
                ?? (alumno.ClasesMes > 0
                    ? alumno.ClasesMes
                    : 0);

            bool controlaClases = corresponden > 0;

            var asistenciaRepo =
                new SqliteAsistenciaRepository(connectionString);

            int tomadas = asistenciaRepo.ContarMes(alumnoId, anio, mes);

            DateTime fechaInscripcion =
                DateTime.TryParse(
                    alumno.FechaInscripcion,
                    out var inscripcion)
                    ? inscripcion.Date
                    : DateTime.Today;

            DateTime fechaInicioCiclo =
                alumno.FechaInicioPlan?.Date
                ?? fechaInscripcion;

            DateTime fechaProximoPago =
                alumno.FechaVencimientoPlan?.Date
                ?? fechaInicioCiclo.AddMonths(1);

            decimal abonado =
                ObtenerAbonadoEstado(
                    alumnoId,
                    fechaInicioCiclo,
                    fechaProximoPago);

            decimal cuota = 0m;

            // Proyección mensual de ciclos pagados y avance de vencimiento
            while (true)
            {
                decimal cuotaMes = ObtenerPrecio(alumno.PlanId, fechaInicioCiclo.Year, fechaInicioCiclo.Month);

                if (cuotaMes > 0 && abonado >= cuotaMes && fechaProximoPago <= DateTime.Today)
                {
                    abonado -= cuotaMes;
                    fechaInicioCiclo = fechaProximoPago;
                    fechaProximoPago = fechaProximoPago.AddMonths(1);
                }
                else
                {
                    break;
                }
            }

            // Acumulación de cuotas adeudadas desde el ciclo inicial hasta la fecha actual
            DateTime iterInicio = fechaInicioCiclo;
            while (iterInicio <= DateTime.Today)
            {
                cuota += ObtenerPrecio(alumno.PlanId, iterInicio.Year, iterInicio.Month);
                iterInicio = iterInicio.AddMonths(1);
            }

            if (cuota == 0)
            {
                cuota = ObtenerPrecio(alumno.PlanId, fechaInicioCiclo.Year, fechaInicioCiclo.Month);
            }

            int diasRestantes =
                (fechaProximoPago - DateTime.Today).Days;

            int restantes = int.MaxValue;

            var estado = EstadoMes.EnProgreso;

            if (controlaClases)
            {
                restantes =
                    Math.Max(corresponden - tomadas, 0);

                if (tomadas > corresponden)
                {
                    estado = EstadoMes.Excedido;
                }
                else if (tomadas == corresponden)
                {
                    estado = EstadoMes.Completo;
                }
            }

            return new EstadoAlumnoMes
            {
                AlumnoId = alumnoId,
                Nombre = alumno.Nombre,
                Codigo = alumno.Codigo,
                Cedula = alumno.Cedula,
                Telefono = alumno.Telefono,
                FotoRuta = alumno.FotoRuta,

                PlanVecesSemana = alumno.PlanVecesSemana,
                PlanId = alumno.PlanId,

                PlanNombre =
                    plan?.DescripcionCompleta
                    ?? (alumno.PlanVecesSemana > 0
                        ? $"{alumno.PlanVecesSemana}x/semana"
                        : "Sin plan"),

                ControlaClases = controlaClases,
                ClasesMes = corresponden,
                Tomadas = tomadas,
                Restantes = restantes,
                Estado = estado,

                EsMembresia = !controlaClases,

                FechaInicioPlan = fechaInicioCiclo,
                FechaVencimientoPlan = fechaProximoPago,
                DiasRestantes = diasRestantes,

                CuotaTotal = cuota,
                MontoAbonado = abonado
            };
        }

        public List<EstadoAlumnoMes> ObtenerDashboard(
            int anio,
            int mes,
            int actividadId)
        {
            var resultado =
                new List<EstadoAlumnoMes>();

            var alumnos =
                new SqliteAlumnoRepository(connectionString)
                    .ObtenerPorActividad(actividadId);

            foreach (var alumno in alumnos)
            {
                if (DateTime.TryParse(
                        alumno.FechaInscripcion,
                        out var inscripcion)
                    && (anio * 100 + mes)
                        < (inscripcion.Year * 100 + inscripcion.Month))
                {
                    continue;
                }

                var estado =
                    ObtenerEstado(
                        alumno.Id,
                        anio,
                        mes);

                if (estado != null)
                    resultado.Add(estado);
            }

            return resultado;
        }

        private decimal ObtenerAbonadoCiclo(
            int alumnoId,
            DateTime fechaInicio,
            DateTime fechaProximoPago)
        {
            DateTime limiteSuperior =
                fechaProximoPago > DateTime.Today
                    ? fechaProximoPago
                    : DateTime.Today;

            return EjecutarDecimal(
                @"
SELECT COALESCE(SUM(monto), 0)
FROM pagos
WHERE alumno_id = $id
  AND date(fecha_pago) >= date($inicio)
  AND date(fecha_pago) <= date($fin);",
                ("$id", alumnoId),
                ("$inicio", fechaInicio.ToString("yyyy-MM-dd")),
                ("$fin", limiteSuperior.ToString("yyyy-MM-dd")));
        }

        private decimal ObtenerAbonadoEstado(
            int alumnoId,
            DateTime fechaInicioCiclo,
            DateTime fechaProximoPago)
        {
            return ObtenerAbonadoCiclo(
                alumnoId,
                fechaInicioCiclo,
                fechaProximoPago);
        }

        private decimal ObtenerPrecio(
            int planId,
            int anio,
            int mes)
        {
            using var conn =
                new SqliteConnection(connectionString);

            conn.Open();

            int periodo =
                anio * 100 + mes;

            using var historial =
                conn.CreateCommand();

            historial.CommandText = @"
SELECT precio
FROM planes_actividad_precio_historial
WHERE plan_id = $id
  AND (anio * 100 + mes) <= $periodo
ORDER BY anio DESC, mes DESC
LIMIT 1;";

            historial.Parameters.AddWithValue(
                "$id",
                planId);

            historial.Parameters.AddWithValue(
                "$periodo",
                periodo);

            var valor =
                historial.ExecuteScalar();

            if (valor != null &&
                valor != DBNull.Value)
            {
                return Convert.ToDecimal(valor);
            }

            using var actual =
                conn.CreateCommand();

            actual.CommandText = @"
SELECT COALESCE(precio, 0)
FROM planes_actividad
WHERE id = $id;";

            actual.Parameters.AddWithValue(
                "$id",
                planId);

            return Convert.ToDecimal(
                actual.ExecuteScalar() ?? 0m);
        }

        private decimal EjecutarDecimal(
            string sql,
            params (string name, object value)[] parametros)
        {
            using var conn =
                new SqliteConnection(connectionString);

            conn.Open();

            using var cmd =
                conn.CreateCommand();

            cmd.CommandText = sql;

            foreach (var parametro in parametros)
            {
                cmd.Parameters.AddWithValue(
                    parametro.name,
                    parametro.value);
            }

            return Convert.ToDecimal(
                cmd.ExecuteScalar() ?? 0m);
        }
    }
}
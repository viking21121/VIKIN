using AsistenciaNatacion.Application.Ports;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.SQLite
{
    public class SqliteMetricasRepository : IMetricasRepository
    {
        private readonly string connectionString;

        public SqliteMetricasRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public MetricasResumen ObtenerMetricas(
            int anio,
            int mes,
            int actividadId)
        {
            var metricas = new MetricasResumen();

            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            // 1. Alumnos activos de la actividad
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*)
                    FROM alumnos
                    WHERE activo = 1
                      AND actividad_id = $actividadId;
                ";

                cmd.Parameters.AddWithValue("$actividadId", actividadId);

                metricas.AlumnosActivos =
                    Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 2. Asistencias de hoy de la actividad
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*)
                    FROM asistencias a
                    INNER JOIN alumnos al
                        ON al.id = a.alumno_id
                    WHERE a.fecha = $hoy
                      AND a.presente = 1
                      AND al.actividad_id = $actividadId;
                ";

                cmd.Parameters.AddWithValue(
                    "$hoy",
                    DateTime.Today.ToString("yyyy-MM-dd"));

                cmd.Parameters.AddWithValue("$actividadId", actividadId);

                metricas.AsistenciasHoy =
                    Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 3. Planes por vencer
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*)
                    FROM alumnos al
                    WHERE al.activo = 1
                      AND al.actividad_id = $actividadId
                      AND al.clases_mes > 0
                      AND (
                          al.clases_mes -
                          (
                              SELECT COUNT(*)
                              FROM asistencias a
                              WHERE a.alumno_id = al.id
                                AND a.fecha LIKE $prefijo
                                AND a.presente = 1
                          )
                      ) <= 1;
                ";

                cmd.Parameters.AddWithValue(
                    "$prefijo",
                    $"{anio:D4}-{mes:D2}%");

                cmd.Parameters.AddWithValue("$actividadId", actividadId);

                metricas.PlanesPorVencer =
                    Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 4. Recaudado del mes de la actividad
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COALESCE(SUM(p.monto), 0)
                    FROM pagos p
                    INNER JOIN alumnos al
                        ON al.id = p.alumno_id
                    WHERE p.anio = $anio
                      AND p.mes = $mes
                      AND al.actividad_id = $actividadId;
                ";

                cmd.Parameters.AddWithValue("$anio", anio);
                cmd.Parameters.AddWithValue("$mes", mes);
                cmd.Parameters.AddWithValue("$actividadId", actividadId);

                metricas.RecaudadoMes =
                    Convert.ToDecimal(cmd.ExecuteScalar());
            }

            // 5. Caja del día de la actividad actual
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COALESCE(SUM(p.monto), 0)
                    FROM pagos p
                    INNER JOIN alumnos al ON al.id = p.alumno_id
                    WHERE datetime(p.fecha_pago) >= datetime($inicio)
                      AND datetime(p.fecha_pago) < datetime($fin)
                      AND al.actividad_id = $actividadId;
                ";

                cmd.Parameters.AddWithValue("$inicio", DateTime.Today.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$fin", DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$actividadId", actividadId);

                metricas.RecaudadoHoy = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            return metricas;
        }
    }
}
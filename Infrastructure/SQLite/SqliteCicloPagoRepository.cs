using AsistenciaNatacion.Application.Ports;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.SQLite;

public class SqliteCicloPagoRepository : ICicloPagoRepository
{
    private readonly string connectionString;

    public SqliteCicloPagoRepository(string connectionString)
        => this.connectionString = connectionString;

    public void ConfigurarCiclo(
        int alumnoId,
        DateTime fechaInicio,
        DateTime fechaProximoPago)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
UPDATE alumnos
SET fecha_inicio_plan = $fechaInicio,
    fecha_vencimiento_plan = $fechaProximoPago
WHERE id = $alumnoId
  AND activo = 1;";

        cmd.Parameters.AddWithValue(
            "$alumnoId",
            alumnoId);

        cmd.Parameters.AddWithValue(
            "$fechaInicio",
            fechaInicio.Date.ToString("yyyy-MM-dd"));

        cmd.Parameters.AddWithValue(
            "$fechaProximoPago",
            fechaProximoPago.Date.ToString("yyyy-MM-dd"));

        if (cmd.ExecuteNonQuery() == 0)
        {
            throw new ArgumentException(
                "No fue posible actualizar el ciclo de pago del alumno.");
        }
    }

    public void RenovarSiCorresponde(
        int alumnoId,
        DateTime fechaPago)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        int planId;
        DateTime? fechaInicio;
        DateTime? fechaProximoPago;

        // ------------------------------------------------------------
        // 1. Obtener ciclo actual del alumno
        // ------------------------------------------------------------

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT
    plan_id,
    fecha_inicio_plan,
    fecha_vencimiento_plan
FROM alumnos
WHERE id = $alumnoId
  AND activo = 1
LIMIT 1;";

            cmd.Parameters.AddWithValue(
                "$alumnoId",
                alumnoId);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return;

            planId =
                reader.GetInt32(
                    reader.GetOrdinal("plan_id"));

            fechaInicio =
                LeerFecha(
                    reader,
                    "fecha_inicio_plan");

            fechaProximoPago =
                LeerFecha(
                    reader,
                    "fecha_vencimiento_plan");
        }

        if (!fechaInicio.HasValue ||
            !fechaProximoPago.HasValue)
        {
            return;
        }

        // ------------------------------------------------------------
        // 2. Obtener la cuota correspondiente al ciclo
        // ------------------------------------------------------------

        decimal cuota =
            ObtenerPrecioCiclo(
                conn,
                planId,
                fechaInicio.Value);

        if (cuota <= 0)
            return;

        // ------------------------------------------------------------
        // 3. Calcular cuánto ha pagado dentro del ciclo
        // ------------------------------------------------------------

        decimal acumulado;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT COALESCE(SUM(monto), 0)
FROM pagos
WHERE alumno_id = $alumnoId
  AND date(fecha_pago) >= date($fechaInicio)
  AND date(fecha_pago) <= date($fechaPago);";

            cmd.Parameters.AddWithValue(
                "$alumnoId",
                alumnoId);

            cmd.Parameters.AddWithValue(
                "$fechaInicio",
                fechaInicio.Value
                    .Date
                    .ToString("yyyy-MM-dd"));

            cmd.Parameters.AddWithValue(
                "$fechaPago",
                fechaPago.Date
                    .ToString("yyyy-MM-dd"));

            acumulado =
                Convert.ToDecimal(
                    cmd.ExecuteScalar() ?? 0m);
        }

        // Todavía no ha completado la cuota.
        if (acumulado < cuota)
            return;

        // ------------------------------------------------------------
        // 4. Renovar el ciclo
        // ------------------------------------------------------------
        //
        // La fecha de pago NO determina el próximo vencimiento.
        //
        // Ejemplo:
        //
        // Inicio:       18/07/2026
        // Vencimiento:  18/08/2026
        // Pago tardío:  17/09/2026
        //
        // Resultado:
        //
        // Nuevo inicio:       18/08/2026
        // Nuevo vencimiento:  18/09/2026
        //
        // Se conserva el día original del ciclo.
        // ------------------------------------------------------------

        DateTime nuevaFechaInicio =
            fechaProximoPago.Value.Date;

        DateTime nuevaFechaProximoPago =
            fechaProximoPago.Value.Date.AddMonths(1);

        using var update = conn.CreateCommand();

        update.CommandText = @"
UPDATE alumnos
SET fecha_inicio_plan = $fechaInicio,
    fecha_vencimiento_plan = $fechaProximoPago
WHERE id = $alumnoId
  AND activo = 1;";

        update.Parameters.AddWithValue(
            "$alumnoId",
            alumnoId);

        update.Parameters.AddWithValue(
            "$fechaInicio",
            nuevaFechaInicio.ToString("yyyy-MM-dd"));

        update.Parameters.AddWithValue(
            "$fechaProximoPago",
            nuevaFechaProximoPago.ToString("yyyy-MM-dd"));

        update.ExecuteNonQuery();
    }

    private static decimal ObtenerPrecioCiclo(
        SqliteConnection conn,
        int planId,
        DateTime fechaInicio)
    {
        int periodo =
            fechaInicio.Year * 100 +
            fechaInicio.Month;

        using (var historial = conn.CreateCommand())
        {
            historial.CommandText = @"
SELECT precio
FROM planes_actividad_precio_historial
WHERE plan_id = $planId
  AND (anio * 100 + mes) <= $periodo
ORDER BY anio DESC, mes DESC
LIMIT 1;";

            historial.Parameters.AddWithValue(
                "$planId",
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
        }

        using var actual = conn.CreateCommand();

        actual.CommandText = @"
SELECT COALESCE(precio, 0)
FROM planes_actividad
WHERE id = $planId;";

        actual.Parameters.AddWithValue(
            "$planId",
            planId);

        return Convert.ToDecimal(
            actual.ExecuteScalar() ?? 0m);
    }

    private static DateTime? LeerFecha(
        SqliteDataReader reader,
        string nombre)
    {
        int ordinal =
            reader.GetOrdinal(nombre);

        if (reader.IsDBNull(ordinal))
            return null;

        return DateTime.TryParse(
            reader.GetString(ordinal),
            out var fecha)
            ? fecha
            : null;
    }
}
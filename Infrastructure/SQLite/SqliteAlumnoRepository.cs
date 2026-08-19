using System.Globalization;
using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.SQLite;

public class SqliteAlumnoRepository : IAlumnoRepository
{
    private readonly string connectionString;

    public SqliteAlumnoRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }


    public int Crear(
        string nombre,
        string? telefono,
        string? cedula,
        string codigo,
        int planId,
        int actividadId,
        DateTime fechaInscripcion,
        DateTime fechaProximoPago)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es obligatorio.");

        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código es obligatorio.");

        if (fechaProximoPago.Date <= fechaInscripcion.Date)
            throw new ArgumentException(
                "La fecha del próximo pago debe ser posterior a la fecha de inscripción.");

        var plan = new SqlitePlanActividadRepository(connectionString).ObtenerPorId(planId);

        if (plan == null || plan.ActividadId != actividadId || !plan.Activo)
            throw new ArgumentException(
                "El plan seleccionado no es válido para esta actividad.");

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
INSERT INTO alumnos
(
    nombre,
    telefono,
    cedula,
    codigo,
    fecha_inscripcion,
    plan_veces_semana,
    clases_mes,
    activo,
    actividad_id,
    plan_id,
    fecha_inicio_plan,
    fecha_vencimiento_plan
)
VALUES
(
    $nombre,
    $telefono,
    $cedula,
    $codigo,
    $fechaInscripcion,
    $frecuencia,
    $clases,
    1,
    $actividadId,
    $planId,
    $fechaInicioCiclo,
    $fechaProximoPago
);

SELECT last_insert_rowid();";

        cmd.Parameters.AddWithValue("$nombre", nombre.Trim());
        cmd.Parameters.AddWithValue("$telefono", (object?)telefono ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cedula", (object?)cedula ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$codigo", codigo.Trim());
        cmd.Parameters.AddWithValue("$fechaInscripcion", fechaInscripcion.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$frecuencia", plan.FrecuenciaSemanal ?? 0);
        cmd.Parameters.AddWithValue("$clases", plan.ClasesMes ?? 0);
        cmd.Parameters.AddWithValue("$actividadId", actividadId);
        cmd.Parameters.AddWithValue("$planId", planId);
        cmd.Parameters.AddWithValue("$fechaInicioCiclo", fechaInscripcion.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$fechaProximoPago", fechaProximoPago.Date.ToString("yyyy-MM-dd"));

        try
        {
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new ArgumentException(
                $"Ya existe un alumno con el código \"{codigo}\".");
        }
    }

    public string SiguienteCodigoGlobal()
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT codigo
            FROM alumnos
            WHERE codigo IS NOT NULL
              AND TRIM(codigo) <> ''
              AND UPPER(codigo) LIKE 'A%';";

        int maximo = 0;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var codigo = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();

            if (codigo.Length <= 1 ||
                !codigo.StartsWith("A", StringComparison.OrdinalIgnoreCase))
                continue;

            if (int.TryParse(
                    codigo[1..],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var numero))
            {
                maximo = Math.Max(maximo, numero);
            }
        }

        return $"A{maximo + 1:000}";
    }

    public void ActualizarPlanYTelefono(int alumnoId, int planId, string telefono)
    {
        if (alumnoId <= 0) throw new ArgumentException("El alumno no es válido.");
        if (planId <= 0) throw new ArgumentException("El plan no es válido.");

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE alumnos
            SET plan_id = $planId,
                telefono = $telefono
            WHERE id = $alumnoId AND activo = 1;
        ";
        cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
        cmd.Parameters.AddWithValue("$planId", planId);
        cmd.Parameters.AddWithValue("$telefono", telefono ?? string.Empty);

        if (cmd.ExecuteNonQuery() == 0)
            throw new ArgumentException("No fue posible actualizar el alumno. Verifica que siga activo.");
    }

    public void Desactivar(int alumnoId)
    {
        if (alumnoId <= 0) throw new ArgumentException("El alumno no es válido.");

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE alumnos SET activo = 0 WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", alumnoId);
        cmd.ExecuteNonQuery();
    }

    public List<Alumno> ObtenerPorActividad(int actividadId)
    {
        var alumnos = new List<Alumno>();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            SELECT *
            FROM alumnos
            WHERE activo = 1
              AND actividad_id = $actividadId
            ORDER BY nombre;
        ";

        cmd.Parameters.AddWithValue("$actividadId", actividadId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            alumnos.Add(LeerAlumno(reader));

        return alumnos;
    }

    public Alumno? ObtenerPorId(int alumnoId)
    {
        if (alumnoId <= 0) return null;

        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT * FROM alumnos WHERE id = $id AND activo = 1 LIMIT 1;";
        cmd.Parameters.AddWithValue("$id", alumnoId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? LeerAlumno(reader) : null;
    }

    private static Alumno LeerAlumno(SqliteDataReader reader)
    {
        return new Alumno
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            ActividadId = reader.GetInt32(reader.GetOrdinal("actividad_id")),
            Nombre = reader.GetString(reader.GetOrdinal("nombre")),
            Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
            Cedula = reader.IsDBNull(reader.GetOrdinal("cedula")) ? null : reader.GetString(reader.GetOrdinal("cedula")),
            Codigo = reader.IsDBNull(reader.GetOrdinal("codigo")) ? "" : reader.GetString(reader.GetOrdinal("codigo")),
            FechaInscripcion = reader.GetString(reader.GetOrdinal("fecha_inscripcion")),
            PlanVecesSemana = reader.IsDBNull(reader.GetOrdinal("plan_veces_semana")) ? 0 : reader.GetInt32(reader.GetOrdinal("plan_veces_semana")),
            ClasesMes = reader.IsDBNull(reader.GetOrdinal("clases_mes")) ? 0 : reader.GetInt32(reader.GetOrdinal("clases_mes")),
            PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
            Activo = reader.GetInt32(reader.GetOrdinal("activo")) == 1
        };
    }
}
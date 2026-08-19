using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion;

public class SqliteConfiguracionRepository : IConfiguracionRepository
{
    private readonly string connectionString;

    public SqliteConfiguracionRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    private void AsegurarTabla()
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS configuracion_establecimiento_actividad (
                actividad_id INTEGER PRIMARY KEY,
                nombre TEXT NOT NULL,
                identificacion TEXT DEFAULT '',
                tipo_establecimiento TEXT NOT NULL
            );

            INSERT OR IGNORE INTO configuracion_establecimiento_actividad
            (
                actividad_id,
                nombre,
                identificacion,
                tipo_establecimiento
            )
            SELECT
                1,
                nombre,
                identificacion,
                tipo_establecimiento
            FROM configuracion_establecimiento
            WHERE id = 1;
        ";

        cmd.ExecuteNonQuery();
    }

    public ConfiguracionEstablecimiento Obtener(int actividadId)
    {
        if (actividadId <= 0)
            throw new ArgumentException(
                "La actividad no es válida.",
                nameof(actividadId));

        AsegurarTabla();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            SELECT
                nombre,
                identificacion,
                tipo_establecimiento
            FROM configuracion_establecimiento_actividad
            WHERE actividad_id = $actividadId;
        ";

        cmd.Parameters.AddWithValue("$actividadId", actividadId);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return new ConfiguracionEstablecimiento();
        }

        return new ConfiguracionEstablecimiento
        {
            Nombre = reader["nombre"]?.ToString() ?? "",
            Identificacion = reader["identificacion"]?.ToString() ?? "",
            TipoEstablecimiento =
                reader["tipo_establecimiento"]?.ToString() ?? ""
        };
    }

    public void Guardar(
        int actividadId,
        ConfiguracionEstablecimiento configuracion)
    {
        if (actividadId <= 0)
            throw new ArgumentException(
                "La actividad no es válida.",
                nameof(actividadId));

        AsegurarTabla();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO configuracion_establecimiento_actividad
            (
                actividad_id,
                nombre,
                identificacion,
                tipo_establecimiento
            )
            VALUES
            (
                $actividadId,
                $nombre,
                $identificacion,
                $tipo
            )
            ON CONFLICT(actividad_id) DO UPDATE SET
                nombre = excluded.nombre,
                identificacion = excluded.identificacion,
                tipo_establecimiento = excluded.tipo_establecimiento;
        ";

        cmd.Parameters.AddWithValue(
            "$actividadId",
            actividadId);

        cmd.Parameters.AddWithValue(
            "$nombre",
            configuracion.Nombre);

        cmd.Parameters.AddWithValue(
            "$identificacion",
            configuracion.Identificacion);

        cmd.Parameters.AddWithValue(
            "$tipo",
            configuracion.TipoEstablecimiento);

        cmd.ExecuteNonQuery();
    }
}

using AsistenciaNatacion.Application.App;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion;

public class SqliteConfiguracionOperadorRepository
    : IConfiguracionOperadorRepository
{
    private readonly string connectionString;

    public SqliteConfiguracionOperadorRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    private int ActividadIdActual =>
        ContextoActividad.ActividadId > 0
            ? ContextoActividad.ActividadId
            : 1;

    public ConfiguracionOperador Obtener(string rol)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            SELECT nombre
            FROM configuracion_operador_actividad_rol
            WHERE actividad_id = $actividadId
              AND rol = $rol;
        ";

        cmd.Parameters.AddWithValue("$actividadId", ActividadIdActual);
        cmd.Parameters.AddWithValue("$rol", rol.Trim());

        var resultado = cmd.ExecuteScalar();

        return new ConfiguracionOperador
        {
            Rol = rol.Trim(),
            Nombre = resultado == null || resultado == DBNull.Value
                ? ""
                : resultado.ToString() ?? ""
        };
    }

    public void Guardar(ConfiguracionOperador configuracion)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO configuracion_operador_actividad_rol
            (actividad_id, rol, nombre)
            VALUES ($actividadId, $rol, $nombre)
            ON CONFLICT(actividad_id, rol)
            DO UPDATE SET nombre = excluded.nombre;
        ";

        cmd.Parameters.AddWithValue("$actividadId", ActividadIdActual);
        cmd.Parameters.AddWithValue("$rol", configuracion.Rol.Trim());
        cmd.Parameters.AddWithValue("$nombre", configuracion.Nombre.Trim());

        cmd.ExecuteNonQuery();
    }
}

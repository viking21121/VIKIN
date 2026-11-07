using AsistenciaNatacion.Application.App;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion;

public class SqliteAuditoriaRepository : IAuditoriaRepository
{
    private readonly string connectionString;

    public SqliteAuditoriaRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public void Registrar(
        string accion,
        string detalle,
        string? usuario = null)
    {
        string op = string.IsNullOrWhiteSpace(usuario)
            ? SesionUsuario.OperadorActual
            : usuario;

        int actividadId = ContextoActividad.ActividadId > 0
            ? ContextoActividad.ActividadId
            : 1;

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO auditoria
            (
                fecha,
                usuario,
                accion,
                detalle,
                actividad_id
            )
            VALUES
            (
                $fecha,
                $usuario,
                $accion,
                $detalle,
                $actividadId
            );
        ";

        cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$usuario", op.Trim());
        cmd.Parameters.AddWithValue("$accion", accion.Trim());
        cmd.Parameters.AddWithValue("$detalle", detalle.Trim());
        cmd.Parameters.AddWithValue("$actividadId", actividadId);

        cmd.ExecuteNonQuery();
    }
}

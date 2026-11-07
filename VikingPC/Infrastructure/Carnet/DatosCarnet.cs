using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.Carnet;

/// <summary>Datos extra del carnet impreso (no afectan a pagos ni asistencias).</summary>
public sealed class DatosCarnet
{
    public string ContactoNombre { get; set; } = "";
    public string ContactoTelefono { get; set; } = "";
    public string TipoSangre { get; set; } = "";
    public string AlertasMedicas { get; set; } = "";
    public string NivelCategoria { get; set; } = "";
}

public static class DatosCarnetRepositorio
{
    public static DatosCarnet Obtener(int alumnoId)
    {
        using var conn = new SqliteConnection(Database.ObtenerConnectionString());
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT contacto_emergencia_nombre, contacto_emergencia_telefono,
                   tipo_sangre, alertas_medicas, nivel_categoria
            FROM alumnos WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", alumnoId);
        using var r = cmd.ExecuteReader();
        var d = new DatosCarnet();
        if (r.Read())
        {
            d.ContactoNombre = r.IsDBNull(0) ? "" : r.GetString(0);
            d.ContactoTelefono = r.IsDBNull(1) ? "" : r.GetString(1);
            d.TipoSangre = r.IsDBNull(2) ? "" : r.GetString(2);
            d.AlertasMedicas = r.IsDBNull(3) ? "" : r.GetString(3);
            d.NivelCategoria = r.IsDBNull(4) ? "" : r.GetString(4);
        }
        return d;
    }

    public static void Guardar(int alumnoId, DatosCarnet d)
    {
        using var conn = new SqliteConnection(Database.ObtenerConnectionString());
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE alumnos SET
                contacto_emergencia_nombre = $cn,
                contacto_emergencia_telefono = $ct,
                tipo_sangre = $ts,
                alertas_medicas = $am,
                nivel_categoria = $nc
            WHERE id = $id;";
        cmd.Parameters.AddWithValue("$cn", d.ContactoNombre.Trim());
        cmd.Parameters.AddWithValue("$ct", d.ContactoTelefono.Trim());
        cmd.Parameters.AddWithValue("$ts", d.TipoSangre.Trim());
        cmd.Parameters.AddWithValue("$am", d.AlertasMedicas.Trim());
        cmd.Parameters.AddWithValue("$nc", d.NivelCategoria.Trim());
        cmd.Parameters.AddWithValue("$id", alumnoId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Nombres de las actividades activas (disciplinas de la academia).</summary>
    public static List<string> Disciplinas()
    {
        var lista = new List<string>();
        using var conn = new SqliteConnection(Database.ObtenerConnectionString());
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT nombre FROM actividades WHERE activa = 1 ORDER BY id;";
        using var r = cmd.ExecuteReader();
        while (r.Read()) if (!r.IsDBNull(0)) lista.Add(r.GetString(0));
        return lista;
    }

    public static string NombreActividad(int actividadId)
    {
        using var conn = new SqliteConnection(Database.ObtenerConnectionString());
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT nombre FROM actividades WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", actividadId);
        return cmd.ExecuteScalar() as string ?? "";
    }

    public static string NombreAcademia(int actividadId)
    {
        using var conn = new SqliteConnection(Database.ObtenerConnectionString());
        conn.Open();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT nombre FROM configuracion_establecimiento_actividad WHERE actividad_id = $id;";
            cmd.Parameters.AddWithValue("$id", actividadId);
            if (cmd.ExecuteScalar() is string n && !string.IsNullOrWhiteSpace(n)) return n;
        }
        catch (Exception ex) { Logging.Logger.Warn("Nombre de academia (por actividad): " + ex.Message); }

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT nombre FROM configuracion_establecimiento WHERE id = 1;";
        return cmd2.ExecuteScalar() as string ?? "ACADEMIA";
    }

    public static (byte[]? Bytes, string ContentType) Logo(int actividadId)
    {
        using var conn = new SqliteConnection(Database.ObtenerConnectionString());
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT logo, content_type FROM logo_actividad WHERE actividad_id = $id;";
        cmd.Parameters.AddWithValue("$id", actividadId);
        using var r = cmd.ExecuteReader();
        if (!r.Read() || r.IsDBNull(0)) return (null, "image/png");
        string ct = r.IsDBNull(1) ? "image/png" : r.GetString(1);
        return ((byte[])r["logo"], ct);
    }
}

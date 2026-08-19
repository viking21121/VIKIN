using AsistenciaNatacion.Application.Ports;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace AsistenciaNatacion.Infrastructure.SQLite;

public class SqliteAsistenciaRepository : IAasistenciaRepository
{
    private readonly string connectionString;
    public SqliteAsistenciaRepository(string connectionString) => this.connectionString = connectionString;

    public void Marcar(int alumnoId, DateTime fecha, bool presente)
    {
        using var conn = new SqliteConnection(connectionString); conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO asistencias (alumno_id, fecha, presente) VALUES ($alumnoId,$fecha,$presente)
ON CONFLICT(alumno_id, fecha) DO UPDATE SET presente = excluded.presente;";
        cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
        cmd.Parameters.AddWithValue("$fecha", fecha.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$presente", presente ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public int ContarMes(int alumnoId, int anio, int mes)
    {
        using var conn = new SqliteConnection(connectionString); conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM asistencias WHERE alumno_id=$id AND fecha LIKE $prefijo AND presente=1";
        cmd.Parameters.AddWithValue("$id", alumnoId);
        cmd.Parameters.AddWithValue("$prefijo", $"{anio:D4}-{mes:D2}%");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public HashSet<int> ObtenerIdsAsistieronHoy(int actividadId)
    {
        var ids = new HashSet<int>();
        using var conn = new SqliteConnection(connectionString); conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT DISTINCT a.alumno_id FROM asistencias a INNER JOIN alumnos al ON al.id=a.alumno_id
WHERE a.fecha=$hoy AND a.presente=1 AND al.actividad_id=$actividadId;";
        cmd.Parameters.AddWithValue("$hoy", DateTime.Today.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$actividadId", actividadId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) ids.Add(reader.GetInt32(0));
        return ids;
    }

    public Alumno? ObtenerAlumnoPorCodigo(string codigo, int actividadId)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        using var conn = new SqliteConnection(connectionString); conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM alumnos WHERE codigo=$codigo AND activo=1 AND actividad_id=$actividadId LIMIT 1";
        cmd.Parameters.AddWithValue("$codigo", codigo.Trim()); cmd.Parameters.AddWithValue("$actividadId", actividadId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? LeerAlumno(reader) : null;
    }

    public (int exito, int fallidos) CargarDesdeArchivo(string rutaArchivo, DateTime fecha, int actividadId)
    {
        if (!File.Exists(rutaArchivo)) return (0,0);
        int exito=0, fallidos=0;
        foreach (var linea in File.ReadAllLines(rutaArchivo))
        {
            var codigo = linea.Replace("\"","").Replace(",","").Trim();
            if (string.IsNullOrWhiteSpace(codigo)) continue;
            var alumno = ObtenerAlumnoPorCodigo(codigo, actividadId);
            if (alumno == null) { fallidos++; continue; }
            Marcar(alumno.Id, fecha, true); exito++;
        }
        return (exito, fallidos);
    }

    private static Alumno LeerAlumno(SqliteDataReader r) => new()
    {
        Id=r.GetInt32(r.GetOrdinal("id")), ActividadId=r.GetInt32(r.GetOrdinal("actividad_id")),
        Nombre=r.GetString(r.GetOrdinal("nombre")),
        Telefono=r.IsDBNull(r.GetOrdinal("telefono"))?null:r.GetString(r.GetOrdinal("telefono")),
        Cedula=r.IsDBNull(r.GetOrdinal("cedula"))?null:r.GetString(r.GetOrdinal("cedula")),
        Codigo=r.IsDBNull(r.GetOrdinal("codigo"))?"":r.GetString(r.GetOrdinal("codigo")),
        FechaInscripcion=r.GetString(r.GetOrdinal("fecha_inscripcion")),
        PlanVecesSemana=r.IsDBNull(r.GetOrdinal("plan_veces_semana"))?0:r.GetInt32(r.GetOrdinal("plan_veces_semana")),
        ClasesMes=r.IsDBNull(r.GetOrdinal("clases_mes"))?0:r.GetInt32(r.GetOrdinal("clases_mes")),
        Activo=r.GetInt32(r.GetOrdinal("activo"))==1, PlanId=r.GetInt32(r.GetOrdinal("plan_id")),
        FechaInicioPlan=LeerFecha(r,"fecha_inicio_plan"), FechaVencimientoPlan=LeerFecha(r,"fecha_vencimiento_plan")
    };
    private static DateTime? LeerFecha(SqliteDataReader r,string nombre)
    {
        var o=r.GetOrdinal(nombre); if(r.IsDBNull(o)) return null;
        return DateTime.TryParse(r.GetString(o), out var f)?f:null;
    }
}

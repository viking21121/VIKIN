using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Infrastructure.SQLite;
using Microsoft.Data.Sqlite;

namespace Viking.Tests;

public class MarcarAsistenciaTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IdOperacionVacio_Lanza(string id)
    {
        var caso = new MarcarAsistencia(new AsistenciaRepositoryFake());

        Assert.Throws<ArgumentException>(() =>
            caso.EjecutarIdempotente(id, 1, new DateTime(2026, 11, 1), true));
    }

    [Fact]
    public void IdOperacion_SeEnviaSinEspacios_YDevuelveSiEraNueva()
    {
        var repo = new AsistenciaRepositoryFake { OperacionNueva = false };
        var caso = new MarcarAsistencia(repo);

        bool esNueva = caso.EjecutarIdempotente("  abc123  ", 4, new DateTime(2026, 11, 1), true);

        Assert.False(esNueva);
        Assert.Equal("abc123", Assert.Single(repo.Operaciones).IdOperacion);
    }
}

/// <summary>
/// Prueba el repositorio real contra un SQLite temporal: lo importante es que
/// reenviar la misma operación no duplique ni cambie nada.
/// </summary>
public sealed class SqliteAsistenciaIdempotenciaTests : IDisposable
{
    private readonly string ruta =
        Path.Combine(Path.GetTempPath(), $"viking_test_{Guid.NewGuid():N}.db");

    private string Conexion => $"Data Source={ruta}";

    public SqliteAsistenciaIdempotenciaTests()
    {
        using var conn = new SqliteConnection(Conexion);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE asistencias (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                alumno_id INTEGER NOT NULL,
                fecha TEXT NOT NULL,
                presente INTEGER NOT NULL DEFAULT 1,
                UNIQUE(alumno_id, fecha)
            );
            CREATE TABLE asistencias_movil_ops (
                id_operacion TEXT PRIMARY KEY,
                alumno_id INTEGER NOT NULL,
                fecha TEXT NOT NULL,
                recibido_en TEXT NOT NULL
            );";
        cmd.ExecuteNonQuery();
    }

    private long Contar(string tabla)
    {
        using var conn = new SqliteConnection(Conexion);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {tabla}";
        return (long)cmd.ExecuteScalar()!;
    }

    [Fact]
    public void PrimerEnvio_EsNuevo_YRegistraLaAsistencia()
    {
        var repo = new SqliteAsistenciaRepository(Conexion);

        bool esNueva = repo.MarcarIdempotente("op-1", 10, new DateTime(2026, 11, 1), true);

        Assert.True(esNueva);
        Assert.Equal(1, Contar("asistencias"));
        Assert.Equal(1, Contar("asistencias_movil_ops"));
    }

    [Fact]
    public void ReenvioDeLaMismaOperacion_SeIgnora()
    {
        var repo = new SqliteAsistenciaRepository(Conexion);
        var fecha = new DateTime(2026, 11, 1);

        Assert.True(repo.MarcarIdempotente("op-1", 10, fecha, true));
        Assert.False(repo.MarcarIdempotente("op-1", 10, fecha, true));
        Assert.False(repo.MarcarIdempotente("op-1", 10, fecha, true));

        Assert.Equal(1, Contar("asistencias"));
        Assert.Equal(1, Contar("asistencias_movil_ops"));
    }

    [Fact]
    public void DosEscaneosDistintos_DelMismoAlumnoYDia_NoDuplicanLaFilaDeAsistencia()
    {
        var repo = new SqliteAsistenciaRepository(Conexion);
        var fecha = new DateTime(2026, 11, 1);

        Assert.True(repo.MarcarIdempotente("op-1", 10, fecha, true));
        Assert.True(repo.MarcarIdempotente("op-2", 10, fecha, true));

        Assert.Equal(1, Contar("asistencias"));
        Assert.Equal(2, Contar("asistencias_movil_ops"));
    }

    [Fact]
    public void AlumnosDistintos_GeneranAsistenciasDistintas()
    {
        var repo = new SqliteAsistenciaRepository(Conexion);
        var fecha = new DateTime(2026, 11, 1);

        repo.MarcarIdempotente("op-1", 10, fecha, true);
        repo.MarcarIdempotente("op-2", 11, fecha, true);

        Assert.Equal(2, Contar("asistencias"));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try { File.Delete(ruta); } catch { }
    }
}

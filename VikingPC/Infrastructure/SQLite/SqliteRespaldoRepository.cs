using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Infrastructure.Logging;
using Microsoft.Data.Sqlite;
using System.IO;

namespace AsistenciaNatacion.Infrastructure.SQLite;

/// <summary>
/// Crea respaldos de la base de datos con la API de respaldo en línea de SQLite.
///
/// Con la base en modo WAL, copiar solo el archivo .db puede dejar fuera datos
/// que todavía están en el archivo -wal. La API de respaldo produce una copia
/// consistente aunque la base esté en uso.
/// </summary>
public class SqliteRespaldoRepository : IRespaldoRepository
{
    private const int MaximoRespaldos = 30;

    private readonly string dbPath;

    public SqliteRespaldoRepository(string dbPath) => this.dbPath = dbPath;

    public void CrearAutomatico()
    {
        try
        {
            if (!File.Exists(dbPath))
                return;

            var nombre = $"asistencia_natacion_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";

            // A) Respaldo portátil, junto al programa.
            Guardar(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups"), nombre);

            // B) Respaldo seguro, en Mis Documentos.
            Guardar(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Respaldos_Natacion"),
                nombre);
        }
        catch (Exception ex)
        {
            Logger.Error("No se pudo crear el respaldo automático.", ex);
        }
    }

    private void Guardar(string carpeta, string nombre)
    {
        try
        {
            Directory.CreateDirectory(carpeta);
            string destino = Path.Combine(carpeta, nombre);

            using (var origen = new SqliteConnection($"Data Source={dbPath}"))
            using (var copia = new SqliteConnection($"Data Source={destino}"))
            {
                origen.Open();
                copia.Open();
                origen.BackupDatabase(copia);
            }

            // Las conexiones de respaldo no deben dejar archivos bloqueados.
            SqliteConnection.ClearAllPools();

            var antiguos = new DirectoryInfo(carpeta)
                .GetFiles("asistencia_natacion_backup_*.db")
                .OrderByDescending(x => x.CreationTime)
                .Skip(MaximoRespaldos)
                .ToList();

            foreach (var archivo in antiguos)
                archivo.Delete();
        }
        catch (Exception ex)
        {
            Logger.Error($"No se pudo guardar el respaldo en '{carpeta}'.", ex);
        }
    }
}

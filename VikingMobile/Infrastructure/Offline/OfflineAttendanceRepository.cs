using Microsoft.Maui.Storage;
using System.Text.Json;
using VikingMobile.Diagnostics;

namespace VikingMobile.Infrastructure.Offline;

/// <summary>
/// Cola local de asistencias pendientes de enviar a Viking PC.
///
/// Protecciones para cortes de luz y fallos:
/// - Un candado serializa lecturas y escrituras (la interfaz y la
///   sincronización pueden tocar el archivo a la vez).
/// - Se escribe en un archivo temporal y luego se reemplaza el original,
///   así un corte a mitad de escritura no deja el archivo a medias.
/// - Si el archivo existe pero no se puede leer, se conserva una copia
///   (.corrupto-FECHA) en lugar de descartarlo y sobrescribirlo.
/// </summary>
public class OfflineAttendanceRepository
{
    private static readonly JsonSerializerOptions JsonEscritura = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _candado = new(1, 1);

    public OfflineAttendanceRepository()
    {
        _filePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "offline_attendance.json");
    }

    public async Task<List<OfflineAttendance>> ObtenerTodasAsync()
    {
        await _candado.WaitAsync();
        try
        {
            return await LeerAsync();
        }
        finally
        {
            _candado.Release();
        }
    }

    public async Task<bool> AgregarAsync(
        OfflineAttendance asistencia)
    {
        await _candado.WaitAsync();
        try
        {
            var asistencias = await LeerAsync();

            if (string.IsNullOrWhiteSpace(asistencia.IdOperacion))
                asistencia.IdOperacion = Guid.NewGuid().ToString("N");

            // Misma operación ya guardada (por ejemplo, un reintento).
            if (asistencias.Any(x => x.IdOperacion == asistencia.IdOperacion))
                return false;

            // Evitar duplicados no sincronizados para el mismo alumno, actividad y fecha
            bool yaExiste = asistencias.Any(x =>
                !x.Sincronizada &&
                x.CodigoAlumno.Equals(asistencia.CodigoAlumno, StringComparison.OrdinalIgnoreCase) &&
                x.ActividadId == asistencia.ActividadId &&
                x.FechaHora.Date == asistencia.FechaHora.Date);

            if (yaExiste)
                return false;

            asistencia.Id =
                asistencias.Count == 0
                    ? 1
                    : asistencias.Max(x => x.Id) + 1;

            asistencias.Add(asistencia);

            await GuardarAsync(asistencias);
            return true;
        }
        finally
        {
            _candado.Release();
        }
    }

    public async Task<List<OfflineAttendance>>
        ObtenerPendientesAsync()
    {
        var asistencias =
            await ObtenerTodasAsync();

        return asistencias
            .Where(x => !x.Sincronizada)
            .OrderBy(x => x.FechaHora)
            .ToList();
    }

    public async Task MarcarSincronizadaAsync(
        int id)
    {
        await _candado.WaitAsync();
        try
        {
            var asistencias = await LeerAsync();

            var asistencia =
                asistencias.FirstOrDefault(
                    x => x.Id == id);

            if (asistencia == null)
                return;

            asistencia.Sincronizada = true;
            asistencia.FechaSincronizacion =
                DateTime.Now;
            asistencia.UltimoError = null;

            await GuardarAsync(asistencias);
        }
        finally
        {
            _candado.Release();
        }
    }

    public async Task MarcarErrorAsync(
        int id,
        string error)
    {
        await _candado.WaitAsync();
        try
        {
            var asistencias = await LeerAsync();

            var asistencia =
                asistencias.FirstOrDefault(
                    x => x.Id == id);

            if (asistencia == null)
                return;

            asistencia.UltimoError = error;

            await GuardarAsync(asistencias);
        }
        finally
        {
            _candado.Release();
        }
    }

    public async Task<int> ContarPendientesAsync()
    {
        var pendientes =
            await ObtenerPendientesAsync();

        return pendientes.Count;
    }

    // ------------------------------------------------------------------
    // Acceso al archivo. Solo se llama con el candado tomado.
    // ------------------------------------------------------------------

    private async Task<List<OfflineAttendance>> LeerAsync()
    {
        if (!File.Exists(_filePath))
            return new List<OfflineAttendance>();

        List<OfflineAttendance> asistencias;

        try
        {
            string json = await File.ReadAllTextAsync(_filePath);

            asistencias =
                JsonSerializer.Deserialize<List<OfflineAttendance>>(json)
                ?? new List<OfflineAttendance>();
        }
        catch (Exception ex)
        {
            // No se descarta el archivo: se aparta una copia para poder
            // recuperar las asistencias pendientes a mano.
            AppLog.Error("No se pudo leer la cola de asistencias offline.", ex);
            ConservarCopiaCorrupta();
            return new List<OfflineAttendance>();
        }

        // Registros guardados por versiones anteriores no tienen identificador.
        bool hayCambios = false;
        foreach (var asistencia in asistencias)
        {
            if (string.IsNullOrWhiteSpace(asistencia.IdOperacion))
            {
                asistencia.IdOperacion = Guid.NewGuid().ToString("N");
                hayCambios = true;
            }
        }

        if (hayCambios)
            await GuardarAsync(asistencias);

        return asistencias;
    }

    private void ConservarCopiaCorrupta()
    {
        try
        {
            string copia = $"{_filePath}.corrupto-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Move(_filePath, copia);
            AppLog.Warn($"Se conservó el archivo ilegible como '{Path.GetFileName(copia)}'.");
        }
        catch (Exception ex)
        {
            AppLog.Error("No se pudo apartar el archivo de asistencias ilegible.", ex);
        }
    }

    private async Task GuardarAsync(
        List<OfflineAttendance> asistencias)
    {
        string json =
            JsonSerializer.Serialize(
                asistencias,
                JsonEscritura);

        string temporal = _filePath + ".tmp";

        await File.WriteAllTextAsync(temporal, json);
        File.Move(temporal, _filePath, overwrite: true);
    }
}

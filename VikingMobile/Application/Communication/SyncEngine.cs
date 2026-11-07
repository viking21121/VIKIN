using System.Text.Json;
using VikingMobile.Infrastructure.Offline;

namespace VikingMobile.Application.Communication;

public class SyncResult
{
    public int Exitosos { get; set; }
    public int Fallidos { get; set; }
    public int TotalProcesados => Exitosos + Fallidos;
    public int Restantes { get; set; }
    public bool Conectado { get; set; } = true;
    public string Mensaje { get; set; } = "";
}

public class SyncEngine
{
    private readonly OfflineAttendanceRepository _repository;
    private readonly CommunicationManager _communicationManager;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SyncEngine(
        OfflineAttendanceRepository repository,
        CommunicationManager communicationManager)
    {
        _repository = repository;
        _communicationManager = communicationManager;
    }

    public async Task<SyncResult> SincronizarPendientesAsync(string? serverAddress = null)
    {
        if (!await _syncLock.WaitAsync(0))
            return new SyncResult { Mensaje = "Ya hay una sincronización en curso.", Restantes = await _repository.ContarPendientesAsync() };

        try
        {
        string address = string.IsNullOrWhiteSpace(serverAddress)
            ? AppState.ServerAddress
            : serverAddress;

        var pendientes = await _repository.ObtenerPendientesAsync();

        if (pendientes.Count == 0)
        {
            return new SyncResult
            {
                Conectado = true,
                Mensaje = "No hay asistencias pendientes por sincronizar.",
                Restantes = 0
            };
        }

        // Verificar o restaurar conexión si es necesario
        if (_communicationManager.CurrentChannel?.IsConnected != true)
        {
            bool conectado = await _communicationManager.ConnectAsync(address);
            if (!conectado)
            {
                return new SyncResult
                {
                    Conectado = false,
                    Mensaje = "No se pudo conectar con el servidor Viking.",
                    Restantes = pendientes.Count
                };
            }
        }

        int exitosos = 0;
        int fallidos = 0;

        foreach (var item in pendientes)
        {
            try
            {
                var payload = new
                {
                    idOperacion = item.IdOperacion,
                    codigoAlumno = item.CodigoAlumno,
                    qr = item.Qr,
                    actividadId = item.ActividadId,
                    fechaHora = item.FechaHora
                };

                string jsonPayload = JsonSerializer.Serialize(payload);

                string responseText = await _communicationManager.SendAsync(
                    "api/asistencia/sincronizar",
                    jsonPayload);

                var responseObj = JsonSerializer.Deserialize<SyncAttendanceResponse>(
                    responseText,
                    JsonOptions);

                if (responseObj?.Ok == true)
                {
                    await _repository.MarcarSincronizadaAsync(item.Id);
                    exitosos++;
                }
                else
                {
                    string errorMsg = responseObj?.Mensaje ?? "Respuesta no válida del servidor.";
                    await _repository.MarcarErrorAsync(item.Id, errorMsg);
                    fallidos++;
                }
            }
            catch (Exception ex)
            {
                VikingMobile.Diagnostics.AppLog.Warn(
                    $"No se pudo sincronizar la asistencia {item.IdOperacion}.", ex);
                await _repository.MarcarErrorAsync(item.Id, ex.Message);
                fallidos++;
            }
        }

        int restantes = await _repository.ContarPendientesAsync();

        string mensaje = fallidos == 0
            ? $"✅ Sincronización exitosa: {exitosos} asistencia(s) enviada(s)."
            : $"⚠️ Sincronizados: {exitosos}, Fallidos: {fallidos}. Mantienen estado pendiente.";

        return new SyncResult
        {
            Exitosos = exitosos,
            Fallidos = fallidos,
            Restantes = restantes,
            Conectado = true,
            Mensaje = mensaje
        };
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private class SyncAttendanceResponse
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
    }
}

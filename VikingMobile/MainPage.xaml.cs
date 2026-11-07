using System.Text.Json;
using VikingMobile.Application.Activities;
using VikingMobile.Application.Communication;
using ZXing.Net.Maui;

namespace VikingMobile;

public partial class MainPage : ContentPage
{
    private bool _procesandoQr;
    private bool _sincronizandoAutomatico;

    public MainPage()
    {
        InitializeComponent();
    }

    // =========================================================
    // CICLO DE VIDA
    // =========================================================

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Restaurar actividad desde Preferences si el contexto se perdió
        RestaurarActividadGuardada();

        // Reactivar cámara cada vez que se entra a la pantalla
        BarcodeReaderView.IsDetecting = true;
        _procesandoQr = false;

        StatusLabel.Text = "Listo para escanear";

        _ = InicializarModoAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Pausar cámara al salir (ahorro de recursos)
        BarcodeReaderView.IsDetecting = false;
    }

    // =========================================================
    // INICIALIZACIÓN
    // =========================================================

    private void RestaurarActividadGuardada()
    {
        if (ActividadContexto.ActividadId != null)
            return;

        int id     = Preferences.Get("viking_actividad_id", 0);
        string nom = Preferences.Get("viking_actividad_nombre", "");

        if (id > 0 && !string.IsNullOrWhiteSpace(nom))
        {
            ActividadContexto.Establecer(new ActividadMobile
            {
                Id = id,
                Nombre = nom
            });
        }
    }

    private async Task InicializarModoAsync()
    {
        if (AppState.ModeManager.CurrentMode == AttendanceMode.Offline)
        {
            StatusLabel.Text = "Listo para escanear";
            return;
        }

        // Si ya está conectado (viene de ConfiguracionPage), sincronizar pendientes.
        if (AppState.IsConnected)
        {
            await IntentarSincronizacionAutomaticaAsync();
            return;
        }

        // Si no está conectado, intentar conectar.
        await ConectarConVikingAsync();
    }

    // =========================================================
    // QR
    // =========================================================

    private void BarcodesDetected(
        object sender,
        BarcodeDetectionEventArgs e)
    {
        if (_procesandoQr)
            return;

        var barcode = e.Results.FirstOrDefault();
        if (barcode == null)
            return;

        string contenido = barcode.Value?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(contenido))
            return;

        _procesandoQr = true;

        MainThread.BeginInvokeOnMainThread(
            async () => await RegistrarAsistenciaDesdeQr(contenido));
    }

    private async Task RegistrarAsistenciaDesdeQr(string contenido)
    {
        try
        {
            // Pausar detección mientras se procesa
            BarcodeReaderView.IsDetecting = false;

            string codigoAlumno = ExtraerCodigoAlumno(contenido);

            if (string.IsNullOrWhiteSpace(codigoAlumno))
            {
                StatusLabel.Text = "QR no válido";
                return;
            }

            // ── MODO OFFLINE ───────────────────────────────────────
            if (AppState.ModeManager.CurrentMode == AttendanceMode.Offline)
            {
                await RegistrarOfflineAsync(codigoAlumno, contenido);
                return;
            }

            // ── VALIDAR ACTIVIDAD ──────────────────────────────────
            if (ActividadContexto.ActividadId == null)
            {
                StatusLabel.Text = "Selecciona una actividad en Configuración";
                return;
            }

            // ── MODO AUTOMÁTICO: reconectar si es necesario ────────
            bool conectado = AppState.IsConnected;

            if (!conectado &&
                AppState.ModeManager.CurrentMode == AttendanceMode.Automatic)
            {
                conectado = await AppState.ConnectAsync();
            }

            // Sin conexión
            if (!conectado)
            {
                if (AppState.ModeManager.CurrentMode == AttendanceMode.Automatic)
                {
                    await RegistrarOfflineAsync(codigoAlumno, contenido);
                    return;
                }

                StatusLabel.Text = "Sin conexión con Viking";
                return;
            }

            // ── ONLINE ─────────────────────────────────────────────
            StatusLabel.Text = "Registrando...";

            // Mismo identificador para el intento en línea y para la copia
            // offline: si el servidor recibió el envío pero se perdió la
            // respuesta, el reenvío posterior no se duplica.
            string idOperacion = Guid.NewGuid().ToString("N");

            var payload = new
            {
                idOperacion,
                codigoAlumno,
                qr         = contenido,
                actividadId = ActividadContexto.ActividadId,
                fechaHora   = DateTime.Now
            };

            string json;
            string responseText;

            try
            {
                json         = JsonSerializer.Serialize(payload);
                responseText = await AppState.Communication.SendAsync(
                    "api/asistencia/registrar",
                    json);
            }
            catch
            {
                // Falló el envío
                if (AppState.ModeManager.CurrentMode == AttendanceMode.Automatic)
                {
                    await RegistrarOfflineAsync(codigoAlumno, contenido, idOperacion);
                    return;
                }

                StatusLabel.Text = "Error al conectar con Viking";
                return;
            }

            var resultado = JsonSerializer.Deserialize<AttendanceResponse>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resultado?.Ok == true)
            {
                AlumnoNombreLabel.Text =
                    resultado.Alumno ?? "Alumno";

                AlumnoCodigoLabel.Text = codigoAlumno;

                PlanLabel.Text =
                    string.IsNullOrWhiteSpace(resultado.Plan)
                        ? "Sin plan"
                        : resultado.Plan;

                VencimientoLabel.Text =
                    string.IsNullOrWhiteSpace(resultado.FechaVencimiento)
                        ? "—"
                        : resultado.FechaVencimiento;

                // Aplicar estado del semáforo
                string semaforo = resultado.EstadoSemaforo?.ToLowerInvariant() ?? "verde";
                if (semaforo == "rojo")
                {
                    CardSemaforoBanner.BackgroundColor = Color.FromArgb("#DC2626");
                    CardSemaforoLabel.Text = "🔴 VENCIDO";
                }
                else if (semaforo == "amarillo")
                {
                    CardSemaforoBanner.BackgroundColor = Color.FromArgb("#D97706");
                    CardSemaforoLabel.Text = $"🟡 {resultado.TituloEstado ?? "INCOMPLETO"}";
                }
                else
                {
                    CardSemaforoBanner.BackgroundColor = Color.FromArgb("#16803A");
                    CardSemaforoLabel.Text = "🟢 SOLVENTE";
                }

                if (!string.IsNullOrWhiteSpace(resultado.SubtituloEstado))
                {
                    CardDetalleEstadoLabel.Text = resultado.SubtituloEstado;
                    CardDetalleEstadoLabel.TextColor = semaforo == "rojo" ? Color.FromArgb("#DC2626") : Color.FromArgb("#D97706");
                    CardDetalleEstadoLabel.IsVisible = true;
                }
                else
                {
                    CardDetalleEstadoLabel.IsVisible = false;
                }

                // Cargar foto si está disponible
                if (AppState.IsConnected && !string.IsNullOrWhiteSpace(codigoAlumno))
                {
                    try
                    {
                        string fotoUrl = $"{AppState.CommunicationServerUrl.TrimEnd('/')}/api/alumnos/{Uri.EscapeDataString(codigoAlumno)}/foto?t={DateTime.UtcNow.Ticks}";
                        CardFotoAlumnoImage.Source = new UriImageSource { Uri = new Uri(fotoUrl), CachingEnabled = false };
                        CardFotoAlumnoImage.IsVisible = true;
                        CardFotoPlaceholderLabel.IsVisible = false;
                    }
                    catch
                    {
                        CardFotoAlumnoImage.IsVisible = false;
                        CardFotoPlaceholderLabel.IsVisible = true;
                    }
                }
                else
                {
                    CardFotoAlumnoImage.IsVisible = false;
                    CardFotoPlaceholderLabel.IsVisible = true;
                }

                StatusLabel.Text = "✓ Asistencia procesada";
                AlumnoCard.IsVisible = true;
            }
            else
            {
                StatusLabel.Text =
                    resultado?.Mensaje ?? "No se pudo registrar";
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            await Task.Delay(5800);

            AlumnoCard.IsVisible = false;
            StatusLabel.Text     = "Listo para escanear";
            _procesandoQr        = false;

            // Reactivar cámara para la siguiente lectura
            BarcodeReaderView.IsDetecting = true;
        }
    }

    // =========================================================
    // OFFLINE
    // =========================================================

    private async Task RegistrarOfflineAsync(
        string codigoAlumno,
        string contenido,
        string? idOperacion = null)
    {
        if (ActividadContexto.ActividadId == null)
        {
            StatusLabel.Text = "Selecciona una actividad en Configuración";
            return;
        }

        StatusLabel.Text = "Guardando...";

        try
        {
            var registro = new Infrastructure.Offline.OfflineAttendance
            {
                IdOperacion        = idOperacion ?? Guid.NewGuid().ToString("N"),
                CodigoAlumno       = codigoAlumno,
                Qr                 = contenido,
                ActividadId        = ActividadContexto.ActividadId,
                FechaHora          = DateTime.Now,
                Sincronizada       = false,
                FechaSincronizacion = null
            };

            bool agregada = await AppState.OfflineRepo.AgregarAsync(registro);
            int pendientes = await AppState.OfflineRepo.ContarPendientesAsync();

            StatusLabel.Text = agregada
                ? $"Guardada sin conexión · {pendientes} pendiente(s)"
                : $"Ya estaba guardada · {pendientes} pendiente(s)";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error al guardar: {ex.Message}";
        }
    }

    // =========================================================
    // CONEXIÓN Y SINCRONIZACIÓN AUTOMÁTICA
    // =========================================================

    private async Task ConectarConVikingAsync()
    {
        try
        {
            StatusLabel.Text = "Conectando...";

            bool conectado = await AppState.ConnectAsync();

            if (!conectado)
            {
                StatusLabel.Text = "Listo para escanear";
                return;
            }

            // Actualizar lista de actividades en AppState
            await AppState.CargarActividadesAsync();

            // Restaurar actividad si sigue disponible
            if (ActividadContexto.ActividadId != null &&
                AppState.Actividades.Count > 0)
            {
                var match = AppState.Actividades.FirstOrDefault(
                    x => x.Id == ActividadContexto.ActividadId.Value);

                if (match != null)
                {
                    ActividadContexto.Establecer(new ActividadMobile
                    {
                        Id = match.Id,
                        Nombre = match.Nombre
                    });
                }
            }

            StatusLabel.Text = "Listo para escanear";

            await IntentarSincronizacionAutomaticaAsync();
        }
        catch
        {
            StatusLabel.Text = "Listo para escanear";
        }
    }

    private async Task IntentarSincronizacionAutomaticaAsync()
    {
        if (_sincronizandoAutomatico)
            return;

        int pendientes = await AppState.OfflineRepo.ContarPendientesAsync();
        if (pendientes == 0)
            return;

        try
        {
            _sincronizandoAutomatico = true;
            StatusLabel.Text = $"Sincronizando {pendientes} pendiente(s)...";

            var resultado = await AppState.SyncEngine.SincronizarPendientesAsync();

            if (resultado.Exitosos > 0)
            {
                StatusLabel.Text =
                    $"✓ {resultado.Exitosos} asistencia(s) sincronizada(s)";

                await Task.Delay(2000);
            }
        }
        catch
        {
            // Falla silenciosa: no bloquear al escáner
        }
        finally
        {
            _sincronizandoAutomatico = false;
            StatusLabel.Text = "Listo para escanear";
        }
    }

    // =========================================================
    // QR → CÓDIGO
    // =========================================================

    private string ExtraerCodigoAlumno(string contenido)
    {
        if (contenido.StartsWith("VIKING|ALUMNO|"))
        {
            string[] partes = contenido.Split('|');
            if (partes.Length >= 3)
                return partes[2].Trim();
        }

        // Compatibilidad con QR antiguos: A0025
        return contenido.Trim();
    }

    // =========================================================
    // RESPUESTA ASISTENCIA
    // =========================================================

    private class AttendanceResponse
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string? Alumno { get; set; }
        public int? AlumnoId { get; set; }
        public string? Plan { get; set; }
        public string? FechaVencimiento { get; set; }
        public string? EstadoSemaforo { get; set; }
        public string? TituloEstado { get; set; }
        public string? SubtituloEstado { get; set; }
        public bool TieneFoto { get; set; }
    }
}
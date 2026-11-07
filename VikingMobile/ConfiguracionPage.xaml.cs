using VikingMobile.Application.Activities;
using VikingMobile.Application.Communication;
using VikingMobile.Diagnostics;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace VikingMobile;

public partial class ConfiguracionPage : ContentPage
{
    private const string ActividadIdKey     = "viking_actividad_id";
    private const string ActividadNombreKey = "viking_actividad_nombre";

    public ConfiguracionPage()
    {
        InitializeComponent();

        ActualizarModoVisual();
        ActualizarServerVisual();
        CargarActividadGuardada();
        ActualizarIdentidadVisual();
        _ = InicializarAsync();
    }

    private void LogoLabel_Tapped(object sender, TappedEventArgs e)
    {
        // 5 toques seguidos sobre el logo revelan la sección avanzada
        // (validación de identidad, diagnóstico). A propósito no hay un
        // botón visible para esto: son ajustes que solo debería tocar
        // quien esté configurando la app, no cualquiera que la abra.
        AvanzadoStack.IsVisible = !AvanzadoStack.IsVisible;
    }

    // =========================================================
    // CICLO DE VIDA
    // =========================================================

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ContinuarButton.IsEnabled = true;
        ActualizarModoVisual();
        ActualizarServerVisual();
        ActualizarActividadVisual();
        ActualizarIdentidadVisual();
        _ = ActualizarPendientesAsync();
    }

    // =========================================================
    // INICIALIZACIÓN Y CONEXIÓN
    // =========================================================

    private async Task InicializarAsync()
    {
        await ActualizarPendientesAsync();

        if (AppState.ModeManager.CurrentMode == AttendanceMode.Offline)
        {
            AppState.Actividades =
                AppState.ActividadService.ObtenerActividadesLocales();

            ReintentarConexionButton.IsVisible = false;
            EstadoLabel.Text = AppState.Actividades.Count > 0
                ? "🟠 Modo offline · actividades disponibles"
                : "🟠 Modo offline · sin actividades guardadas";

            return;
        }

        await ProbarConexionAsync();
    }

    private async Task ProbarConexionAsync()
    {
        ReintentarConexionButton.IsVisible = false;
        EstadoLabel.Text = $"🔄 Conectando con Viking ({AppState.ServerAddress})...";

        bool conectado = await AppState.ConnectAsync();

        if (conectado)
        {
            await AppState.CargarActividadesAsync();
            ActualizarActividadVisual();
            EstadoLabel.Text = "🟢 Viking conectado por Wi-Fi";
            ReintentarConexionButton.IsVisible = false;
        }
        else
        {
            string detalle = !string.IsNullOrWhiteSpace(AppState.UltimoError)
                ? $" ({AppState.UltimoError})"
                : "";

            if (AppState.ModeManager.CurrentMode == AttendanceMode.Automatic)
            {
                AppState.Actividades =
                    AppState.ActividadService.ObtenerActividadesLocales();

                EstadoLabel.Text = AppState.Actividades.Count > 0
                    ? $"🟠 Sin conexión{detalle} · usando actividades locales"
                    : $"🟠 Sin conexión con Viking{detalle}";
            }
            else
            {
                EstadoLabel.Text = $"🔴 No se pudo conectar con Viking{detalle}";
            }

            ReintentarConexionButton.IsVisible = true;
        }
    }

    private async void ReintentarConexionButton_Clicked(object sender, EventArgs e)
    {
        await ProbarConexionAsync();
    }

    // =========================================================
    // DIRECCIÓN SERVIDOR (IP)
    // =========================================================

    private void ActualizarServerVisual()
    {
        ServerLabel.Text = AppState.ServerAddress;
    }

    private async void ServerBorder_Tapped(object sender, TappedEventArgs e)
    {
        string? nuevaUrl = await DisplayPromptAsync(
            "Servidor Viking",
            "Ingresa la dirección IP del servidor Viking:",
            "Guardar",
            "Cancelar",
            initialValue: AppState.ServerAddress,
            keyboard: Keyboard.Url);

        if (string.IsNullOrWhiteSpace(nuevaUrl))
            return;

        nuevaUrl = nuevaUrl.Trim();
        if (!nuevaUrl.StartsWith("http://") && !nuevaUrl.StartsWith("https://"))
        {
            nuevaUrl = "http://" + nuevaUrl;
        }

        AppState.ServerAddress = nuevaUrl;
        ActualizarServerVisual();

        if (AppState.ModeManager.CurrentMode != AttendanceMode.Offline)
        {
            await ProbarConexionAsync();
        }
    }

    // =========================================================
    // MODO
    // =========================================================

    private void ActualizarModoVisual()
    {
        ModoLabel.Text = AppState.ModeManager.GetDisplayText();
    }

    private async void ModoBorder_Tapped(
        object sender,
        TappedEventArgs e)
    {
        string? opcion = await DisplayActionSheet(
            "Modo de asistencia",
            "Cancelar",
            null,
            "🟢 Online",
            "🟠 Offline",
            "🔵 Automático");

        if (string.IsNullOrWhiteSpace(opcion) || opcion == "Cancelar")
            return;

        AttendanceMode nuevoModo = opcion switch
        {
            "🟢 Online"     => AttendanceMode.Online,
            "🟠 Offline"    => AttendanceMode.Offline,
            "🔵 Automático" => AttendanceMode.Automatic,
            _               => AppState.ModeManager.CurrentMode
        };

        AppState.ModeManager.SetMode(nuevoModo);
        ActualizarModoVisual();

        if (nuevoModo == AttendanceMode.Offline)
        {
            await AppState.Communication.DisconnectAsync();
            AppState.Actividades =
                AppState.ActividadService.ObtenerActividadesLocales();
            EstadoLabel.Text = "🟠 Modo offline";
            ReintentarConexionButton.IsVisible = false;
            return;
        }

        await ProbarConexionAsync();
    }

    // =========================================================
    // ACTIVIDAD GUARDADA
    // =========================================================

    private void CargarActividadGuardada()
    {
        if (ActividadContexto.ActividadId != null)
        {
            ActualizarActividadVisual();
            return;
        }

        int id     = Preferences.Get(ActividadIdKey, 0);
        string nom = Preferences.Get(ActividadNombreKey, "");

        if (id > 0 && !string.IsNullOrWhiteSpace(nom))
        {
            ActividadContexto.Establecer(new ActividadMobile
            {
                Id = id,
                Nombre = nom
            });
        }

        ActualizarActividadVisual();
    }

    private void ActualizarActividadVisual()
    {
        ActividadLabel.Text = ActividadContexto.ActividadId == null
            ? "🏢 Seleccionar actividad"
            : $"🏢 {ActividadContexto.NombreActividad}";
    }

    // =========================================================
    // SELECTOR DE ACTIVIDAD
    // =========================================================

    private async void ActividadBorder_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (AppState.Actividades.Count == 0)
        {
            await AppState.CargarActividadesAsync();
        }

        if (AppState.Actividades.Count == 0)
        {
            await DisplayAlert(
                "Sin actividades",
                "No hay actividades disponibles. Revisa la dirección del servidor o la conexión Wi-Fi.",
                "OK");

            return;
        }

        await MostrarSelectorAsync();
    }

    private async Task MostrarSelectorAsync()
    {
        var opciones = AppState.Actividades
            .Select(x => $"{x.Id} · {x.Nombre}")
            .ToArray();

        string? seleccion = await DisplayActionSheet(
            "Selecciona la actividad",
            "Cancelar",
            null,
            opciones);

        if (string.IsNullOrWhiteSpace(seleccion) || seleccion == "Cancelar")
            return;

        var actividad = AppState.Actividades.FirstOrDefault(
            x => seleccion.StartsWith($"{x.Id} · "));

        if (actividad == null)
            return;

        ActividadContexto.Establecer(new ActividadMobile
        {
            Id = actividad.Id,
            Nombre = actividad.Nombre
        });

        Preferences.Set(ActividadIdKey, actividad.Id);
        Preferences.Set(ActividadNombreKey, actividad.Nombre);

        ActualizarActividadVisual();
        EstadoLabel.Text = $"🟢 Actividad: {actividad.Nombre}";
    }

    // =========================================================
    // PENDIENTES OFFLINE
    // =========================================================

    private async Task ActualizarPendientesAsync()
    {
        int pendientes = await AppState.OfflineRepo.ContarPendientesAsync();
        PendientesLabel.Text = pendientes.ToString();
    }

    private async void SincronizarAhoraButton_Clicked(
        object sender,
        EventArgs e)
    {
        int pendientes = await AppState.OfflineRepo.ContarPendientesAsync();

        if (pendientes == 0)
        {
            await DisplayAlert(
                "Sincronización",
                "No hay asistencias pendientes por sincronizar.",
                "OK");

            return;
        }

        try
        {
            SincronizarAhoraButton.IsEnabled = false;
            SincronizarAhoraButton.Text = "⏳ Sincronizando...";

            if (!AppState.IsConnected)
            {
                bool conectado = await AppState.ConnectAsync();
                if (!conectado)
                {
                    await DisplayAlert(
                        "Sin conexión",
                        $"No se pudo conectar con Viking ({AppState.ServerAddress}).",
                        "OK");

                    return;
                }
            }

            var resultado = await AppState.SyncEngine.SincronizarPendientesAsync();
            await ActualizarPendientesAsync();

            await DisplayAlert("Sincronización", resultado.Mensaje, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Error",
                $"Error al sincronizar: {ex.Message}",
                "OK");
        }
        finally
        {
            SincronizarAhoraButton.Text = "🔄 SINCRONIZAR AHORA";
            SincronizarAhoraButton.IsEnabled = true;
        }
    }

    // =========================================================
    // CONTINUAR
    // =========================================================

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (ActividadContexto.ActividadId == null)
        {
            await DisplayAlert(
                "Actividad requerida",
                "Selecciona una actividad antes de continuar.",
                "OK");

            return;
        }

        ContinuarButton.IsEnabled = false;

        await Shell.Current.GoToAsync("//MainPage");
    }
    private async void CorreoBorder_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(CorreoConfiguracionPage));
    }

    // =========================================================
    // VALIDACIÓN DE IDENTIDAD (CREAR/RECUPERAR PIN)
    // =========================================================

    private void ActualizarIdentidadVisual()
    {
        bool requiere = AppState.AuthService.RequiereValidacionPc;

        IdentidadLabel.Text = requiere
            ? "🟢 Con Viking PC"
            : "🟠 Manual (sin validar)";

        IdentidadAyudaLabel.IsVisible = !requiere;
    }

    private async void IdentidadBorder_Tapped(object sender, TappedEventArgs e)
    {
        string? opcion = await DisplayActionSheet(
            "Validación de identidad al crear/recuperar PIN",
            "Cancelar",
            null,
            "🟢 Con Viking PC",
            "🟠 Manual (sin validar)");

        if (string.IsNullOrWhiteSpace(opcion) || opcion == "Cancelar")
            return;

        if (opcion == "🟠 Manual (sin validar)")
        {
            bool confirmar = await DisplayAlert(
                "Modo manual",
                "En Manual, cualquiera que escriba un código de alumno o el nombre del establecimiento podrá crear un PIN, sin que Viking PC lo confirme primero. Úsalo solo mientras Viking PC no tenga listos api/cliente/validar y api/admin/validar.\n\n¿Deseas activarlo?",
                "Sí, activar",
                "Cancelar");

            if (!confirmar)
            {
                ActualizarIdentidadVisual();
                return;
            }
        }

        AppState.AuthService.RequiereValidacionPc = opcion == "🟢 Con Viking PC";
        ActualizarIdentidadVisual();
    }

    // =========================================================
    // DIAGNÓSTICO
    // =========================================================

    private async void DiagnosticoBorder_Tapped(object sender, TappedEventArgs e)
    {
        string log = CrashLogger.LeerUltimoError();

        // DisplayAlert no hace scroll bien con textos muy largos:
        // se recorta a los últimos ~1500 caracteres, que es lo más
        // reciente y lo más útil para diagnosticar.
        if (log.Length > 1500)
            log = "…\n" + log[^1500..];

        bool copiar = await DisplayAlert("Últimos errores", log, "Copiar", "Cerrar");

        if (copiar)
            await Clipboard.Default.SetTextAsync(CrashLogger.LeerUltimoError());
    }

}
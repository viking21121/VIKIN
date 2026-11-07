using VikingMobile.Application.Activities;
using VikingMobile.Application.Auth;
using VikingMobile.Application.Admin;
using VikingMobile.Application.Cliente;
using VikingMobile.Infrastructure.Communication;
using VikingMobile.Infrastructure.Offline;

namespace VikingMobile.Application.Communication;

/// <summary>
/// Estado global de la aplicación. Instancia única compartida
/// entre ConfiguracionPage y MainPage para evitar problemas de
/// conexiones duplicadas o desconectadas al navegar entre páginas.
/// </summary>
public static class AppState
{
    private const string ServerAddressKey = "viking_server_address";
    public const string DefaultServerAddress = "http://192.168.1.5:5050";

    public static string ServerAddress
    {
        get
        {
            var val = Preferences.Get(ServerAddressKey, DefaultServerAddress);
            if (!string.IsNullOrEmpty(val) && val.Contains("192.168.1.6"))
            {
                Preferences.Set(ServerAddressKey, DefaultServerAddress);
                return DefaultServerAddress;
            }
            return val;
        }
        set => Preferences.Set(ServerAddressKey, string.IsNullOrWhiteSpace(value) ? DefaultServerAddress : value.Trim());
    }

    public static string CommunicationServerUrl => ServerAddress.TrimEnd('/');

    // ── Canal de red ──────────────────────────────────────────────
    public static readonly WiFiNetworkChannel WifiChannel =
        new WiFiNetworkChannel();

    // ── Gestor de comunicación ────────────────────────────────────
    public static readonly CommunicationManager Communication;

    // ── Gestores de dominio ───────────────────────────────────────
    public static readonly AttendanceModeManager ModeManager =
        new AttendanceModeManager();

    public static readonly ActividadService ActividadService;

    // ── Modo Cliente / Modo Administrador ──────────────────────────
    public static readonly ClienteService ClienteService;

    public static readonly AdminService AdminService;
    public static readonly MobileAuthService AuthService;

    public static readonly OfflineAttendanceRepository OfflineRepo =
        new OfflineAttendanceRepository();

    public static readonly SyncEngine SyncEngine;

    // ── Lista de actividades disponibles (compartida) ─────────────
    public static List<ActividadResponse> Actividades { get; set; } =
        new();

    static AppState()
    {
        Communication = new CommunicationManager();
        Communication.AddChannel(WifiChannel);

        ActividadService = new ActividadService(WifiChannel);
        ClienteService = new ClienteService(WifiChannel);
        AdminService = new AdminService(WifiChannel);
        AuthService = new MobileAuthService();

        SyncEngine = new SyncEngine(OfflineRepo, Communication);
    }

    // ── Helpers ───────────────────────────────────────────────────

    public static bool IsConnected =>
        Communication.CurrentChannel?.IsConnected == true;

    public static string? UltimoError => WifiChannel.UltimoError;

    public static async Task<bool> ConnectAsync() =>
        await Communication.ConnectAsync(ServerAddress);

    public static async Task<List<ActividadResponse>> CargarActividadesAsync()
    {
        try
        {
            if (IsConnected)
            {
                var lista = await ActividadService.ObtenerActividadesAsync();
                if (lista.Count > 0)
                {
                    Actividades = lista;
                    return lista;
                }
            }
        }
        catch { /* falla silenciosa: se usa caché */ }

        // Fallback a caché local
        var local = ActividadService.ObtenerActividadesLocales();
        if (local.Count > 0)
            Actividades = local;

        return Actividades;
    }

    /// <summary>
    /// Logo del establecimiento (personalización de marca), disponible
    /// para cualquier pantalla sin necesitar sesión de administrador ni
    /// de cliente. Si no hay actividad seleccionada, o el establecimiento
    /// no configuró un logo, o falla la conexión, devuelve null y quien
    /// llama simplemente muestra su texto por defecto — nunca bloquea.
    /// </summary>
    public static async Task<byte[]?> ObtenerLogoPublicoAsync()
    {
        try
        {
            if (ActividadContexto.ActividadId is null or 0)
                return null;

            string json = System.Text.Json.JsonSerializer.Serialize(new
            {
                actividadId = ActividadContexto.ActividadId.Value.ToString()
            });

            string text = await WifiChannel.SendAsync("api/publico/logo", json);

            using var doc = System.Text.Json.JsonDocument.Parse(text);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ok", out var okProp) || !okProp.GetBoolean())
                return null;

            if (!root.TryGetProperty("logoBase64", out var logoProp))
                return null;

            string base64 = logoProp.GetString() ?? "";
            return string.IsNullOrWhiteSpace(base64) ? null : Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }
    }
}

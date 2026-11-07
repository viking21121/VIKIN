using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace VikingMobile.Application.Auth;

public sealed class MobileAuthService
{
    private const string AccountsKey = "viking_mobile_accounts";
    private const string PendingPrefix = "viking_mobile_code_";
    private const string SenderEmailKey = "viking_mail_sender";
    private const string SenderPasswordKey = "viking_mail_app_password";
    private const string RequiereValidacionKey = "viking_mobile_requiere_validacion_pc";
    private const string DefaultSender = "avendanoalejandro841@gmail.com";

    private readonly object _sync = new();
    private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public string SenderEmail
    {
        get => Preferences.Get(SenderEmailKey, DefaultSender);
        set => Preferences.Set(SenderEmailKey, string.IsNullOrWhiteSpace(value) ? DefaultSender : value.Trim());
    }

    /// <summary>
    /// Si es true (valor por defecto), crear o recuperar un PIN exige
    /// validar antes la identidad contra Viking PC (api/cliente/validar
    /// o api/admin/validar). Se puede desactivar desde Configuración
    /// mientras esos endpoints todavía no existan en Viking PC, para no
    /// bloquear la configuración del PIN en Mobile.
    /// </summary>
    public bool RequiereValidacionPc
    {
        get => Preferences.Get(RequiereValidacionKey, true);
        set => Preferences.Set(RequiereValidacionKey, value);
    }

    public async Task<bool> TieneCorreoConfiguradoAsync()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(await SecureStorage.Default.GetAsync(SenderPasswordKey));
        }
        catch
        {
            return false;
        }
    }

    public async Task ConfigurarCorreoAsync(string appPassword)
    {
        if (string.IsNullOrWhiteSpace(appPassword))
        {
            SecureStorage.Default.Remove(SenderPasswordKey);
            return;
        }

        await SecureStorage.Default.SetAsync(SenderPasswordKey, appPassword.Trim());
    }

    public MobileAccount? ObtenerCuenta(MobileAccountRole rol, string usuario)
    {
        return ObtenerCuentas().FirstOrDefault(x =>
            x.Rol == rol &&
            x.Usuario.Equals(usuario.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool VerificarPin(MobileAccount cuenta, string pin)
    {
        return PinValido(pin) &&
               CryptographicOperations.FixedTimeEquals(
                   Convert.FromHexString(cuenta.PinHash),
                   SHA256.HashData(Encoding.UTF8.GetBytes(pin)));
    }

    public async Task<(bool Ok, string Mensaje)> EnviarCodigoRegistroAsync(
        MobileAccountRole rol, string usuario, string correo)
    {
        usuario = usuario.Trim();
        correo = correo.Trim();

        if (!correo.Contains('@'))
            return (false, "Ingresa un correo válido.");

        var existing = ObtenerCuenta(rol, usuario);
        if (existing != null)
            return (false, "Esta cuenta ya tiene un PIN configurado.");

        var identity = await ObtenerIdentidadAsync(rol, usuario);
        if (!identity.Ok)
            return identity;

        return await GenerarYEnviarCodigoAsync(rol, usuario, correo, true);
    }

    public async Task<(bool Ok, string Mensaje)> EnviarCodigoAsync(
        MobileAccountRole rol, string usuario)
    {
        var cuenta = ObtenerCuenta(rol, usuario);
        if (cuenta == null)
            return (false, "No existe una cuenta Mobile para ese usuario.");

        return await GenerarYEnviarCodigoAsync(rol, usuario, cuenta.Correo, false);
    }

    public bool VerificarCodigo(MobileAccountRole rol, string usuario, string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return false;

        string key = PendingKey(rol, usuario);
        string raw = Preferences.Get(key, "");
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        try
        {
            var pending = JsonSerializer.Deserialize<PendingCode>(raw, _json);
            if (pending == null || pending.ExpiraUtc < DateTime.UtcNow || pending.Intentos >= 5)
            {
                Preferences.Remove(key);
                return false;
            }

            if (pending.Codigo != codigo.Trim())
            {
                pending.Intentos++;
                Preferences.Set(key, JsonSerializer.Serialize(pending));
                return false;
            }

            pending.Verificado = true;
            Preferences.Set(key, JsonSerializer.Serialize(pending));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool CrearCuentaConCodigo(
        MobileAccountRole rol, string usuario, string codigo, string pin)
    {
        if (!PinValido(pin) || !VerificarCodigo(rol, usuario, codigo))
            return false;

        var raw = Preferences.Get(PendingKey(rol, usuario), "");
        var pending = JsonSerializer.Deserialize<PendingCode>(raw, _json);
        if (pending == null || !pending.Verificado)
            return false;

        var cuentas = ObtenerCuentas();
        cuentas.RemoveAll(x => x.Rol == rol &&
                               x.Usuario.Equals(usuario.Trim(), StringComparison.OrdinalIgnoreCase));

        cuentas.Add(new MobileAccount
        {
            Rol = rol,
            Usuario = usuario.Trim(),
            Correo = pending.Correo,
            PinHash = HashPin(pin),
            CorreoVerificado = true
        });

        GuardarCuentas(cuentas);
        Preferences.Remove(PendingKey(rol, usuario));
        return true;
    }

    public bool CambiarPinConCodigo(
        MobileAccountRole rol, string usuario, string pin)
    {
        if (!PinValido(pin))
            return false;

        string raw = Preferences.Get(PendingKey(rol, usuario), "");
        var pending = JsonSerializer.Deserialize<PendingCode>(raw, _json);
        if (pending == null || !pending.Verificado || pending.ExpiraUtc < DateTime.UtcNow)
            return false;

        var cuenta = ObtenerCuenta(rol, usuario);
        if (cuenta == null)
            return false;

        cuenta.PinHash = HashPin(pin);
        cuenta.CorreoVerificado = true;
        GuardarCuentas(ObtenerCuentas());
        Preferences.Remove(PendingKey(rol, usuario));
        return true;
    }

    public async Task<(bool Ok, string Mensaje)> ObtenerIdentidadAsync(
        MobileAccountRole rol, string usuario)
    {
        // Modo manual: mientras Viking PC no exponga api/cliente/validar
        // ni api/admin/validar, esto permite seguir configurando el PIN
        // en Mobile sin quedar bloqueado por esa dependencia.
        if (!RequiereValidacionPc)
            return (true, "");

        if (VikingMobile.Application.Activities.ActividadContexto.ActividadId is null or 0)
            return (false, "Selecciona una actividad en Configuración antes de continuar.");

        try
        {
            if (!await VikingMobile.Application.Communication.AppState.ConnectAsync())
                return (false, "No se pudo conectar con Viking PC.");

            string endpoint = rol == MobileAccountRole.Cliente
                ? "api/cliente/validar"
                : "api/admin/validar";

            string json;

            if (rol == MobileAccountRole.Cliente)
            {
                json = JsonSerializer.Serialize(new
                {
                    codigoAlumno = usuario,
                    actividadId = (VikingMobile.Application.Activities.ActividadContexto.ActividadId ?? 0).ToString()
                });
            }
            else
            {
                json = JsonSerializer.Serialize(new
                {
                    usuario,
                    actividadId = (VikingMobile.Application.Activities.ActividadContexto.ActividadId ?? 0).ToString()
                });
            }

            string response = await VikingMobile.Application.Communication.AppState.Communication.SendAsync(endpoint, json);
            using var doc = JsonDocument.Parse(response);

            bool ok = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
            string mensaje = doc.RootElement.TryGetProperty("mensaje", out var msg)
                ? msg.GetString() ?? ""
                : "";

            return ok ? (true, "") : (false, string.IsNullOrWhiteSpace(mensaje) ? "La identidad no pudo validarse en Viking PC." : mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo validar con Viking PC: {ex.Message}");
        }
    }

    private async Task<(bool Ok, string Mensaje)> GenerarYEnviarCodigoAsync(
        MobileAccountRole rol, string usuario, string correo, bool registro)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return (false, "Esta cuenta no tiene un correo configurado.");

        string? appPassword = await SecureStorage.Default.GetAsync(SenderPasswordKey);
        if (string.IsNullOrWhiteSpace(appPassword))
            return (false, "Configura primero la contraseña de aplicación de Gmail en Configuración.");

        string codigo = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        Preferences.Set(PendingKey(rol, usuario), JsonSerializer.Serialize(new PendingCode
        {
            Codigo = codigo,
            Correo = correo,
            ExpiraUtc = DateTime.UtcNow.AddMinutes(10),
            Intentos = 0,
            Verificado = false
        }));

        try
        {
            var sender = new GmailVerificationCodeSender(SenderEmail, appPassword);
            await sender.EnviarAsync(correo, codigo);
            return (true, "Código enviado al correo.");
        }
        catch (Exception ex)
        {
            Preferences.Remove(PendingKey(rol, usuario));
            return (false, $"No se pudo enviar el correo: {ex.Message}");
        }
    }

    private List<MobileAccount> ObtenerCuentas()
    {
        lock (_sync)
        {
            try
            {
                string raw = Preferences.Get(AccountsKey, "");
                return string.IsNullOrWhiteSpace(raw)
                    ? new List<MobileAccount>()
                    : JsonSerializer.Deserialize<List<MobileAccount>>(raw, _json) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }

    private void GuardarCuentas(List<MobileAccount> cuentas)
    {
        lock (_sync)
            Preferences.Set(AccountsKey, JsonSerializer.Serialize(cuentas));
    }

    private static string HashPin(string pin) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pin)));

    public static bool PinValido(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4 || pin.Length > 8)
            return false;
        return pin.All(char.IsDigit);
    }

    private static string PendingKey(MobileAccountRole rol, string usuario) =>
        $"{PendingPrefix}{rol}_{usuario.Trim().ToUpperInvariant()}";

    private sealed class PendingCode
    {
        public string Codigo { get; set; } = "";
        public string Correo { get; set; } = "";
        public DateTime ExpiraUtc { get; set; }
        public int Intentos { get; set; }
        public bool Verificado { get; set; }
    }
}

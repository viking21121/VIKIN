using System.Globalization;
using System.Text.Json;
using VikingMobile.Application.Activities;
using VikingMobile.Application.Communication;

namespace VikingMobile.Application.Admin;

/// <summary>
/// Orquesta el acceso de un administrador a los datos de su
/// establecimiento (Modo Administrador). Traduce el contrato real de
/// Viking PC 1.0.2 al modelo que consumen las páginas de Mobile.
/// </summary>
public class AdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICommunicationChannel _communicationChannel;

    public AdminService(ICommunicationChannel communicationChannel) =>
        _communicationChannel = communicationChannel;

    // =====================================================================
    // IDENTIDAD Y DASHBOARD
    // =====================================================================

    public async Task<AdminDashboardResponse> ValidarIdentidadAsync(string usuario)
    {
        try
        {
            if (ActividadContexto.ActividadId is null or 0)
                return Fallo("Selecciona una actividad en Configuración antes de continuar.");

            string json = JsonSerializer.Serialize(new
            {
                usuario = usuario.Trim(),
                actividadId = ActividadContexto.ActividadId.Value.ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/validar", json);

            var pc = JsonSerializer.Deserialize<DashboardPc>(text, JsonOptions);
            return pc == null ? Fallo("Respuesta inválida del servidor.") : Mapear(pc);
        }
        catch (Exception ex)
        {
            return Fallo($"No se pudo conectar con Viking PC: {ex.Message}");
        }
    }

    public async Task<AdminDashboardResponse> IniciarSesionAsync(string usuario)
    {
        usuario = usuario.Trim();

        try
        {
            var ident = await ValidarIdentidadAsync(usuario);
            if (!ident.Ok)
                return ident;

            string json = JsonSerializer.Serialize(new
            {
                usuario,
                actividadId = ident.ActividadId.ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/dashboard", json);

            var pc = JsonSerializer.Deserialize<DashboardPc>(text, JsonOptions);
            return pc == null ? Fallo("Respuesta inválida del servidor.") : Mapear(pc);
        }
        catch (Exception ex)
        {
            return Fallo($"No se pudo conectar con Viking PC: {ex.Message}");
        }
    }

    public Task<AdminDashboardResponse> RefrescarAsync()
    {
        if (!AdminSession.EstaActiva)
            return Task.FromResult(Fallo("No hay una sesión activa."));

        return IniciarSesionAsync(AdminSession.Usuario);
    }

    // =====================================================================
    // ALUMNOS
    // =====================================================================

    public async Task<(bool Ok, string Mensaje, List<AlumnoAdminResumen> Alumnos)> ObtenerAlumnosAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/alumnos", json);
            var pc = JsonSerializer.Deserialize<AlumnosPc>(text, JsonOptions);

            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudieron obtener los alumnos.", new());

            return (true, pc.Mensaje, pc.Alumnos ?? new());
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", new());
        }
    }

    public async Task<(bool Ok, string Mensaje, List<PagoAdminResumen> Pagos)> ObtenerPagosAlumnoAsync(string codigoAlumno)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                codigoAlumno
            });

            string text = await _communicationChannel.SendAsync("api/admin/alumnos/pagos", json);
            var pc = JsonSerializer.Deserialize<PagosAlumnoPc>(text, JsonOptions);

            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudieron obtener los pagos.", new());

            return (true, pc.Mensaje, pc.Pagos ?? new());
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", new());
        }
    }

    public async Task<RegistrarPagoResultado> RegistrarPagoAsync(
        string codigoAlumno,
        decimal montoUsd,
        decimal tasaBcv,
        string metodoPago,
        string referencia)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                codigoAlumno,
                montoUsd = montoUsd.ToString(CultureInfo.InvariantCulture),
                tasaBcv = tasaBcv.ToString(CultureInfo.InvariantCulture),
                metodoPago,
                referencia
            });

            string text = await _communicationChannel.SendAsync("api/admin/pagos/registrar", json);

            var resultado = JsonSerializer.Deserialize<RegistrarPagoResultado>(text, JsonOptions);
            return resultado ?? new RegistrarPagoResultado { Ok = false, Mensaje = "Respuesta inválida del servidor." };
        }
        catch (Exception ex)
        {
            return new RegistrarPagoResultado { Ok = false, Mensaje = $"No se pudo conectar con Viking PC: {ex.Message}" };
        }
    }

    public async Task<string?> ObtenerReciboHtmlAsync(string codigoAlumno, string folio)
    {
        if (string.IsNullOrWhiteSpace(codigoAlumno) || string.IsNullOrWhiteSpace(folio))
            return null;

        if (!_communicationChannel.IsConnected)
            return null;

        int actividadId = ActividadContexto.ActividadId.GetValueOrDefault();

        string endpoint =
            $"api/cliente/recibo?codigoAlumno={Uri.EscapeDataString(codigoAlumno)}" +
            $"&folio={Uri.EscapeDataString(folio)}" +
            $"&actividadId={actividadId}";

        return await _communicationChannel.GetAsync(endpoint);
    }

    // =====================================================================
    // REPORTES
    // =====================================================================

    public async Task<ReportesAdminResponse> ObtenerReportesAsync(int anio, int mes)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                anio = anio.ToString(),
                mes = mes.ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/reportes", json);

            var reportes = JsonSerializer.Deserialize<ReportesAdminResponse>(text, JsonOptions);
            return reportes ?? new ReportesAdminResponse { Ok = false, Mensaje = "Respuesta inválida del servidor." };
        }
        catch (Exception ex)
        {
            return new ReportesAdminResponse { Ok = false, Mensaje = $"No se pudo conectar con Viking PC: {ex.Message}" };
        }
    }

    // =====================================================================
    // SOLICITUDES DE PAGO (crear, listar pendientes, ver comprobante,
    // aprobar, rechazar)
    // =====================================================================

    public async Task<(bool Ok, string Mensaje, long SolicitudId)> SolicitarPagoAsync(
        string codigoAlumno, decimal montoUsd, string concepto)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                codigoAlumno,
                montoUsd = montoUsd.ToString(CultureInfo.InvariantCulture),
                concepto
            });

            string text = await _communicationChannel.SendAsync("api/admin/pagos/solicitar", json);

            var pc = JsonSerializer.Deserialize<SolicitarPagoPc>(text, JsonOptions);
            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudo crear la solicitud.", 0);

            return (true, pc.Mensaje, pc.SolicitudId);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", 0);
        }
    }

    public async Task<(bool Ok, string Mensaje, List<SolicitudPagoAdminResumen> Solicitudes)> ObtenerPagosPendientesAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/pagos/pendientes", json);

            var pc = JsonSerializer.Deserialize<SolicitudesPendientesPc>(text, JsonOptions);
            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudieron obtener las solicitudes.", new());

            return (true, pc.Mensaje, pc.Solicitudes ?? new());
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", new());
        }
    }

    public async Task<(bool Ok, string Mensaje, byte[]? Imagen, string ContentType)> ObtenerComprobantePagoAsync(long solicitudId)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                solicitudId = solicitudId.ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/pagos/comprobante", json);

            var pc = JsonSerializer.Deserialize<ComprobantePc>(text, JsonOptions);
            if (pc?.Ok != true || string.IsNullOrWhiteSpace(pc.ComprobanteBase64))
                return (false, pc?.Mensaje ?? "No se pudo obtener el comprobante.", null, "");

            byte[] bytes = Convert.FromBase64String(pc.ComprobanteBase64);
            return (true, pc.Mensaje, bytes, string.IsNullOrWhiteSpace(pc.ContentType) ? "image/png" : pc.ContentType);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", null, "");
        }
    }

    public async Task<(bool Ok, string Mensaje, string Folio)> AprobarPagoAsync(
        long solicitudId, string metodoPago, decimal tasaBcv, string referencia)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                solicitudId = solicitudId.ToString(),
                metodoPago,
                tasaBcv = tasaBcv.ToString(CultureInfo.InvariantCulture),
                referencia
            });

            string text = await _communicationChannel.SendAsync("api/admin/pagos/aprobar", json);

            var pc = JsonSerializer.Deserialize<AprobarPagoPc>(text, JsonOptions);
            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudo aprobar el pago.", "");

            return (true, pc.Mensaje, pc.Folio);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", "");
        }
    }

    public async Task<(bool Ok, string Mensaje)> RechazarPagoAsync(long solicitudId, string comentario)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                solicitudId = solicitudId.ToString(),
                comentario
            });

            string text = await _communicationChannel.SendAsync("api/admin/pagos/rechazar", json);

            var pc = JsonSerializer.Deserialize<OkMensajePc>(text, JsonOptions);
            return pc == null
                ? (false, "Respuesta inválida del servidor.")
                : (pc.Ok, pc.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}");
        }
    }

    // =====================================================================
    // MÉTODOS DE PAGO DEL ESTABLECIMIENTO (dónde le pueden pagar al alumno)
    // =====================================================================

    public async Task<(bool Ok, string Mensaje, List<MetodoPagoAdminResumen> Metodos)> ObtenerMetodosPagoAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/metodos-pago/obtener", json);

            var pc = JsonSerializer.Deserialize<MetodosPagoPc>(text, JsonOptions);
            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudieron obtener los métodos de pago.", new());

            return (true, pc.Mensaje, pc.Metodos ?? new());
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", new());
        }
    }

    public async Task<(bool Ok, string Mensaje)> GuardarMetodosPagoAsync(List<MetodoPagoAdminResumen> metodos)
    {
        try
        {
            var campos = new Dictionary<string, string>
            {
                ["usuario"] = AdminSession.Usuario,
                ["actividadId"] = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            };

            // Cada método viaja como un par de campos planos
            // (datos_<clave> / activo_<clave>): el servidor solo admite
            // valores de texto, no listas anidadas.
            foreach (var metodo in metodos)
            {
                campos[$"datos_{metodo.Clave}"] = metodo.Datos ?? "";
                campos[$"activo_{metodo.Clave}"] = metodo.Activo ? "true" : "false";
            }

            string json = JsonSerializer.Serialize(campos);

            string text = await _communicationChannel.SendAsync("api/admin/metodos-pago/guardar", json);

            var pc = JsonSerializer.Deserialize<OkMensajePc>(text, JsonOptions);
            return pc == null
                ? (false, "Respuesta inválida del servidor.")
                : (pc.Ok, pc.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}");
        }
    }

    // =====================================================================
    // TASA BCV AUTOMÁTICA (opcional: si falla, se escribe a mano)
    // =====================================================================

    public async Task<(bool Ok, decimal Tasa)> ObtenerTasaBcvAsync()
    {
        try
        {
            string text = await _communicationChannel.GetAsync("api/publico/tasa-bcv");
            var pc = JsonSerializer.Deserialize<TasaBcvPc>(text, JsonOptions);

            return pc?.Ok == true && pc.Tasa > 0
                ? (true, pc.Tasa)
                : (false, 0);
        }
        catch
        {
            return (false, 0);
        }
    }

    // =====================================================================
    // LOGO DEL ESTABLECIMIENTO
    // =====================================================================

    public async Task<(bool Ok, string Mensaje)> GuardarLogoAsync(byte[] imagen, string contentType)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                logoBase64 = Convert.ToBase64String(imagen),
                contentType
            });

            string text = await _communicationChannel.SendAsync("api/admin/logo/guardar", json);
            var pc = JsonSerializer.Deserialize<OkMensajePc>(text, JsonOptions);

            return pc == null
                ? (false, "Respuesta inválida del servidor.")
                : (pc.Ok, pc.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}");
        }
    }

    public async Task<byte[]?> ObtenerLogoPropioAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                usuario = AdminSession.Usuario,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            });

            string text = await _communicationChannel.SendAsync("api/admin/logo/obtener", json);
            var pc = JsonSerializer.Deserialize<LogoPc>(text, JsonOptions);

            return pc?.Ok == true && !string.IsNullOrWhiteSpace(pc.LogoBase64)
                ? Convert.FromBase64String(pc.LogoBase64)
                : null;
        }
        catch
        {
            return null;
        }
    }

    // =====================================================================
    // TRADUCCIÓN DEL CONTRATO REAL DE VIKING PC
    // =====================================================================

    private static AdminDashboardResponse Mapear(DashboardPc pc) => new()
    {
        Ok = pc.Ok,
        Mensaje = pc.Mensaje,
        Establecimiento = !string.IsNullOrWhiteSpace(pc.NombreEstablecimiento)
            ? pc.NombreEstablecimiento
            : pc.Nombre,
        ActividadId = pc.ActividadId,
        TotalAlumnos = pc.TotalAlumnos > 0 ? pc.TotalAlumnos : pc.Alumnos,
        Activos = pc.Activos,
        PorVencer = pc.PorVencer,
        Morosos = pc.Morosos,
        RecaudadoMes = pc.RecaudadoMes > 0 ? pc.RecaudadoMes : pc.Recaudado
    };

    private static AdminDashboardResponse Fallo(string mensaje) =>
        new() { Ok = false, Mensaje = mensaje };

    // ── Contrato real de Viking PC 1.0.2 ────────────────────────────────
    private sealed class DashboardPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string NombreEstablecimiento { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int ActividadId { get; set; }
        public int TotalAlumnos { get; set; }
        public int Alumnos { get; set; }
        public int Activos { get; set; }
        public int PorVencer { get; set; }
        public int Morosos { get; set; }
        public decimal RecaudadoMes { get; set; }
        public decimal Recaudado { get; set; }
    }

    private sealed class AlumnosPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public List<AlumnoAdminResumen>? Alumnos { get; set; }
    }

    private sealed class PagosAlumnoPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public List<PagoAdminResumen>? Pagos { get; set; }
    }

    private sealed class SolicitarPagoPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public long SolicitudId { get; set; }
    }

    private sealed class SolicitudesPendientesPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public List<SolicitudPagoAdminResumen>? Solicitudes { get; set; }
    }

    private sealed class ComprobantePc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string ComprobanteBase64 { get; set; } = "";
        public string ContentType { get; set; } = "";
    }

    private sealed class AprobarPagoPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string Folio { get; set; } = "";
    }

    private sealed class OkMensajePc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
    }

    private sealed class MetodosPagoPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public List<MetodoPagoAdminResumen>? Metodos { get; set; }
    }

    private sealed class TasaBcvPc
    {
        public bool Ok { get; set; }
        public decimal Tasa { get; set; }
    }

    private sealed class LogoPc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string LogoBase64 { get; set; } = "";
        public string ContentType { get; set; } = "";
    }
}

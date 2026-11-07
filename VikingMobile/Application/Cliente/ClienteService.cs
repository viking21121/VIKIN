using System.Globalization;
using System.Text.Json;
using VikingMobile.Application.Activities;
using VikingMobile.Application.Communication;

namespace VikingMobile.Application.Cliente;

/// <summary>
/// Orquesta el acceso de un alumno a su propia cuenta (Modo Cliente).
/// Habla dos "idiomas": el contrato real que expone Viking PC 1.0.2
/// (clases <see cref="CuentaClientePc"/>/<see cref="PagoPc"/>/
/// <see cref="AsistenciaPc"/>, con los nombres de campo exactos que
/// envía el servidor) y el modelo que ya consumen las páginas de
/// Mobile (<see cref="CuentaClienteResponse"/>, sin cambios). Toda la
/// traducción entre ambos vive aquí, así que Viking PC no necesita
/// tocarse y las páginas tampoco.
/// </summary>
public class ClienteService
{
    private const string CuentaCacheKeyPrefix = "viking_cuenta_cliente_";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] MesesEs =
    {
        "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    private readonly ICommunicationChannel _communicationChannel;

    public ClienteService(ICommunicationChannel communicationChannel) =>
        _communicationChannel = communicationChannel;

    public async Task<CuentaClienteResponse> IniciarSesionAsync(string codigoAlumno)
    {
        codigoAlumno = codigoAlumno.Trim().ToUpperInvariant();

        try
        {
            if (!_communicationChannel.IsConnected)
                return FalloConMensaje("Conecta Viking Mobile con Viking PC para validar tu cuenta.");

            if (ActividadContexto.ActividadId is null or 0)
                return FalloConMensaje("Selecciona una actividad en Configuración antes de continuar.");

            string actividadId = ActividadContexto.ActividadId.Value.ToString();

            string validar = JsonSerializer.Serialize(new { codigoAlumno, actividadId });
            string identidadJson = await _communicationChannel.SendAsync("api/cliente/validar", validar);
            var identidad = JsonSerializer.Deserialize<IdentidadResponse>(identidadJson, JsonOptions);

            if (identidad?.Ok != true)
                return FalloConMensaje(identidad?.Mensaje ?? "El código no pertenece a un alumno activo.");

            string responseText = await _communicationChannel.SendAsync(
                "api/cliente/cuenta",
                JsonSerializer.Serialize(new { codigoAlumno, actividadId }));

            var pc = JsonSerializer.Deserialize<CuentaClientePc>(responseText, JsonOptions);
            if (pc == null)
                return FalloConMensaje("El servidor no devolvió una respuesta válida.");

            var cuenta = MapearCuenta(codigoAlumno, pc);

            if (cuenta.Ok)
                GuardarCuentaLocalmente(codigoAlumno, cuenta);

            return cuenta;
        }
        catch
        {
            var cacheada = ObtenerCuentaCacheada(codigoAlumno);
            if (cacheada != null)
            {
                cacheada.DesdeCache = true;
                return cacheada;
            }

            return FalloConMensaje("No se pudo conectar con Viking PC.");
        }
    }

    public async Task<string?> ObtenerReciboHtmlAsync(ReciboResumen recibo)
    {
        if (recibo == null || string.IsNullOrWhiteSpace(recibo.Folio))
            return null;

        if (!_communicationChannel.IsConnected)
            return null;

        string codigoAlumno = ClienteSession.CodigoAlumno;
        int actividadId = ActividadContexto.ActividadId ?? 0;

        string endpoint =
            $"api/cliente/recibo?codigoAlumno={Uri.EscapeDataString(codigoAlumno)}" +
            $"&folio={Uri.EscapeDataString(recibo.Folio)}" +
            $"&actividadId={actividadId}";

        return await _communicationChannel.GetAsync(endpoint);
    }

    public Task<CuentaClienteResponse> RefrescarAsync()
    {
        if (!ClienteSession.EstaActiva)
            return Task.FromResult(FalloConMensaje("No hay una sesión activa."));

        return IniciarSesionAsync(ClienteSession.CodigoAlumno);
    }

    // =====================================================================
    // SOLICITUDES DE PAGO (ver las mías, subir comprobante)
    // =====================================================================

    public async Task<(bool Ok, string Mensaje, List<SolicitudPagoClienteResumen> Solicitudes)> ObtenerPagosPendientesAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                codigoAlumno = ClienteSession.CodigoAlumno,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            });

            string text = await _communicationChannel.SendAsync("api/cliente/pagos/pendientes", json);

            var pc = JsonSerializer.Deserialize<SolicitudesPendientesClientePc>(text, JsonOptions);
            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudieron obtener tus pagos pendientes.", new());

            return (true, pc.Mensaje, pc.Solicitudes ?? new());
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", new());
        }
    }

    public async Task<(bool Ok, string Mensaje)> SubirComprobanteAsync(
        long solicitudId, byte[] imagen, string contentType)
    {
        try
        {
            if (imagen.Length == 0)
                return (false, "No se seleccionó ninguna imagen.");

            string json = JsonSerializer.Serialize(new
            {
                codigoAlumno = ClienteSession.CodigoAlumno,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                solicitudId = solicitudId.ToString(),
                comprobanteBase64 = Convert.ToBase64String(imagen),
                contentType
            });

            string text = await _communicationChannel.SendAsync("api/cliente/pagos/comprobante", json);

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
    // MÉTODOS DE PAGO Y PAGO INICIADO POR EL ALUMNO (sin esperar al
    // administrador: elige método, ve los datos, sube su comprobante)
    // =====================================================================

    public async Task<(bool Ok, string Mensaje, List<MetodoPagoClienteResumen> Metodos)> ObtenerMetodosPagoAsync()
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString()
            });

            string text = await _communicationChannel.SendAsync("api/cliente/metodos-pago", json);

            var pc = JsonSerializer.Deserialize<MetodosPagoClientePc>(text, JsonOptions);
            if (pc?.Ok != true)
                return (false, pc?.Mensaje ?? "No se pudieron obtener los métodos de pago.", new());

            return (true, pc.Mensaje, pc.Metodos ?? new());
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo conectar con Viking PC: {ex.Message}", new());
        }
    }

    public async Task<(bool Ok, string Mensaje)> CrearPagoAsync(
        decimal montoUsd, string concepto, string metodoPago, byte[] imagen, string contentType)
    {
        try
        {
            if (imagen.Length == 0)
                return (false, "Debes adjuntar el comprobante de tu pago.");

            string json = JsonSerializer.Serialize(new
            {
                codigoAlumno = ClienteSession.CodigoAlumno,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                montoUsd = montoUsd.ToString(CultureInfo.InvariantCulture),
                concepto,
                metodoPago,
                comprobanteBase64 = Convert.ToBase64String(imagen),
                contentType
            });

            string text = await _communicationChannel.SendAsync("api/cliente/pagos/crear", json);

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

    public async Task<(bool Ok, string Mensaje)> SubirFotoAlumnoAsync(string codigoAlumno, byte[] fotoBytes)
    {
        try
        {
            if (fotoBytes.Length == 0)
                return (false, "No se seleccionó ninguna imagen.");

            string json = JsonSerializer.Serialize(new
            {
                codigoAlumno,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                fotoBase64 = Convert.ToBase64String(fotoBytes)
            });

            string text = await _communicationChannel.SendAsync($"api/alumnos/{Uri.EscapeDataString(codigoAlumno)}/foto", json);
            var pc = JsonSerializer.Deserialize<OkMensajePc>(text, JsonOptions);
            return pc == null
                ? (false, "Respuesta inválida del servidor.")
                : (pc.Ok, pc.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudo subir la foto: {ex.Message}");
        }
    }

    public async Task<(bool Ok, string Mensaje)> ActualizarDatosAlumnoAsync(string codigoAlumno, string cedula, string telefono)
    {
        try
        {
            string json = JsonSerializer.Serialize(new
            {
                codigoAlumno,
                actividadId = ActividadContexto.ActividadId.GetValueOrDefault().ToString(),
                cedula,
                telefono
            });

            string text = await _communicationChannel.SendAsync($"api/alumnos/{Uri.EscapeDataString(codigoAlumno)}/datos", json);
            var pc = JsonSerializer.Deserialize<OkMensajePc>(text, JsonOptions);
            return pc == null
                ? (false, "Respuesta inválida del servidor.")
                : (pc.Ok, pc.Mensaje);
        }
        catch (Exception ex)
        {
            return (false, $"No se pudieron actualizar los datos: {ex.Message}");
        }
    }

    // =====================================================================
    // TRADUCCIÓN DEL CONTRATO REAL DE VIKING PC AL MODELO DE MOBILE
    // =====================================================================

    private static CuentaClienteResponse MapearCuenta(string codigoAlumno, CuentaClientePc pc)
    {
        if (!pc.Ok)
            return FalloConMensaje(pc.Mensaje);

        DateTime? vencimiento = pc.FechaVencimientoPlan ?? pc.FechaVencimiento;

        return new CuentaClienteResponse
        {
            Ok = true,
            Mensaje = pc.Mensaje,
            CodigoAlumno = string.IsNullOrWhiteSpace(pc.CodigoAlumno) ? codigoAlumno : pc.CodigoAlumno,
            NombreAlumno = !string.IsNullOrWhiteSpace(pc.NombreAlumno) ? pc.NombreAlumno : pc.Nombre,
            // Viking PC no devuelve el nombre de la actividad en /api/cliente/cuenta:
            // Mobile ya lo conoce localmente porque el alumno la seleccionó en Configuración.
            Actividad = ActividadContexto.NombreActividad,
            Plan = pc.PlanNombre,
            Estado = ClasificarEstadoMembresia(vencimiento),
            FechaVencimiento = FormatearFecha(vencimiento),
            Cedula = pc.Cedula,
            Telefono = pc.Telefono,
            FotoRuta = pc.FotoRuta,
            // "Restantes" solo tiene sentido cuando el plan controla por número de clases.
            ClasesRestantes = pc.ControlaClases ? pc.Restantes : null,
            SaldoPendiente = pc.MontoPendiente,
            Recibos = (pc.Recibos ?? new())
                .Select(p => new ReciboResumen
                {
                    Folio = p.Folio,
                    Fecha = FormatearFecha(p.FechaPago),
                    Monto = p.MontoUsd
                })
                .ToList(),
            Asistencias = AgruparAsistenciasPorMes(pc.Asistencias ?? new())
        };
    }

    /// <summary>
    /// Activo / por vencer / moroso, con la misma ventana de 7 días que
    /// usa Viking PC para clasificar a los alumnos en el dashboard de
    /// administrador (api/admin/dashboard), para que ambas vistas
    /// siempre coincidan sobre el mismo alumno.
    /// </summary>
    private static string ClasificarEstadoMembresia(DateTime? fechaVencimiento)
    {
        if (fechaVencimiento is null)
            return "activo";

        int dias = (fechaVencimiento.Value.Date - DateTime.Today).Days;

        if (dias < 0)
            return "moroso";

        if (dias <= 7)
            return "por_vencer";

        return "activo";
    }

    private static List<AsistenciaMensual> AgruparAsistenciasPorMes(List<AsistenciaPc> registros)
    {
        var fechas = new List<DateTime>();

        foreach (var registro in registros)
        {
            string texto = !string.IsNullOrWhiteSpace(registro.Fecha)
                ? registro.Fecha
                : registro.FechaAsistencia;

            if (DateTime.TryParse(
                    texto,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fecha))
            {
                fechas.Add(fecha.Date);
            }
        }

        return fechas
            .GroupBy(f => new { f.Year, f.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new AsistenciaMensual
            {
                Mes = $"{MesesEs[g.Key.Month]} {g.Key.Year}",
                Cantidad = g.Count()
            })
            .ToList();
    }

    private static string FormatearFecha(DateTime? fecha) =>
        fecha?.ToString("dd/MM/yyyy") ?? "";

    // =====================================================================
    // CACHÉ LOCAL (SOLO CONSULTA SIN CONEXIÓN)
    // =====================================================================

    private static CuentaClienteResponse? ObtenerCuentaCacheada(string codigoAlumno)
    {
        try
        {
            string json = Preferences.Get(ClaveCache(codigoAlumno), "");
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<CuentaClienteResponse>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void GuardarCuentaLocalmente(string codigoAlumno, CuentaClienteResponse cuenta)
    {
        try
        {
            Preferences.Set(ClaveCache(codigoAlumno), JsonSerializer.Serialize(cuenta));
        }
        catch
        {
        }
    }

    private static string ClaveCache(string codigoAlumno) =>
        $"{CuentaCacheKeyPrefix}{codigoAlumno.Trim().ToUpperInvariant()}";

    private static CuentaClienteResponse FalloConMensaje(string mensaje) =>
        new() { Ok = false, Mensaje = mensaje };

    private sealed class IdentidadResponse
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
    }

    // ── Contrato real de Viking PC 1.0.2 (api/cliente/cuenta) ──────────
    // Nombres de propiedad iguales, letra por letra (salvo mayúsculas),
    // a las claves JSON que envía VikingMobileApiServer.ProcesarClienteCuentaAsync.
    private sealed class CuentaClientePc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string NombreAlumno { get; set; } = "";
        public string CodigoAlumno { get; set; } = "";
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string? FotoRuta { get; set; }
        public int ActividadId { get; set; }
        public string PlanNombre { get; set; } = "";
        public string Estado { get; set; } = "";
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaVencimientoPlan { get; set; }
        public int DiasRestantes { get; set; }
        public decimal CuotaTotal { get; set; }
        public decimal MontoAbonado { get; set; }
        public decimal MontoPendiente { get; set; }
        public int ClasesMes { get; set; }
        public int Tomadas { get; set; }
        public int Restantes { get; set; }
        public bool ControlaClases { get; set; }
        public List<AsistenciaPc> Asistencias { get; set; } = new();
        public List<PagoPc> Recibos { get; set; } = new();
    }

    private sealed class AsistenciaPc
    {
        public string Fecha { get; set; } = "";
        public string FechaAsistencia { get; set; } = "";
    }

    private sealed class PagoPc
    {
        public string Folio { get; set; } = "";
        public DateTime FechaPago { get; set; }
        public decimal MontoUsd { get; set; }
    }

    private sealed class SolicitudesPendientesClientePc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public List<SolicitudPagoClienteResumen>? Solicitudes { get; set; }
    }

    private sealed class OkMensajePc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
    }

    private sealed class MetodosPagoClientePc
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public List<MetodoPagoClienteResumen>? Metodos { get; set; }
    }
}

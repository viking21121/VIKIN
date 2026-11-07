using AsistenciaNatacion.Application.Mobile;
using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Infrastructure.SQLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.Mobile
{
    /// <summary>
    /// API HTTP local para Viking Mobile.
    /// Mantiene los endpoints existentes de asistencia y actividades y añade
    /// los endpoints de identidad, cuenta, dashboard y recibos usados por
    /// las versiones actuales de Viking Mobile.
    /// </summary>
    public sealed class VikingMobileApiServer : IDisposable
    {
        private readonly HttpListener listener = new();
        private readonly IRegistrarAsistenciaMovil registrarAsistencia;
        private readonly Func<List<AsistenciaNatacion.Domain.Actividad>> obtenerActividades;
        private CancellationTokenSource? cancellation;
        private Task? serverTask;

        public int Port { get; }
        public bool EstaEjecutando { get; private set; }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private static readonly HttpClient TasaHttpClient = new() { Timeout = TimeSpan.FromSeconds(6) };
        private static decimal _tasaBcvCache;
        private static DateTime _tasaBcvCacheExpira = DateTime.MinValue;

        /// <summary>
        /// Registro simple de errores del servidor Mobile a un archivo de
        /// texto junto a la base de datos. Antes, cualquier fallo en un
        /// endpoint solo se veía en la respuesta al celular; esto deja
        /// rastro también en el PC para diagnosticar sin depender de que
        /// alguien capture pantalla del error en Mobile.
        /// </summary>
        private static void RegistrarErrorServidor(string ruta, Exception ex)
        {
            try
            {
                string archivo = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "viking_mobile_errores.log");

                string texto =
                    $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} — {ruta} ===\n{ex}\n\n";

                File.AppendAllText(archivo, texto);
            }
            catch
            {
                // El registro de errores nunca debe tumbar el servidor.
            }
        }

        public VikingMobileApiServer(
            IRegistrarAsistenciaMovil registrarAsistencia,
            Func<List<AsistenciaNatacion.Domain.Actividad>> obtenerActividades,
            int port = 5050)
        {
            this.registrarAsistencia = registrarAsistencia
                ?? throw new ArgumentNullException(nameof(registrarAsistencia));
            this.obtenerActividades = obtenerActividades
                ?? throw new ArgumentNullException(nameof(obtenerActividades));

            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));

            Port = port;
            listener.Prefixes.Add($"http://+:{Port}/");
        }

        public void Iniciar()
        {
            if (EstaEjecutando)
                return;

            cancellation = new CancellationTokenSource();

            try
            {
                listener.Start();
            }
            catch
            {
                cancellation.Dispose();
                cancellation = null;
                throw;
            }

            EstaEjecutando = true;
            serverTask = Task.Run(() => EscucharAsync(cancellation.Token));
        }

        public async Task DetenerAsync()
        {
            if (!EstaEjecutando)
                return;

            try
            {
                cancellation?.Cancel();
                listener.Stop();

                if (serverTask != null)
                    await serverTask;
            }
            catch
            {
            }
            finally
            {
                EstaEjecutando = false;
                cancellation?.Dispose();
                cancellation = null;
                serverTask = null;
            }
        }

        private async Task EscucharAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext? context = null;

                try
                {
                    context = await listener.GetContextAsync();
                    _ = Task.Run(() => ProcesarAsync(context), token);
                }
                catch (HttpListenerException)
                {
                    if (token.IsCancellationRequested)
                        return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
            }
        }

        private async Task ProcesarAsync(HttpListenerContext context)
        {
            try
            {
                ConfigurarCors(context.Response);

                string path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? "";
                string method = context.Request.HttpMethod.ToUpperInvariant();

                if (method == "OPTIONS")
                {
                    context.Response.StatusCode = 204;
                    return;
                }

                if (method == "GET" && path == "/api/viking/health")
                {
                    await EscribirJsonAsync(context.Response, 200, new
                    {
                        ok = true,
                        version = typeof(VikingMobileApiServer).Assembly
                            .GetName().Version?.ToString(3) ?? "desconocida",
                        servicio = "Viking Mobile API"
                    });
                    return;
                }

                if (method == "GET" && path == "/api/actividades")
                {
                    await ProcesarActividadesAsync(context);
                    return;
                }

                if (method == "POST" &&
                    (path == "/api/asistencia/registrar" ||
                     path == "/api/asistencia/sincronizar"))
                {
                    await ProcesarAsistenciaAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/cliente/validar")
                {
                    await ProcesarClienteValidarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/cliente/cuenta")
                {
                    await ProcesarClienteCuentaAsync(context);
                    return;
                }

                if (method == "GET" && path == "/api/cliente/recibo")
                {
                    await ProcesarClienteReciboAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/validar")
                {
                    await ProcesarAdminValidarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/dashboard")
                {
                    await ProcesarAdminDashboardAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/alumnos")
                {
                    await ProcesarAdminAlumnosAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/alumnos/pagos")
                {
                    await ProcesarAdminAlumnoPagosAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/pagos/registrar")
                {
                    await ProcesarAdminPagoRegistrarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/reportes")
                {
                    await ProcesarAdminReportesAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/pagos/solicitar")
                {
                    await ProcesarAdminPagoSolicitarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/pagos/pendientes")
                {
                    await ProcesarAdminPagosPendientesAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/pagos/comprobante")
                {
                    await ProcesarAdminPagoComprobanteAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/pagos/aprobar")
                {
                    await ProcesarAdminPagoAprobarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/pagos/rechazar")
                {
                    await ProcesarAdminPagoRechazarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/cliente/pagos/pendientes")
                {
                    await ProcesarClientePagosPendientesAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/cliente/pagos/comprobante")
                {
                    await ProcesarClientePagoComprobanteAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/cliente/pagos/crear")
                {
                    await ProcesarClientePagoCrearAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/metodos-pago/obtener")
                {
                    await ProcesarAdminMetodosPagoObtenerAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/metodos-pago/guardar")
                {
                    await ProcesarAdminMetodosPagoGuardarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/cliente/metodos-pago")
                {
                    await ProcesarClienteMetodosPagoAsync(context);
                    return;
                }

                if (method == "GET" && path == "/api/publico/tasa-bcv")
                {
                    await ProcesarTasaBcvAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/logo/guardar")
                {
                    await ProcesarAdminLogoGuardarAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/admin/logo/obtener")
                {
                    await ProcesarAdminLogoObtenerAsync(context);
                    return;
                }

                if (method == "POST" && path == "/api/publico/logo")
                {
                    await ProcesarPublicoLogoAsync(context);
                    return;
                }

                if (method == "POST" && (path.StartsWith("/api/alumnos/") && path.EndsWith("/foto") || path == "/api/alumnos/foto"))
                {
                    await ProcesarAlumnoSubirFotoAsync(context, path);
                    return;
                }

                if (method == "GET" && (path.StartsWith("/api/alumnos/") && path.EndsWith("/foto") || path == "/api/alumnos/foto"))
                {
                    await ProcesarAlumnoObtenerFotoAsync(context, path);
                    return;
                }

                if (method == "POST" && (path.StartsWith("/api/alumnos/") && path.EndsWith("/datos") || path == "/api/alumnos/datos"))
                {
                    await ProcesarAlumnoActualizarDatosAsync(context, path);
                    return;
                }

                if (method == "GET" && (path.StartsWith("/api/alumnos/") && path.EndsWith("/carnet") || path == "/api/alumnos/carnet"))
                {
                    await ProcesarAlumnoObtenerCarnetAsync(context, path);
                    return;
                }

                await EscribirJsonAsync(context.Response, 404, new
                {
                    ok = false,
                    mensaje = "Endpoint no encontrado."
                });
            }
            catch (Exception ex)
            {
                RegistrarErrorServidor(context.Request.Url?.AbsolutePath ?? "(desconocida)", ex);

                try
                {
                    await EscribirJsonAsync(context.Response, 500, new
                    {
                        ok = false,
                        mensaje = "Error interno del servidor Viking.",
                        detalle = ex.Message
                    });
                }
                catch
                {
                }
            }
            finally
            {
                try { context.Response.Close(); } catch { }
            }
        }

        private async Task ProcesarActividadesAsync(HttpListenerContext context)
        {
            var actividades = obtenerActividades();

            await EscribirJsonAsync(context.Response, 200, actividades);
        }

        private async Task ProcesarAsistenciaAsync(HttpListenerContext context)
        {
            if (context.Request.ContentType == null ||
                !context.Request.ContentType.StartsWith(
                    "application/json", StringComparison.OrdinalIgnoreCase))
            {
                await EscribirJsonAsync(context.Response, 415, new
                {
                    ok = false,
                    mensaje = "Content-Type debe ser application/json."
                });
                return;
            }

            string body = await LeerBodyAsync(context);

            MobileAttendanceRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<MobileAttendanceRequest>(
                    body, JsonOptions);
            }
            catch (JsonException)
            {
                await EscribirJsonAsync(context.Response, 400, new
                {
                    ok = false,
                    mensaje = "JSON inválido."
                });
                return;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.CodigoAlumno))
            {
                await EscribirJsonAsync(context.Response, 400, new
                {
                    ok = false,
                    mensaje = "CodigoAlumno es obligatorio."
                });
                return;
            }

            MobileAttendanceResponse result = registrarAsistencia.Ejecutar(request);
            await EscribirJsonAsync(context.Response, result.Ok ? 200 : 422, result);
        }

        private async Task ProcesarClienteValidarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string codigo = Obtener(data, "codigoAlumno", "codigo", "usuario");
            int actividadId = ObtenerInt(data, "actividadId");

            if (string.IsNullOrWhiteSpace(codigo))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El código del alumno es obligatorio."
                });
                return;
            }

            if (!ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La actividad seleccionada no está disponible."
                });
                return;
            }

            var alumno = new SqliteAlumnoRepository(Database.ObtenerConnectionString())
                .ObtenerPorActividad(actividadId)
                .FirstOrDefault(x =>
                    string.Equals(x.Codigo?.Trim(), codigo.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El código del alumno no coincide con Viking PC."
                });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Identidad validada correctamente.",
                nombre = alumno.Nombre,
                nombreAlumno = alumno.Nombre,
                codigoAlumno = alumno.Codigo,
                actividadId = alumno.ActividadId
            });
        }

        private async Task ProcesarClienteCuentaAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string codigo = Obtener(data, "codigoAlumno", "codigo", "usuario");
            int actividadId = ObtenerInt(data, "actividadId");

            if (string.IsNullOrWhiteSpace(codigo) || !ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "No se pudo validar el alumno o la actividad."
                });
                return;
            }

            var alumno = new SqliteAlumnoRepository(Database.ObtenerConnectionString())
                .ObtenerPorActividad(actividadId)
                .FirstOrDefault(x =>
                    string.Equals(x.Codigo?.Trim(), codigo.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado."
                });
                return;
            }

            int anio = DateTime.Today.Year;
            int mes = DateTime.Today.Month;

            var estado = AppServices.CrearObtenerEstadoAlumno()
                .Ejecutar(alumno.Id, anio, mes);

            var pagos = ObtenerPagos(alumno.Id);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Cuenta obtenida correctamente.",
                nombre = alumno.Nombre,
                nombreAlumno = alumno.Nombre,
                codigoAlumno = alumno.Codigo,
                cedula = alumno.Cedula ?? "",
                telefono = alumno.Telefono ?? "",
                fotoRuta = alumno.FotoRuta ?? "",
                tieneFoto = !string.IsNullOrWhiteSpace(alumno.FotoRuta),
                actividadId = alumno.ActividadId,
                planNombre = estado?.PlanNombre ?? "Sin plan",
                estado = estado?.Estado.ToString() ?? "",
                fechaInicioPlan = estado?.FechaInicioPlan,
                fechaVencimiento = estado?.FechaVencimientoPlan,
                fechaVencimientoPlan = estado?.FechaVencimientoPlan,
                diasRestantes = estado?.DiasRestantes ?? 0,
                cuotaTotal = estado?.CuotaTotal ?? 0m,
                montoAbonado = estado?.MontoAbonado ?? 0m,
                montoPendiente = Math.Max(
                    (estado?.CuotaTotal ?? 0m) - (estado?.MontoAbonado ?? 0m), 0m),
                clasesMes = estado?.ClasesMes ?? 0,
                tomadas = estado?.Tomadas ?? 0,
                restantes = estado?.Restantes ?? 0,
                controlaClases = estado?.ControlaClases ?? false,
                asistencias = ObtenerAsistencias(alumno.Id),
                pagos = pagos,
                recibos = pagos
            });
        }

        private async Task ProcesarAdminValidarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            if (string.IsNullOrWhiteSpace(usuario))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El nombre del establecimiento es obligatorio."
                });
                return;
            }

            if (!ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La actividad seleccionada no está disponible."
                });
                return;
            }

            var configuracion = AppServices.CrearObtenerConfiguracion()
                .Ejecutar(actividadId);

            if (string.IsNullOrWhiteSpace(configuracion.Nombre))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El establecimiento no tiene una configuración válida en Viking PC."
                });
                return;
            }

            bool coincide = Normalizar(configuracion.Nombre) == Normalizar(usuario);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = coincide,
                mensaje = coincide
                    ? "Identidad validada correctamente."
                    : "El nombre del establecimiento no coincide con Viking PC.",
                nombreEstablecimiento = configuracion.Nombre,
                nombre = configuracion.Nombre,
                actividadId
            });
        }

        private async Task ProcesarAdminDashboardAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            if (string.IsNullOrWhiteSpace(usuario) || !ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "No se pudo validar el establecimiento o la actividad."
                });
                return;
            }

            var configuracion = AppServices.CrearObtenerConfiguracion()
                .Ejecutar(actividadId);

            if (Normalizar(configuracion.Nombre) != Normalizar(usuario))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El nombre del establecimiento no coincide con Viking PC."
                });
                return;
            }

            var estados = AppServices.CrearObtenerDashboard()
                .Ejecutar(DateTime.Today.Year, DateTime.Today.Month, actividadId);

            int total = estados.Count;
            int activos = estados.Count(x =>
                x.FechaVencimientoPlan == null ||
                x.FechaVencimientoPlan.Value.Date >= DateTime.Today);

            int porVencer = estados.Count(x =>
                x.FechaVencimientoPlan.HasValue &&
                x.FechaVencimientoPlan.Value.Date >= DateTime.Today &&
                x.FechaVencimientoPlan.Value.Date <= DateTime.Today.AddDays(7));

            int morosos = estados.Count(x =>
                x.FechaVencimientoPlan.HasValue &&
                x.FechaVencimientoPlan.Value.Date < DateTime.Today);

            decimal recaudado = ObtenerRecaudadoMes(actividadId);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Dashboard obtenido correctamente.",
                nombreEstablecimiento = configuracion.Nombre,
                nombre = configuracion.Nombre,
                actividadId,
                totalAlumnos = total,
                alumnos = total,
                activos,
                porVencer,
                morosos,
                recaudadoMes = recaudado,
                recaudado = recaudado
            });
        }

        private async Task ProcesarAdminAlumnosAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var estados = AppServices.CrearObtenerDashboard()
                .Ejecutar(DateTime.Today.Year, DateTime.Today.Month, actividadId);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var alumnos = estados
                .OrderBy(x => x.Nombre)
                .Select(x =>
                {
                    bool fotoExiste = (!string.IsNullOrWhiteSpace(x.FotoRuta) && File.Exists(Path.Combine(baseDir, x.FotoRuta)))
                        || File.Exists(Path.Combine(baseDir, "fotos", $"{x.Codigo}.jpg"));
                    return new
                    {
                        alumnoId = x.AlumnoId,
                        codigo = x.Codigo,
                        nombre = x.Nombre,
                        cedula = x.Cedula ?? "",
                        telefono = x.Telefono ?? "",
                        fotoRuta = x.FotoRuta,
                        tieneFoto = fotoExiste,
                        fotoUrl = fotoExiste ? $"/api/alumnos/{x.Codigo}/foto" : null,
                        planNombre = x.PlanNombre,
                        controlaClases = x.ControlaClases,
                        clasesMes = x.ClasesMes,
                        tomadas = x.Tomadas,
                        restantes = x.Restantes,
                        fechaVencimientoPlan = x.FechaVencimientoPlan,
                        diasRestantes = x.DiasRestantes,
                        cuotaTotal = x.CuotaTotal,
                        montoAbonado = x.MontoAbonado,
                        montoPendiente = x.MontoRestante,
                        estadoMembresia = ClasificarEstadoMembresia(x.FechaVencimientoPlan)
                    };
                })
                .ToList();

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Alumnos obtenidos correctamente.",
                actividadId,
                alumnos
            });
        }

        private async Task ProcesarAdminAlumnoPagosAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");
            string codigo = Obtener(data, "codigoAlumno", "codigo");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var alumno = ResolverAlumno(actividadId, codigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado en esta actividad."
                });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Pagos obtenidos correctamente.",
                codigoAlumno = alumno.Codigo,
                nombreAlumno = alumno.Nombre,
                pagos = ObtenerPagos(alumno.Id)
            });
        }

        private async Task ProcesarAdminPagoRegistrarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");
            string codigo = Obtener(data, "codigoAlumno", "codigo");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var alumno = ResolverAlumno(actividadId, codigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado en esta actividad."
                });
                return;
            }

            if (!decimal.TryParse(
                    Obtener(data, "montoUsd", "monto"),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montoUsd))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El monto en dólares no es válido."
                });
                return;
            }

            if (!decimal.TryParse(
                    Obtener(data, "tasaBcv"),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tasaBcv))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La tasa BCV no es válida."
                });
                return;
            }

            string metodoPago = Obtener(data, "metodoPago");
            if (string.IsNullOrWhiteSpace(metodoPago))
                metodoPago = "Móvil";

            string referencia = Obtener(data, "referencia");
            if (string.IsNullOrWhiteSpace(referencia))
                referencia = $"MOBILE-{DateTime.Now:yyyyMMddHHmmssfff}";

            DateTime fechaPago = DateTime.TryParse(
                Obtener(data, "fechaPago"), CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime fp) ? fp : DateTime.Now;

            int anio = ObtenerInt(data, "anio");
            if (anio <= 0) anio = fechaPago.Year;

            int mes = ObtenerInt(data, "mes");
            if (mes is <= 0 or > 12) mes = fechaPago.Month;

            decimal montoBs = Math.Round(montoUsd * tasaBcv, 2);

            try
            {
                var pago = AppServices.CrearRegistrarPago().Ejecutar(
                    alumno.Id, anio, mes, montoUsd, fechaPago,
                    metodoPago, referencia, tasaBcv, montoBs);

                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = true,
                    mensaje = "Pago registrado correctamente.",
                    folio = pago.Folio,
                    montoUsd = pago.MontoUsd,
                    fechaPago = pago.FechaPago
                });
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = ex.Message
                });
            }
        }

        private async Task ProcesarAdminReportesAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            int anio = ObtenerInt(data, "anio");
            if (anio <= 0) anio = DateTime.Today.Year;

            int mes = ObtenerInt(data, "mes");
            if (mes is <= 0 or > 12) mes = DateTime.Today.Month;

            var (recaudadoTotal, cantidadPagos, pagosPorMetodo) =
                ObtenerReportePagos(actividadId, anio, mes);

            var (asistenciasTotal, alumnosConAsistencia, asistenciasPorDia) =
                ObtenerReporteAsistencias(actividadId, anio, mes);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Reportes obtenidos correctamente.",
                anio,
                mes,
                recaudadoTotal,
                cantidadPagos,
                pagosPorMetodo,
                asistenciasTotal,
                alumnosConAsistencia,
                asistenciasPorDia
            });
        }

        // =====================================================================
        // SOLICITUDES DE PAGO (Administrador crea, Cliente sube comprobante,
        // Administrador aprueba o rechaza)
        // =====================================================================

        private async Task ProcesarAdminPagoSolicitarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");
            string codigo = Obtener(data, "codigoAlumno", "codigo");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var alumno = ResolverAlumno(actividadId, codigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado en esta actividad."
                });
                return;
            }

            if (!decimal.TryParse(
                    Obtener(data, "montoUsd", "monto"),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montoUsd) ||
                montoUsd <= 0)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El monto en dólares no es válido."
                });
                return;
            }

            string concepto = Obtener(data, "concepto");
            if (string.IsNullOrWhiteSpace(concepto))
                concepto = "Mensualidad";

            int anio = ObtenerInt(data, "anio");
            if (anio <= 0) anio = DateTime.Today.Year;

            int mes = ObtenerInt(data, "mes");
            if (mes is <= 0 or > 12) mes = DateTime.Today.Month;

            long solicitudId = InsertarSolicitudPago(alumno.Id, actividadId, anio, mes, montoUsd, concepto);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Solicitud de pago creada. El alumno ya puede verla y subir su comprobante.",
                solicitudId
            });
        }

        private async Task ProcesarAdminPagosPendientesAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Solicitudes obtenidas correctamente.",
                solicitudes = ObtenerSolicitudesPago(actividadId)
            });
        }

        private async Task ProcesarAdminPagoComprobanteAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");
            int solicitudId = ObtenerInt(data, "solicitudId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var (bytes, contentType) = ObtenerComprobante(actividadId, solicitudId);
            if (bytes == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Esta solicitud no tiene comprobante todavía."
                });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "",
                comprobanteBase64 = Convert.ToBase64String(bytes),
                contentType
            });
        }

        private async Task ProcesarAdminPagoAprobarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");
            int solicitudId = ObtenerInt(data, "solicitudId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var solicitud = ObtenerSolicitud(actividadId, solicitudId);
            if (solicitud == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Solicitud no encontrada."
                });
                return;
            }

            if (solicitud.Value.Estado != "comprobante_subido")
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El alumno todavía no ha subido el comprobante de este pago."
                });
                return;
            }

            string metodoPago = Obtener(data, "metodoPago");
            if (string.IsNullOrWhiteSpace(metodoPago))
                metodoPago = string.IsNullOrWhiteSpace(solicitud.Value.MetodoPago)
                    ? "Pagomovil"
                    : solicitud.Value.MetodoPago;

            decimal.TryParse(
                Obtener(data, "tasaBcv"),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tasaBcv);

            string referencia = Obtener(data, "referencia");
            if (string.IsNullOrWhiteSpace(referencia))
                referencia = $"MOBILE-{DateTime.Now:yyyyMMddHHmmssfff}";

            decimal montoBs = Math.Round(solicitud.Value.MontoUsd * tasaBcv, 2);

            try
            {
                var pago = AppServices.CrearRegistrarPago().Ejecutar(
                    solicitud.Value.AlumnoId, solicitud.Value.Anio, solicitud.Value.Mes,
                    solicitud.Value.MontoUsd, DateTime.Now, metodoPago, referencia,
                    tasaBcv, montoBs);

                MarcarSolicitud(solicitudId, "aprobado", pago.Folio, "");

                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = true,
                    mensaje = "Pago aprobado y registrado correctamente.",
                    folio = pago.Folio
                });
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = ex.Message
                });
            }
        }

        private async Task ProcesarAdminPagoRechazarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");
            int solicitudId = ObtenerInt(data, "solicitudId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            if (ObtenerSolicitud(actividadId, solicitudId) == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Solicitud no encontrada."
                });
                return;
            }

            string comentario = Obtener(data, "comentario");
            if (string.IsNullOrWhiteSpace(comentario))
                comentario = "Comprobante rechazado por el administrador.";

            MarcarSolicitud(solicitudId, "rechazado", "", comentario);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Solicitud rechazada."
            });
        }

        private async Task ProcesarClientePagosPendientesAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            int actividadId = ObtenerInt(data, "actividadId");
            string codigo = Obtener(data, "codigoAlumno", "codigo");

            if (!ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La actividad seleccionada no está disponible."
                });
                return;
            }

            var alumno = ResolverAlumno(actividadId, codigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado."
                });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Solicitudes obtenidas correctamente.",
                solicitudes = ObtenerSolicitudesPagoAlumno(alumno.Id)
            });
        }

        private async Task ProcesarClientePagoComprobanteAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            int actividadId = ObtenerInt(data, "actividadId");
            string codigo = Obtener(data, "codigoAlumno", "codigo");
            int solicitudId = ObtenerInt(data, "solicitudId");
            string base64 = Obtener(data, "comprobanteBase64");
            string contentType = Obtener(data, "contentType");

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "image/png";

            if (!ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La actividad seleccionada no está disponible."
                });
                return;
            }

            var alumno = ResolverAlumno(actividadId, codigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(base64))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "No se recibió ninguna imagen."
                });
                return;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La imagen recibida no es válida."
                });
                return;
            }

            // Límite razonable para no llenar la base de datos con imágenes enormes.
            if (bytes.Length > 6 * 1024 * 1024)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La imagen es demasiado grande (máximo 6 MB)."
                });
                return;
            }

            bool actualizado = GuardarComprobante(solicitudId, alumno.Id, bytes, contentType);

            if (!actualizado)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "No se encontró esa solicitud para este alumno, o ya fue resuelta por el administrador."
                });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Comprobante enviado. El administrador lo revisará pronto."
            });
        }

        /// <summary>
        /// El alumno inicia y sube su propio pago en un solo paso: crea
        /// la solicitud (con el método que dice haber usado) y adjunta
        /// el comprobante de una vez, sin depender de que el
        /// administrador la haya creado antes.
        /// </summary>
        private async Task ProcesarClientePagoCrearAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            int actividadId = ObtenerInt(data, "actividadId");
            string codigo = Obtener(data, "codigoAlumno", "codigo");
            string metodoPago = Obtener(data, "metodoPago");
            string concepto = Obtener(data, "concepto");
            string base64 = Obtener(data, "comprobanteBase64");
            string contentType = Obtener(data, "contentType");

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "image/png";

            if (!ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La actividad seleccionada no está disponible."
                });
                return;
            }

            var alumno = ResolverAlumno(actividadId, codigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Alumno no encontrado."
                });
                return;
            }

            if (!decimal.TryParse(
                    Obtener(data, "montoUsd", "monto"),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montoUsd) ||
                montoUsd <= 0)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "El monto en dólares no es válido."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(concepto))
                concepto = "Mensualidad";

            if (string.IsNullOrWhiteSpace(base64))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "Debes adjuntar el comprobante de tu pago."
                });
                return;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La imagen recibida no es válida."
                });
                return;
            }

            if (bytes.Length > 6 * 1024 * 1024)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La imagen es demasiado grande (máximo 6 MB)."
                });
                return;
            }

            int anio = DateTime.Today.Year;
            int mes = DateTime.Today.Month;

            long solicitudId = InsertarSolicitudPago(
                alumno.Id, actividadId, anio, mes, montoUsd, concepto,
                metodoPago, estadoInicial: "pendiente");

            GuardarComprobante((int)solicitudId, alumno.Id, bytes, contentType);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Pago enviado. El administrador lo revisará pronto.",
                solicitudId
            });
        }

        // =====================================================================
        // MÉTODOS DE PAGO DEL ESTABLECIMIENTO (dónde le pueden pagar al alumno)
        // =====================================================================

        private async Task ProcesarAdminMetodosPagoObtenerAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Métodos obtenidos correctamente.",
                metodos = ObtenerMetodosPago(actividadId, soloActivos: false)
            });
        }

        private async Task ProcesarAdminMetodosPagoGuardarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            // Cada método viaja como un par de campos planos
            // (datos_<clave> / activo_<clave>) en vez de una lista anidada,
            // porque LeerDiccionarioAsync solo admite valores de texto.
            var metodos = new List<MetodoPagoEntrada>();
            foreach (var (metodo, clave) in MetodosPagoEstandar)
            {
                string datos = Obtener(data, $"datos_{clave}");
                bool activo = Obtener(data, $"activo_{clave}") == "true";
                metodos.Add(new MetodoPagoEntrada { Metodo = metodo, Datos = datos, Activo = activo });
            }

            GuardarMetodosPago(actividadId, metodos);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Métodos de pago guardados correctamente."
            });
        }


        private async Task ProcesarClienteMetodosPagoAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);
            int actividadId = ObtenerInt(data, "actividadId");

            if (!ActividadValida(actividadId))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La actividad seleccionada no está disponible."
                });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Métodos obtenidos correctamente.",
                metodos = ObtenerMetodosPago(actividadId, soloActivos: true)
            });
        }

        /// <summary>
        /// Tasa BCV automática, para no tener que escribirla a mano cada
        /// vez que se aprueba un pago. Viking PC hace la llamada externa
        /// (ya tiene salida a internet para el correo de verificación) y
        /// Mobile solo le pregunta a Viking PC, igual que con todo lo demás.
        /// Si la consulta externa falla, Mobile simplemente deja el campo
        /// en blanco para que se escriba a mano — nunca es obligatoria.
        /// </summary>
        private async Task ProcesarTasaBcvAsync(HttpListenerContext context)
        {
            try
            {
                if (_tasaBcvCache > 0 && DateTime.Now < _tasaBcvCacheExpira)
                {
                    await EscribirJsonAsync(context.Response, 200, new
                    {
                        ok = true,
                        mensaje = "",
                        tasa = _tasaBcvCache
                    });
                    return;
                }

                string json = await TasaHttpClient.GetStringAsync("https://bcv.today/api/v1/rate.json");

                using var doc = JsonDocument.Parse(json);
                decimal tasa = doc.RootElement.GetProperty("USD").GetDecimal();

                _tasaBcvCache = tasa;
                _tasaBcvCacheExpira = DateTime.Now.AddMinutes(15);

                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = true,
                    mensaje = "",
                    tasa
                });
            }
            catch (Exception ex)
            {
                RegistrarErrorServidor("/api/publico/tasa-bcv", ex);

                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "No se pudo obtener la tasa BCV automáticamente. Escríbela a mano."
                });
            }
        }

        // =====================================================================
        // LOGO DEL ESTABLECIMIENTO (personalización de marca)
        // =====================================================================

        private async Task ProcesarAdminLogoGuardarAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            string base64 = Obtener(data, "logoBase64");
            string contentType = Obtener(data, "contentType");
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "image/png";

            if (string.IsNullOrWhiteSpace(base64))
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "No se recibió ninguna imagen."
                });
                return;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La imagen recibida no es válida."
                });
                return;
            }

            if (bytes.Length > 3 * 1024 * 1024)
            {
                await EscribirJsonAsync(context.Response, 200, new
                {
                    ok = false,
                    mensaje = "La imagen es demasiado grande (máximo 3 MB)."
                });
                return;
            }

            using (var conn = new SqliteConnection(Database.ObtenerConnectionString()))
            {
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
INSERT INTO logo_actividad (actividad_id, logo, content_type)
VALUES ($actividadId, $logo, $contentType)
ON CONFLICT (actividad_id)
DO UPDATE SET logo = excluded.logo, content_type = excluded.content_type;";

                cmd.Parameters.AddWithValue("$actividadId", actividadId);
                cmd.Parameters.AddWithValue("$logo", bytes);
                cmd.Parameters.AddWithValue("$contentType", contentType);

                cmd.ExecuteNonQuery();
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Logo actualizado correctamente."
            });
        }

        private async Task ProcesarAdminLogoObtenerAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);

            string usuario = Obtener(data, "usuario", "nombreEstablecimiento", "nombre");
            int actividadId = ObtenerInt(data, "actividadId");

            var (ok, mensaje) = ValidarEstablecimiento(usuario, actividadId);
            if (!ok)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje });
                return;
            }

            var (bytes, contentType) = ObtenerLogo(actividadId);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "",
                logoBase64 = bytes == null ? "" : Convert.ToBase64String(bytes),
                contentType
            });
        }

        private async Task ProcesarPublicoLogoAsync(HttpListenerContext context)
        {
            var data = await LeerDiccionarioAsync(context);
            int actividadId = ObtenerInt(data, "actividadId");

            var (bytes, contentType) = ObtenerLogo(actividadId);

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = bytes != null,
                mensaje = bytes == null ? "Este establecimiento no configuró un logo." : "",
                logoBase64 = bytes == null ? "" : Convert.ToBase64String(bytes),
                contentType
            });
        }

        private async Task ProcesarAlumnoSubirFotoAsync(HttpListenerContext context, string path)
        {
            var data = await LeerDiccionarioAsync(context);

            // Obtener el identificador ya sea de la URL (/api/alumnos/{id}/foto) o del body
            string idOcodigo = ExtraerIdentificadorDeRuta(path, "foto");
            if (string.IsNullOrWhiteSpace(idOcodigo))
                idOcodigo = Obtener(data, "codigoAlumno", "codigo", "id", "alumnoId");

            int actividadId = ObtenerInt(data, "actividadId");
            string fotoBase64 = Obtener(data, "fotoBase64", "imagenBase64", "foto");

            if (string.IsNullOrWhiteSpace(idOcodigo))
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "El código o ID del alumno es obligatorio." });
                return;
            }

            if (string.IsNullOrWhiteSpace(fotoBase64))
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "No se recibió la foto en Base64." });
                return;
            }

            byte[] bytesFoto;
            try
            {
                bytesFoto = Convert.FromBase64String(fotoBase64);
            }
            catch
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "La imagen no tiene un formato Base64 válido." });
                return;
            }

            // Validar tamaño máximo (1.5 MB para seguridad de red)
            if (bytesFoto.Length > 1572864)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "La foto excede el tamaño máximo permitido (1.5 MB)." });
                return;
            }

            var alumno = ResolverAlumnoPorIdOCodigo(actividadId, idOcodigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "Alumno no encontrado." });
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dirFotos = Path.Combine(baseDir, "fotos");
            if (!Directory.Exists(dirFotos))
                Directory.CreateDirectory(dirFotos);

            string nombreArchivo = $"{alumno.Codigo.Trim()}.jpg";
            string rutaCompleta = Path.Combine(dirFotos, nombreArchivo);
            string rutaRelativa = Path.Combine("fotos", nombreArchivo);

            await File.WriteAllBytesAsync(rutaCompleta, bytesFoto);

            // Actualizar en SQLite
            using (var conn = new SqliteConnection(Database.ObtenerConnectionString()))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE alumnos SET foto_ruta = $ruta WHERE id = $id;";
                cmd.Parameters.AddWithValue("$ruta", rutaRelativa);
                cmd.Parameters.AddWithValue("$id", alumno.Id);
                cmd.ExecuteNonQuery();
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Foto guardada correctamente.",
                fotoRuta = rutaRelativa,
                codigo = alumno.Codigo
            });
        }

        private async Task ProcesarAlumnoObtenerFotoAsync(HttpListenerContext context, string path)
        {
            string idOcodigo = ExtraerIdentificadorDeRuta(path, "foto");
            if (string.IsNullOrWhiteSpace(idOcodigo))
            {
                var query = context.Request.QueryString;
                idOcodigo = query["codigo"] ?? query["codigoAlumno"] ?? query["id"] ?? "";
            }

            if (string.IsNullOrWhiteSpace(idOcodigo))
            {
                await EscribirJsonAsync(context.Response, 400, new { ok = false, mensaje = "Identificador de alumno no proporcionado." });
                return;
            }

            var alumno = ResolverAlumnoPorIdOCodigo(0, idOcodigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 404, new { ok = false, mensaje = "Alumno no encontrado." });
                return;
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? rutaFoto = null;

            if (!string.IsNullOrWhiteSpace(alumno.FotoRuta))
            {
                if (File.Exists(alumno.FotoRuta)) rutaFoto = alumno.FotoRuta;
                else if (File.Exists(Path.Combine(baseDir, alumno.FotoRuta))) rutaFoto = Path.Combine(baseDir, alumno.FotoRuta);
            }

            if (rutaFoto == null)
            {
                string candidata = Path.Combine(baseDir, "fotos", $"{alumno.Codigo.Trim()}.jpg");
                if (File.Exists(candidata)) rutaFoto = candidata;
            }

            if (rutaFoto == null || !File.Exists(rutaFoto))
            {
                await EscribirJsonAsync(context.Response, 404, new { ok = false, mensaje = "El alumno no tiene foto registrada." });
                return;
            }

            byte[] bytes = await File.ReadAllBytesAsync(rutaFoto);
            context.Response.ContentType = "image/jpeg";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
        }

        private async Task ProcesarAlumnoActualizarDatosAsync(HttpListenerContext context, string path)
        {
            var data = await LeerDiccionarioAsync(context);

            string idOcodigo = ExtraerIdentificadorDeRuta(path, "datos");
            if (string.IsNullOrWhiteSpace(idOcodigo))
                idOcodigo = Obtener(data, "codigoAlumno", "codigo", "id", "alumnoId");

            int actividadId = ObtenerInt(data, "actividadId");
            string cedula = Obtener(data, "cedula");
            string telefono = Obtener(data, "telefono");

            if (string.IsNullOrWhiteSpace(idOcodigo))
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "El código o ID del alumno es obligatorio." });
                return;
            }

            var alumno = ResolverAlumnoPorIdOCodigo(actividadId, idOcodigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "Alumno no encontrado." });
                return;
            }

            using (var conn = new SqliteConnection(Database.ObtenerConnectionString()))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE alumnos
                    SET cedula = CASE WHEN $cedula <> '' THEN $cedula ELSE cedula END,
                        telefono = CASE WHEN $telefono <> '' THEN $telefono ELSE telefono END
                    WHERE id = $id;";
                cmd.Parameters.AddWithValue("$cedula", cedula.Trim());
                cmd.Parameters.AddWithValue("$telefono", telefono.Trim());
                cmd.Parameters.AddWithValue("$id", alumno.Id);
                cmd.ExecuteNonQuery();
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Datos del alumno actualizados correctamente.",
                alumnoId = alumno.Id,
                codigo = alumno.Codigo,
                cedula = string.IsNullOrWhiteSpace(cedula) ? alumno.Cedula : cedula.Trim(),
                telefono = string.IsNullOrWhiteSpace(telefono) ? alumno.Telefono : telefono.Trim()
            });
        }

        private async Task ProcesarAlumnoObtenerCarnetAsync(HttpListenerContext context, string path)
        {
            string idOcodigo = ExtraerIdentificadorDeRuta(path, "carnet");
            if (string.IsNullOrWhiteSpace(idOcodigo))
            {
                var query = context.Request.QueryString;
                idOcodigo = query["codigo"] ?? query["codigoAlumno"] ?? query["id"] ?? "";
            }

            if (string.IsNullOrWhiteSpace(idOcodigo))
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "El código del alumno es obligatorio." });
                return;
            }

            var alumno = ResolverAlumnoPorIdOCodigo(0, idOcodigo);
            if (alumno == null)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "Alumno no encontrado." });
                return;
            }

            var carnet = AppServices.CrearObtenerCarnetPorId().Ejecutar(alumno.Id);
            if (carnet == null)
            {
                await EscribirJsonAsync(context.Response, 200, new { ok = false, mensaje = "No se pudo generar el carné del alumno." });
                return;
            }

            await EscribirJsonAsync(context.Response, 200, new
            {
                ok = true,
                mensaje = "Carné obtenido correctamente.",
                alumnoId = carnet.AlumnoId,
                codigo = carnet.Codigo,
                nombre = carnet.Nombre,
                cedula = carnet.Cedula ?? "",
                telefono = carnet.Telefono ?? "",
                plan = carnet.PlanNombre,
                estadoSemaforo = carnet.EstadoSemaforo.ToString().ToLowerInvariant(),
                tituloEstado = carnet.TituloEstado,
                subtituloEstado = carnet.SubtituloEstado,
                motivoDetalle = carnet.MotivoDetalle ?? "",
                fechaVencimiento = carnet.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "",
                diasRestantes = carnet.DiasRestantes,
                saldoPendiente = carnet.SaldoPendiente,
                tieneFoto = carnet.TieneFoto
            });
        }

        private static string ExtraerIdentificadorDeRuta(string path, string accionFinal)
        {
            // /api/alumnos/{id}/foto -> extrae {id}
            string prefijo = "/api/alumnos/";
            string sufijo = "/" + accionFinal;

            if (path.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase) &&
                path.EndsWith(sufijo, StringComparison.OrdinalIgnoreCase))
            {
                int start = prefijo.Length;
                int len = path.Length - start - sufijo.Length;
                if (len > 0)
                    return Uri.UnescapeDataString(path.Substring(start, len).Trim());
            }

            return "";
        }

        private static Alumno? ResolverAlumnoPorIdOCodigo(int actividadId, string idOcodigo)
        {
            if (string.IsNullOrWhiteSpace(idOcodigo)) return null;

            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();
            using var cmd = conn.CreateCommand();

            if (int.TryParse(idOcodigo, out int alumnoId) && alumnoId > 0 && !idOcodigo.StartsWith("A", StringComparison.OrdinalIgnoreCase))
            {
                cmd.CommandText = "SELECT * FROM alumnos WHERE id = $id AND activo = 1 LIMIT 1;";
                cmd.Parameters.AddWithValue("$id", alumnoId);
            }
            else
            {
                cmd.CommandText = actividadId > 0
                    ? "SELECT * FROM alumnos WHERE UPPER(TRIM(codigo)) = UPPER(TRIM($cod)) AND actividad_id = $act AND activo = 1 LIMIT 1;"
                    : "SELECT * FROM alumnos WHERE UPPER(TRIM(codigo)) = UPPER(TRIM($cod)) AND activo = 1 LIMIT 1;";
                cmd.Parameters.AddWithValue("$cod", idOcodigo.Trim());
                if (actividadId > 0)
                    cmd.Parameters.AddWithValue("$act", actividadId);
            }

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Alumno
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    ActividadId = reader.GetInt32(reader.GetOrdinal("actividad_id")),
                    Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                    Cedula = reader.IsDBNull(reader.GetOrdinal("cedula")) ? null : reader.GetString(reader.GetOrdinal("cedula")),
                    Codigo = reader.IsDBNull(reader.GetOrdinal("codigo")) ? "" : reader.GetString(reader.GetOrdinal("codigo")),
                    FechaInscripcion = reader.GetString(reader.GetOrdinal("fecha_inscripcion")),
                    PlanVecesSemana = reader.IsDBNull(reader.GetOrdinal("plan_veces_semana")) ? 0 : reader.GetInt32(reader.GetOrdinal("plan_veces_semana")),
                    ClasesMes = reader.IsDBNull(reader.GetOrdinal("clases_mes")) ? 0 : reader.GetInt32(reader.GetOrdinal("clases_mes")),
                    PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
                    Activo = reader.GetInt32(reader.GetOrdinal("activo")) == 1,
                    FechaInicioPlan = LeerFechaNullableHelper(reader, "fecha_inicio_plan"),
                    FechaVencimientoPlan = LeerFechaNullableHelper(reader, "fecha_vencimiento_plan"),
                    FotoRuta = reader.IsDBNull(reader.GetOrdinal("foto_ruta")) ? null : reader.GetString(reader.GetOrdinal("foto_ruta"))
                };
            }

            return null;
        }

        private static DateTime? LeerFechaNullableHelper(SqliteDataReader reader, string nombre)
        {
            int ordinal = reader.GetOrdinal(nombre);
            if (reader.IsDBNull(ordinal)) return null;
            return DateTime.TryParse(reader.GetString(ordinal), out var fecha) ? fecha : null;
        }

        private static (byte[]? Bytes, string ContentType) ObtenerLogo(int actividadId)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT logo, content_type
FROM logo_actividad
WHERE actividad_id = $actividadId;";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read() || reader["logo"] is DBNull)
                return (null, "image/png");

            byte[] bytes = (byte[])reader["logo"];
            string contentType = reader["content_type"]?.ToString() ?? "image/png";
            return (bytes, contentType);
        }

        private sealed class MetodoPagoEntrada
        {
            public string Metodo { get; set; } = "";
            public string Datos { get; set; } = "";
            public bool Activo { get; set; }
        }

        private static readonly (string Metodo, string Clave)[] MetodosPagoEstandar =
        {
            ("Efectivo $", "efectivo_usd"),
            ("Efectivo bs", "efectivo_bs"),
            ("Pagomovil", "pagomovil"),
            ("Transferencia", "transferencia"),
            ("Zelle", "zelle"),
            ("Binance Pay", "binance")
        };

        private static List<object> ObtenerMetodosPago(int actividadId, bool soloActivos)
        {
            var guardados = new Dictionary<string, (string Datos, bool Activo)>(StringComparer.OrdinalIgnoreCase);

            using (var conn = new SqliteConnection(Database.ObtenerConnectionString()))
            {
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
SELECT metodo, datos, activo
FROM metodos_pago_actividad
WHERE actividad_id = $actividadId;";
                cmd.Parameters.AddWithValue("$actividadId", actividadId);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string metodo = reader["metodo"]?.ToString() ?? "";
                    string datos = reader["datos"]?.ToString() ?? "";
                    bool activo = Convert.ToInt64(reader["activo"]) == 1;
                    guardados[metodo] = (datos, activo);
                }
            }

            var resultado = new List<object>();

            foreach (var (metodo, clave) in MetodosPagoEstandar)
            {
                var (datos, activo) = guardados.TryGetValue(metodo, out var valor)
                    ? valor
                    : ("", false);

                if (soloActivos && (!activo || string.IsNullOrWhiteSpace(datos)))
                    continue;

                resultado.Add(new { metodo, clave, datos, activo });
            }

            return resultado;
        }

        private static void GuardarMetodosPago(int actividadId, List<MetodoPagoEntrada> metodos)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            foreach (var entrada in metodos)
            {
                if (string.IsNullOrWhiteSpace(entrada.Metodo))
                    continue;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
INSERT INTO metodos_pago_actividad (actividad_id, metodo, datos, activo)
VALUES ($actividadId, $metodo, $datos, $activo)
ON CONFLICT (actividad_id, metodo)
DO UPDATE SET datos = excluded.datos, activo = excluded.activo;";

                cmd.Parameters.AddWithValue("$actividadId", actividadId);
                cmd.Parameters.AddWithValue("$metodo", entrada.Metodo);
                cmd.Parameters.AddWithValue("$datos", entrada.Datos ?? "");
                cmd.Parameters.AddWithValue("$activo", entrada.Activo ? 1 : 0);

                cmd.ExecuteNonQuery();
            }
        }

        private readonly struct SolicitudPagoInfo
        {
            public int AlumnoId { get; init; }
            public int Anio { get; init; }
            public int Mes { get; init; }
            public decimal MontoUsd { get; init; }
            public string Estado { get; init; }
            public string MetodoPago { get; init; }
        }

        private static long InsertarSolicitudPago(
            int alumnoId, int actividadId, int anio, int mes, decimal montoUsd,
            string concepto, string metodoPago = "", string estadoInicial = "pendiente")
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO solicitudes_pago
    (alumno_id, actividad_id, anio, mes, monto_usd, tasa_bcv, concepto, metodo_pago, estado, fecha_creacion)
VALUES
    ($alumnoId, $actividadId, $anio, $mes, $montoUsd, 0, $concepto, $metodoPago, $estado, $fecha);
SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$anio", anio);
            cmd.Parameters.AddWithValue("$mes", mes);
            cmd.Parameters.AddWithValue("$montoUsd", montoUsd);
            cmd.Parameters.AddWithValue("$concepto", concepto);
            cmd.Parameters.AddWithValue("$metodoPago", metodoPago ?? "");
            cmd.Parameters.AddWithValue("$estado", estadoInicial);
            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            return Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
        }

        private static List<object> ObtenerSolicitudesPago(int actividadId)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT sp.id, a.codigo, a.nombre, sp.monto_usd, sp.concepto, sp.estado, sp.metodo_pago,
       sp.fecha_creacion, sp.fecha_comprobante,
       (sp.comprobante IS NOT NULL) AS tiene_comprobante
FROM solicitudes_pago sp
INNER JOIN alumnos a ON a.id = sp.alumno_id
WHERE sp.actividad_id = $actividadId
  AND sp.estado IN ('pendiente', 'comprobante_subido')
ORDER BY sp.fecha_creacion DESC;";

            cmd.Parameters.AddWithValue("$actividadId", actividadId);

            var resultado = new List<object>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new
                {
                    solicitudId = Convert.ToInt64(reader["id"]),
                    codigoAlumno = reader["codigo"]?.ToString() ?? "",
                    nombreAlumno = reader["nombre"]?.ToString() ?? "",
                    montoUsd = Convert.ToDecimal(reader["monto_usd"]),
                    concepto = reader["concepto"]?.ToString() ?? "",
                    estado = reader["estado"]?.ToString() ?? "",
                    metodoPago = reader["metodo_pago"]?.ToString() ?? "",
                    fechaCreacion = reader["fecha_creacion"]?.ToString() ?? "",
                    fechaComprobante = reader["fecha_comprobante"] is DBNull
                        ? null
                        : reader["fecha_comprobante"]?.ToString(),
                    tieneComprobante = Convert.ToInt64(reader["tiene_comprobante"]) == 1
                });
            }

            return resultado;
        }

        private static List<object> ObtenerSolicitudesPagoAlumno(int alumnoId)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT id, monto_usd, concepto, estado, metodo_pago, fecha_creacion, fecha_resolucion,
       comentario_admin, folio_pago
FROM solicitudes_pago
WHERE alumno_id = $alumnoId
ORDER BY fecha_creacion DESC
LIMIT 20;";

            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);

            var resultado = new List<object>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new
                {
                    solicitudId = Convert.ToInt64(reader["id"]),
                    montoUsd = Convert.ToDecimal(reader["monto_usd"]),
                    concepto = reader["concepto"]?.ToString() ?? "",
                    estado = reader["estado"]?.ToString() ?? "",
                    metodoPago = reader["metodo_pago"]?.ToString() ?? "",
                    fechaCreacion = reader["fecha_creacion"]?.ToString() ?? "",
                    fechaResolucion = reader["fecha_resolucion"] is DBNull
                        ? null
                        : reader["fecha_resolucion"]?.ToString(),
                    comentarioAdmin = reader["comentario_admin"]?.ToString() ?? "",
                    folioPago = reader["folio_pago"]?.ToString() ?? ""
                });
            }


            return resultado;
        }

        private static (byte[]? Bytes, string ContentType) ObtenerComprobante(int actividadId, int solicitudId)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT comprobante, comprobante_content_type
FROM solicitudes_pago
WHERE id = $id AND actividad_id = $actividadId;";

            cmd.Parameters.AddWithValue("$id", solicitudId);
            cmd.Parameters.AddWithValue("$actividadId", actividadId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read() || reader["comprobante"] is DBNull)
                return (null, "");

            byte[] bytes = (byte[])reader["comprobante"];
            string contentType = reader["comprobante_content_type"]?.ToString() ?? "image/png";
            return (bytes, contentType);
        }

        private static bool GuardarComprobante(int solicitudId, int alumnoId, byte[] bytes, string contentType)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE solicitudes_pago
SET comprobante = $comprobante,
    comprobante_content_type = $contentType,
    estado = 'comprobante_subido',
    fecha_comprobante = $fecha
WHERE id = $id
  AND alumno_id = $alumnoId
  AND estado IN ('pendiente', 'comprobante_subido');";

            cmd.Parameters.AddWithValue("$comprobante", bytes);
            cmd.Parameters.AddWithValue("$contentType", contentType);
            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$id", solicitudId);
            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);

            return cmd.ExecuteNonQuery() > 0;
        }

        private static SolicitudPagoInfo? ObtenerSolicitud(int actividadId, int solicitudId)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT alumno_id, anio, mes, monto_usd, estado, metodo_pago
FROM solicitudes_pago
WHERE id = $id AND actividad_id = $actividadId;";

            cmd.Parameters.AddWithValue("$id", solicitudId);
            cmd.Parameters.AddWithValue("$actividadId", actividadId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new SolicitudPagoInfo
            {
                AlumnoId = Convert.ToInt32(reader["alumno_id"]),
                Anio = Convert.ToInt32(reader["anio"]),
                Mes = Convert.ToInt32(reader["mes"]),
                MontoUsd = Convert.ToDecimal(reader["monto_usd"]),
                Estado = reader["estado"]?.ToString() ?? "",
                MetodoPago = reader["metodo_pago"]?.ToString() ?? ""
            };
        }

        private static void MarcarSolicitud(int solicitudId, string estado, string folioPago, string comentario)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
UPDATE solicitudes_pago
SET estado = $estado,
    folio_pago = $folioPago,
    comentario_admin = $comentario,
    fecha_resolucion = $fecha
WHERE id = $id;";

            cmd.Parameters.AddWithValue("$estado", estado);
            cmd.Parameters.AddWithValue("$folioPago", folioPago ?? "");
            cmd.Parameters.AddWithValue("$comentario", comentario ?? "");
            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$id", solicitudId);

            cmd.ExecuteNonQuery();
        }

        private async Task ProcesarClienteReciboAsync(HttpListenerContext context)
        {
            string codigo = context.Request.QueryString["codigoAlumno"] ?? "";
            string folio = context.Request.QueryString["folio"] ?? "";
            int actividadId = ObtenerInt(context.Request.QueryString["actividadId"]);

            if (string.IsNullOrWhiteSpace(codigo) ||
                string.IsNullOrWhiteSpace(folio) ||
                !ActividadValida(actividadId))
            {
                context.Response.StatusCode = 400;
                await EscribirTextoAsync(context.Response,
                    "<html><body><h3>Solicitud de recibo inválida.</h3></body></html>",
                    "text/html; charset=utf-8");
                return;
            }

            var alumno = new SqliteAlumnoRepository(Database.ObtenerConnectionString())
                .ObtenerPorActividad(actividadId)
                .FirstOrDefault(x =>
                    string.Equals(x.Codigo?.Trim(), codigo.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (alumno == null)
            {
                context.Response.StatusCode = 404;
                await EscribirTextoAsync(context.Response,
                    "<html><body><h3>Alumno no encontrado.</h3></body></html>",
                    "text/html; charset=utf-8");
                return;
            }

            var pago = ObtenerPagoPorFolio(folio);

            if (pago == null || pago.AlumnoId != alumno.Id)
            {
                context.Response.StatusCode = 404;
                await EscribirTextoAsync(context.Response,
                    "<html><body><h3>Recibo no encontrado.</h3></body></html>",
                    "text/html; charset=utf-8");
                return;
            }

            var configuracion = AppServices.CrearObtenerConfiguracion()
                .Ejecutar(actividadId);

            decimal cuota = ObtenerCuotaCiclo(
                alumno.PlanId, pago.FechaPago.Year, pago.FechaPago.Month);

            decimal abonado = ObtenerAbonadoHastaPago(
                alumno.Id, pago.FechaPago);

            decimal restante = Math.Max(cuota - abonado, 0m);

            // Misma plantilla que el recibo de VIKING PC (logo incluido).
            string html = ImpresionRecibo.ConstruirHtml(
                configuracion.Nombre,
                configuracion.Identificacion,
                configuracion.TipoEstablecimiento,
                actividadId,
                alumno.Nombre,
                alumno.Codigo,
                alumno.Cedula,
                pago.Anio,
                pago.Mes,
                pago.MontoUsd,
                pago.TasaBcv,
                pago.MontoBs,
                pago.MetodoPago,
                pago.Referencia,
                cuota,
                abonado,
                restante,
                pago.Folio,
                pago.FechaPago);

            await EscribirTextoAsync(
                context.Response,
                html,
                "text/html; charset=utf-8");
        }

        private bool ActividadValida(int actividadId)
        {
            if (actividadId <= 0)
                return false;

            return obtenerActividades()
                .Any(a => a.Id == actividadId && a.Activa);
        }

        /// <summary>
        /// Validación compartida por los endpoints nuevos de Modo
        /// Administrador (alumnos, pagos, reportes): misma regla que ya
        /// usan ProcesarAdminValidarAsync/ProcesarAdminDashboardAsync,
        /// factorizada aquí para no duplicarla en cada endpoint nuevo.
        /// </summary>
        private (bool Ok, string Mensaje) ValidarEstablecimiento(string usuario, int actividadId)
        {
            if (string.IsNullOrWhiteSpace(usuario) || !ActividadValida(actividadId))
                return (false, "No se pudo validar el establecimiento o la actividad.");

            var configuracion = AppServices.CrearObtenerConfiguracion()
                .Ejecutar(actividadId);

            if (Normalizar(configuracion.Nombre) != Normalizar(usuario))
                return (false, "El nombre del establecimiento no coincide con Viking PC.");

            return (true, "");
        }

        private static Alumno? ResolverAlumno(int actividadId, string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return null;

            return new SqliteAlumnoRepository(Database.ObtenerConnectionString())
                .ObtenerPorActividad(actividadId)
                .FirstOrDefault(x =>
                    string.Equals(x.Codigo?.Trim(), codigo.Trim(),
                        StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Activo / por vencer / moroso con la misma ventana de 7 días
        /// que ya usa ProcesarAdminDashboardAsync para sus contadores,
        /// para que el listado de alumnos y el dashboard siempre coincidan.
        /// </summary>
        private static string ClasificarEstadoMembresia(DateTime? fechaVencimientoPlan)
        {
            if (!fechaVencimientoPlan.HasValue)
                return "activo";

            if (fechaVencimientoPlan.Value.Date < DateTime.Today)
                return "moroso";

            if (fechaVencimientoPlan.Value.Date <= DateTime.Today.AddDays(7))
                return "por_vencer";

            return "activo";
        }

        private static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return "";

            string descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in descompuesto)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return string.Join(
                " ",
                sb.ToString()
                    .Normalize(NormalizationForm.FormC)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
        }

        private static async Task<string> LeerBodyAsync(HttpListenerContext context)
        {
            using var reader = new StreamReader(
                context.Request.InputStream,
                context.Request.ContentEncoding ?? Encoding.UTF8);

            return await reader.ReadToEndAsync();
        }

        private static async Task<Dictionary<string, string>> LeerDiccionarioAsync(
            HttpListenerContext context)
        {
            string body = await LeerBodyAsync(context);

            if (string.IsNullOrWhiteSpace(body))
                return new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            try
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    body, JsonOptions);

                return result ?? new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException)
            {
                return new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string Obtener(
            Dictionary<string, string> data,
            params string[] nombres)
        {
            foreach (string nombre in nombres)
            {
                if (data.TryGetValue(nombre, out string? valor) &&
                    !string.IsNullOrWhiteSpace(valor))
                    return valor.Trim();
            }

            return "";
        }

        private static int ObtenerInt(
            Dictionary<string, string> data,
            string nombre = "actividadId")
        {
            return data.TryGetValue(nombre, out string? valor)
                && int.TryParse(valor, out int resultado)
                ? resultado
                : 0;
        }

        private static int ObtenerInt(string? valor)
        {
            return int.TryParse(valor, out int resultado) ? resultado : 0;
        }

        private static List<object> ObtenerPagos(int alumnoId)
        {
            var resultado = new List<object>();

            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT id, anio, mes, monto, tasa_bcv, monto_bs,
       fecha_pago, metodo_pago, referencia, folio
FROM pagos
WHERE alumno_id = $id
ORDER BY datetime(fecha_pago) DESC, id DESC;";

            cmd.Parameters.AddWithValue("$id", alumnoId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string fechaTexto = reader["fecha_pago"]?.ToString() ?? "";
                DateTime.TryParse(fechaTexto, out DateTime fecha);

                resultado.Add(new
                {
                    id = Convert.ToInt32(reader["id"]),
                    anio = Convert.ToInt32(reader["anio"]),
                    mes = Convert.ToInt32(reader["mes"]),
                    montoUsd = Convert.ToDecimal(reader["monto"]),
                    montoDolares = Convert.ToDecimal(reader["monto"]),
                    tasaBcv = Convert.ToDecimal(reader["tasa_bcv"]),
                    montoBs = Convert.ToDecimal(reader["monto_bs"]),
                    fechaPago = fecha,
                    metodoPago = reader["metodo_pago"]?.ToString() ?? "",
                    referencia = reader["referencia"]?.ToString() ?? "",
                    folio = reader["folio"]?.ToString() ?? ""
                });
            }

            return resultado;
        }

        private static List<object> ObtenerAsistencias(int alumnoId)
        {
            var resultado = new List<object>();

            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT fecha
FROM asistencias
WHERE alumno_id = $id
ORDER BY date(fecha) DESC
LIMIT 100;";

            cmd.Parameters.AddWithValue("$id", alumnoId);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string fecha = reader["fecha"]?.ToString() ?? "";
                resultado.Add(new
                {
                    fecha,
                    fechaAsistencia = fecha
                });
            }

            return resultado;
        }

        private static decimal ObtenerRecaudadoMes(int actividadId)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COALESCE(SUM(p.monto), 0)
FROM pagos p
INNER JOIN alumnos a ON a.id = p.alumno_id
WHERE a.actividad_id = $actividadId
  AND a.activo = 1
  AND strftime('%Y', p.fecha_pago) = $anio
  AND strftime('%m', p.fecha_pago) = $mes;";

            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$anio", DateTime.Today.Year.ToString("0000"));
            cmd.Parameters.AddWithValue("$mes", DateTime.Today.Month.ToString("00"));

            return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
        }

        /// <summary>
        /// Reporte de recaudación para el Modo Administrador de Mobile:
        /// total, cantidad de pagos y desglose por método de pago, para
        /// cualquier mes/año (no solo el mes actual, a diferencia de
        /// <see cref="ObtenerRecaudadoMes"/>, que sigue igual para no
        /// afectar a /api/admin/dashboard).
        /// </summary>
        private static (decimal Total, int Cantidad, List<object> PorMetodo)
            ObtenerReportePagos(int actividadId, int anio, int mes)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT p.metodo_pago, p.monto
FROM pagos p
INNER JOIN alumnos a ON a.id = p.alumno_id
WHERE a.actividad_id = $actividadId
  AND strftime('%Y', p.fecha_pago) = $anio
  AND strftime('%m', p.fecha_pago) = $mes;";

            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$anio", anio.ToString("0000"));
            cmd.Parameters.AddWithValue("$mes", mes.ToString("00"));

            decimal total = 0m;
            int cantidad = 0;
            var porMetodo = new Dictionary<string, (decimal Total, int Cantidad)>(
                StringComparer.OrdinalIgnoreCase);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                decimal monto = Convert.ToDecimal(reader["monto"]);
                string metodo = reader["metodo_pago"]?.ToString() ?? "Sin especificar";

                total += monto;
                cantidad++;

                if (!porMetodo.TryGetValue(metodo, out var actual))
                    actual = (0m, 0);

                porMetodo[metodo] = (actual.Total + monto, actual.Cantidad + 1);
            }

            var lista = porMetodo
                .Select(kv => (object)new
                {
                    metodo = kv.Key,
                    total = kv.Value.Total,
                    cantidad = kv.Value.Cantidad
                })
                .ToList();

            return (total, cantidad, lista);
        }

        /// <summary>
        /// Reporte de asistencia para el Modo Administrador de Mobile:
        /// total de registros, alumnos distintos que asistieron y el
        /// desglose por día, para el mes/año pedido.
        /// </summary>
        private static (int Total, int AlumnosConAsistencia, List<object> PorDia)
            ObtenerReporteAsistencias(int actividadId, int anio, int mes)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT s.fecha, s.alumno_id
FROM asistencias s
INNER JOIN alumnos a ON a.id = s.alumno_id
WHERE a.actividad_id = $actividadId
  AND strftime('%Y', s.fecha) = $anio
  AND strftime('%m', s.fecha) = $mes;";

            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$anio", anio.ToString("0000"));
            cmd.Parameters.AddWithValue("$mes", mes.ToString("00"));

            var alumnosDistintos = new HashSet<int>();
            var porDia = new Dictionary<string, int>();
            int total = 0;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                total++;
                alumnosDistintos.Add(Convert.ToInt32(reader["alumno_id"]));

                string fecha = reader["fecha"]?.ToString() ?? "";
                string dia = fecha.Length >= 10 ? fecha[..10] : fecha;

                porDia[dia] = porDia.TryGetValue(dia, out int actual) ? actual + 1 : 1;
            }

            var lista = porDia
                .OrderBy(kv => kv.Key)
                .Select(kv => (object)new { fecha = kv.Key, cantidad = kv.Value })
                .ToList();

            return (total, alumnosDistintos.Count, lista);
        }

        private static Pago? ObtenerPagoPorFolio(string folio)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT id, alumno_id, anio, mes, monto, tasa_bcv, monto_bs,
       fecha_pago, metodo_pago, referencia, folio
FROM pagos
WHERE folio = $folio
LIMIT 1;";

            cmd.Parameters.AddWithValue("$folio", folio);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            DateTime.TryParse(
                reader["fecha_pago"]?.ToString(),
                out DateTime fechaPago);

            return new Pago
            {
                Id = Convert.ToInt32(reader["id"]),
                AlumnoId = Convert.ToInt32(reader["alumno_id"]),
                Anio = Convert.ToInt32(reader["anio"]),
                Mes = Convert.ToInt32(reader["mes"]),
                MontoUsd = Convert.ToDecimal(reader["monto"]),
                TasaBcv = Convert.ToDecimal(reader["tasa_bcv"]),
                MontoBs = Convert.ToDecimal(reader["monto_bs"]),
                FechaPago = fechaPago,
                MetodoPago = reader["metodo_pago"]?.ToString() ?? "",
                Referencia = reader["referencia"]?.ToString() ?? "",
                Folio = reader["folio"]?.ToString() ?? ""
            };
        }

        private static decimal ObtenerAbonadoHastaPago(int alumnoId, DateTime fechaPago)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT COALESCE(SUM(monto), 0)
FROM pagos
WHERE alumno_id = $alumnoId
  AND date(fecha_pago) <= date($fechaPago);";

            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue(
                "$fechaPago",
                fechaPago.ToString("yyyy-MM-dd"));

            return Convert.ToDecimal(cmd.ExecuteScalar() ?? 0m);
        }

        private static decimal ObtenerCuotaCiclo(
            int planId,
            int anio,
            int mes)
        {
            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();

            int periodo = anio * 100 + mes;

            using var historial = conn.CreateCommand();
            historial.CommandText = @"
SELECT precio
FROM planes_actividad_precio_historial
WHERE plan_id = $planId
  AND (anio * 100 + mes) <= $periodo
ORDER BY anio DESC, mes DESC
LIMIT 1;";

            historial.Parameters.AddWithValue("$planId", planId);
            historial.Parameters.AddWithValue("$periodo", periodo);

            var valor = historial.ExecuteScalar();

            if (valor != null && valor != DBNull.Value)
                return Convert.ToDecimal(valor);

            using var actual = conn.CreateCommand();
            actual.CommandText = @"
SELECT COALESCE(precio, 0)
FROM planes_actividad
WHERE id = $planId;";

            actual.Parameters.AddWithValue("$planId", planId);

            return Convert.ToDecimal(actual.ExecuteScalar() ?? 0m);
        }

        private static void ConfigurarCors(HttpListenerResponse response)
        {
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Accept";
        }

        private static async Task EscribirJsonAsync(
            HttpListenerResponse response,
            int statusCode,
            object data)
        {
            response.StatusCode = statusCode;
            response.ContentType = "application/json; charset=utf-8";

            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions);
            response.ContentLength64 = bytes.Length;

            await response.OutputStream.WriteAsync(bytes);
        }

        private static async Task EscribirTextoAsync(
            HttpListenerResponse response,
            string texto,
            string contentType)
        {
            response.ContentType = contentType;
            byte[] bytes = Encoding.UTF8.GetBytes(texto);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
        }

        public void Dispose()
        {
            try
            {
                if (EstaEjecutando)
                    DetenerAsync().GetAwaiter().GetResult();
            }
            catch
            {
            }

            listener.Close();
        }
    }
}

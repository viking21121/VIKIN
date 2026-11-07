using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AsistenciaNatacion
{
    public enum MonedaBcv
    {
        Dolar,
        Euro
    }

    public class ResultadoTasaBcv
    {
        public decimal? Tasa { get; set; }
        public string Fuente { get; set; } = "Desconocida";
        public bool Exitoso => Tasa.HasValue && Tasa.Value > 0;
        public string MensajeError { get; set; } = string.Empty;
        public DateTime FechaConsulta { get; set; } = DateTime.Now;
        public MonedaBcv Moneda { get; set; } = MonedaBcv.Dolar;
    }

    public static class ServicioBcv
    {
        // Validación SSL estricta por defecto.
        // Se toleran errores de certificado únicamente para bcv.org.ve.
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidarCertificadoSeguro
        };

        private static readonly HttpClient client = new HttpClient(handler);

        // Caché independiente para cada moneda.
        private static readonly Dictionary<MonedaBcv, ResultadoTasaBcv> _cacheResultados = new();

        private static readonly TimeSpan TtlCache =
            TimeSpan.FromMinutes(30);

        static ServicioBcv()
        {
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/120.0.0.0 Safari/537.36");

            client.Timeout = TimeSpan.FromSeconds(6);
        }

        private static bool ValidarCertificadoSeguro(
            HttpRequestMessage message,
            X509Certificate2? cert,
            X509Chain? chain,
            SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;

            string host = message.RequestUri?.Host ?? "";

            if (host.EndsWith(
                "bcv.org.ve",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        // ============================================================
        // MÉTODOS PÚBLICOS
        // ============================================================

        // Mantiene compatibilidad con el código actual.
        // Si alguien llama este método sin indicar moneda,
        // seguirá obteniendo DÓLAR.
        public static async Task<decimal?> ObtenerTasaBcvAsync(
            bool forzarRefresco = false)
        {
            return await ObtenerTasaBcvAsync(
                MonedaBcv.Dolar,
                forzarRefresco);
        }

        // Nuevo método: permite solicitar dólar o euro.
        public static async Task<decimal?> ObtenerTasaBcvAsync(
            MonedaBcv moneda,
            bool forzarRefresco = false)
        {
            var resultado =
                await ObtenerTasaDetalladaAsync(
                    moneda,
                    forzarRefresco);

            return resultado.Tasa;
        }

        // Mantiene compatibilidad con el código actual.
        public static async Task<ResultadoTasaBcv> ObtenerTasaDetalladaAsync(
            bool forzarRefresco = false)
        {
            return await ObtenerTasaDetalladaAsync(
                MonedaBcv.Dolar,
                forzarRefresco);
        }

        // Nuevo método principal para dólar/euro.
        public static async Task<ResultadoTasaBcv> ObtenerTasaDetalladaAsync(
            MonedaBcv moneda,
            bool forzarRefresco = false)
        {
            // ========================================================
            // 1. REVISAR CACHÉ DE LA MONEDA SOLICITADA
            // ========================================================

            if (!forzarRefresco &&
                _cacheResultados.TryGetValue(
                    moneda,
                    out ResultadoTasaBcv? cache) &&
                (DateTime.Now - cache.FechaConsulta) < TtlCache)
            {
                return cache;
            }

            var errores = new StringBuilder();

            // ========================================================
            // 2. INTENTO PRINCIPAL: DOLARAPI
            // ========================================================

            var resApi =
                await ObtenerDeDolarApiAsync(moneda);

            if (resApi.Exitoso)
            {
                GuardarCache(moneda, resApi);
                return resApi;
            }

            if (!string.IsNullOrEmpty(resApi.MensajeError))
            {
                errores.AppendLine(
                    $"DolarApi ({NombreMoneda(moneda)}): " +
                    resApi.MensajeError);
            }

            // ========================================================
            // 3. RESPALDO: SITIO OFICIAL DEL BCV
            // ========================================================

            var resBcv =
                await ObtenerDeBcvDirectoAsync(moneda);

            if (resBcv.Exitoso)
            {
                GuardarCache(moneda, resBcv);
                return resBcv;
            }

            if (!string.IsNullOrEmpty(resBcv.MensajeError))
            {
                errores.AppendLine(
                    $"BCV Directo ({NombreMoneda(moneda)}): " +
                    resBcv.MensajeError);
            }

            // ========================================================
            // 4. RESULTADO FALLIDO
            // ========================================================

            return new ResultadoTasaBcv
            {
                Tasa = null,
                Fuente = "Ninguna",
                Moneda = moneda,
                MensajeError =
                    errores.Length > 0
                        ? errores.ToString().TrimEnd()
                        : "No se pudo obtener la tasa desde ninguna fuente."
            };
        }

        // ============================================================
        // DOLARAPI
        // ============================================================

        private static async Task<ResultadoTasaBcv>
            ObtenerDeDolarApiAsync(MonedaBcv moneda)
        {
            var res = new ResultadoTasaBcv
            {
                Fuente = "DolarApi",
                Moneda = moneda
            };

            try
            {
                string endpoint =
                    moneda == MonedaBcv.Dolar
                        ? "dolares"
                        : "euros";

                string url =
                    $"https://ve.dolarapi.com/v1/{endpoint}/oficial?_t={DateTime.Now.Ticks}";

                using var request =
                    new HttpRequestMessage(
                        HttpMethod.Get,
                        url);

                request.Headers.Add(
                    "Cache-Control",
                    "no-cache");

                var response =
                    await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    res.MensajeError =
                        $"Respuesta HTTP {(int)response.StatusCode} " +
                        $"({response.ReasonPhrase})";

                    return res;
                }

                string json =
                    await response.Content.ReadAsStringAsync();

                using var doc =
                    JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty(
                        "promedio",
                        out var promedioElem) &&
                    promedioElem.TryGetDecimal(
                        out decimal tasa))
                {
                    res.Tasa =
                        Math.Round(tasa, 2);

                    return res;
                }

                res.MensajeError =
                    "Campo 'promedio' no encontrado " +
                    "en el JSON devuelto por DolarApi.";
            }
            catch (TaskCanceledException)
            {
                res.MensajeError =
                    "Tiempo de espera agotado (Timeout).";
            }
            catch (HttpRequestException ex)
            {
                res.MensajeError =
                    $"Error de red/SSL: {ex.Message}";
            }
            catch (Exception ex)
            {
                res.MensajeError =
                    $"Error de procesamiento: {ex.Message}";
            }

            return res;
        }

        // ============================================================
        // BCV DIRECTO
        // ============================================================

        private static async Task<ResultadoTasaBcv>
            ObtenerDeBcvDirectoAsync(MonedaBcv moneda)
        {
            var res = new ResultadoTasaBcv
            {
                Fuente = "BCV Sitio Oficial",
                Moneda = moneda
            };

            try
            {
                string url =
                    $"https://www.bcv.org.ve/?_t={DateTime.Now.Ticks}";

                using var response =
                    await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    res.MensajeError =
                        $"Respuesta HTTP {(int)response.StatusCode} " +
                        $"({response.ReasonPhrase})";

                    return res;
                }

                string html =
                    await response.Content.ReadAsStringAsync();

                string idMoneda =
                    moneda == MonedaBcv.Dolar
                        ? "dolar"
                        : "euro";

                int posMoneda =
                    html.IndexOf(
                        $"id=\"{idMoneda}\"",
                        StringComparison.OrdinalIgnoreCase);

                if (posMoneda == -1)
                {
                    res.MensajeError =
                        $"La estructura HTML cambió: " +
                        $"no se encontró id=\"{idMoneda}\" " +
                        "en el sitio del BCV.";

                    return res;
                }

                int posStrong =
                    html.IndexOf(
                        "<strong>",
                        posMoneda,
                        StringComparison.OrdinalIgnoreCase);

                int posCierre =
                    html.IndexOf(
                        "</strong>",
                        posStrong > -1
                            ? posStrong
                            : posMoneda,
                        StringComparison.OrdinalIgnoreCase);

                if (posStrong == -1 ||
                    posCierre == -1 ||
                    posCierre <= posStrong)
                {
                    res.MensajeError =
                        $"No se encontraron las etiquetas " +
                        $"<strong>...</strong> para {NombreMoneda(moneda)}.";

                    return res;
                }

                int inicio =
                    posStrong + "<strong>".Length;

                string valorTexto =
                    html.Substring(
                        inicio,
                        posCierre - inicio)
                    .Trim()
                    .Replace(',', '.');

                if (decimal.TryParse(
                        valorTexto,
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out decimal tasa))
                {
                    res.Tasa =
                        Math.Round(tasa, 2);

                    return res;
                }

                res.MensajeError =
                    $"No se pudo convertir el texto " +
                    $"'{valorTexto}' a número decimal.";
            }
            catch (TaskCanceledException)
            {
                res.MensajeError =
                    "Tiempo de espera agotado al conectar al BCV (Timeout).";
            }
            catch (HttpRequestException ex)
            {
                res.MensajeError =
                    $"Error de conexión con el BCV: {ex.Message}";
            }
            catch (Exception ex)
            {
                res.MensajeError =
                    $"Excepción no esperada: {ex.Message}";
            }

            return res;
        }

        // ============================================================
        // CACHÉ
        // ============================================================

        private static void GuardarCache(
            MonedaBcv moneda,
            ResultadoTasaBcv resultado)
        {
            resultado.FechaConsulta = DateTime.Now;
            resultado.Moneda = moneda;

            _cacheResultados[moneda] = resultado;
        }

        // ============================================================
        // UTILIDADES
        // ============================================================

        private static string NombreMoneda(
            MonedaBcv moneda)
        {
            return moneda == MonedaBcv.Dolar
                ? "Dólar BCV"
                : "Euro BCV";
        }
    }
}
using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;

namespace AsistenciaNatacion
{
    public class ResultadoTasaBcv
    {
        public decimal? Tasa { get; set; }
        public string Fuente { get; set; } = "Desconocida";
        public bool Exitoso => Tasa.HasValue && Tasa.Value > 0;
        public string MensajeError { get; set; } = string.Empty;
        public DateTime FechaConsulta { get; set; } = DateTime.Now;
    }

    public static class ServicioBcv
    {
        // Validación de SSL selectiva: Estricta por defecto, tolera fallos de CA gubernamental solo en bcv.org.ve
        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ValidarCertificadoSeguro
        };

        private static readonly HttpClient client = new HttpClient(handler);

        // Caché en memoria para evitar ser detectado como spam / rate limit por consultas continuas
        private static ResultadoTasaBcv? _cacheResultado;
        private static readonly TimeSpan TtlCache = TimeSpan.FromMinutes(30);

        static ServicioBcv()
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            client.Timeout = TimeSpan.FromSeconds(6);
        }

        private static bool ValidarCertificadoSeguro(HttpRequestMessage message, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;

            // Si el error de certificado ocurre exclusivamente al conectar al sitio del BCV, se permite continuar
            string host = message.RequestUri?.Host ?? "";
            if (host.EndsWith("bcv.org.ve", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Para cualquier otro sitio (ej. ve.dolarapi.com), se exige validación SSL estricta
            return false;
        }

        /// <summary>
        /// Obtiene la tasa BCV optimizada usando caché de 30 minutos.
        /// </summary>
        public static async Task<decimal?> ObtenerTasaBcvAsync(bool forzarRefresco = false)
        {
            var resultado = await ObtenerTasaDetalladaAsync(forzarRefresco);
            return resultado.Tasa;
        }

        /// <summary>
        /// Obtiene la tasa BCV con detalle de fuente y mensajes de diagnóstico en caso de fallo.
        /// </summary>
        public static async Task<ResultadoTasaBcv> ObtenerTasaDetalladaAsync(bool forzarRefresco = false)
        {
            // Usar respuesta de caché si sigue vigente
            if (!forzarRefresco && _cacheResultado != null && (DateTime.Now - _cacheResultado.FechaConsulta) < TtlCache)
            {
                return _cacheResultado;
            }

            var errores = new System.Text.StringBuilder();

            // 1. Intento Principal: API JSON DolarApi
            var resApi = await ObtenerDeDolarApiAsync();
            if (resApi.Exitoso)
            {
                _cacheResultado = resApi;
                return resApi;
            }
            if (!string.IsNullOrEmpty(resApi.MensajeError))
            {
                errores.AppendLine($"DolarApi: {resApi.MensajeError}");
            }

            // 2. Intento de Respaldo: Web oficial BCV
            var resBcv = await ObtenerDeBcvDirectoAsync();
            if (resBcv.Exitoso)
            {
                _cacheResultado = resBcv;
                return resBcv;
            }
            if (!string.IsNullOrEmpty(resBcv.MensajeError))
            {
                errores.AppendLine($"BCV Directo: {resBcv.MensajeError}");
            }

            var resultadoFallido = new ResultadoTasaBcv
            {
                Tasa = null,
                Fuente = "Ninguna",
                MensajeError = errores.Length > 0 ? errores.ToString().TrimEnd() : "No se pudo obtener la tasa desde ninguna fuente."
            };

            return resultadoFallido;
        }

        private static async Task<ResultadoTasaBcv> ObtenerDeDolarApiAsync()
        {
            var res = new ResultadoTasaBcv { Fuente = "DolarApi" };
            try
            {
                string url = $"https://ve.dolarapi.com/v1/dolares/oficial?_t={DateTime.Now.Ticks}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Cache-Control", "no-cache");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    res.MensajeError = $"Respuesta HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                    return res;
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("promedio", out var promedioElem) && promedioElem.TryGetDecimal(out decimal tasa))
                {
                    res.Tasa = Math.Round(tasa, 2);
                    return res;
                }

                res.MensajeError = "Campo 'promedio' no encontrado en el JSON devuelto por DolarApi.";
            }
            catch (TaskCanceledException)
            {
                res.MensajeError = "Tiempo de espera agotado (Timeout).";
            }
            catch (HttpRequestException ex)
            {
                res.MensajeError = $"Error de red/SSL: {ex.Message}";
            }
            catch (Exception ex)
            {
                res.MensajeError = $"Error de procesamiento: {ex.Message}";
            }

            return res;
        }

        private static async Task<ResultadoTasaBcv> ObtenerDeBcvDirectoAsync()
        {
            var res = new ResultadoTasaBcv { Fuente = "BCV Sitio Oficial" };
            try
            {
                string url = $"https://www.bcv.org.ve/?_t={DateTime.Now.Ticks}";
                using var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    res.MensajeError = $"Respuesta HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
                    return res;
                }

                string html = await response.Content.ReadAsStringAsync();

                int posDolar = html.IndexOf("id=\"dolar\"", StringComparison.OrdinalIgnoreCase);
                if (posDolar != -1)
                {
                    int posStrong = html.IndexOf("<strong>", posDolar, StringComparison.OrdinalIgnoreCase);
                    int posCierre = html.IndexOf("</strong>", posStrong > -1 ? posStrong : posDolar, StringComparison.OrdinalIgnoreCase);

                    if (posStrong != -1 && posCierre != -1 && posCierre > posStrong)
                    {
                        int inicio = posStrong + 8;
                        string valorTexto = html.Substring(inicio, posCierre - inicio).Trim().Replace(',', '.');

                        if (decimal.TryParse(valorTexto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tasa))
                        {
                            res.Tasa = Math.Round(tasa, 2);
                            return res;
                        }
                        else
                        {
                            res.MensajeError = $"No se pudo convertir el texto '{valorTexto}' a número decimal.";
                        }
                    }
                    else
                    {
                        res.MensajeError = "No se encontraron las etiquetas <strong>...</strong> del dólar en la página del BCV.";
                    }
                }
                else
                {
                    res.MensajeError = "La estructura HTML cambió: no se encontró id=\"dolar\" en el sitio del BCV.";
                }
            }
            catch (TaskCanceledException)
            {
                res.MensajeError = "Tiempo de espera agotado al conectar al BCV (Timeout).";
            }
            catch (HttpRequestException ex)
            {
                res.MensajeError = $"Error de conexión con el BCV: {ex.Message}";
            }
            catch (Exception ex)
            {
                res.MensajeError = $"Excepción no esperada: {ex.Message}";
            }

            return res;
        }
    }
}
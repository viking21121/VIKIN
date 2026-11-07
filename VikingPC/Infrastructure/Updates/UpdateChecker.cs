using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace AsistenciaNatacion.Infrastructure.Updates;

/// <summary>
/// Comprueba la última versión publicada en GitHub Releases.
/// Si existe una versión superior, inicia VikingUpdater.exe y deja que el
/// actualizador reemplace los archivos cuando VIKING haya terminado.
/// </summary>
public sealed class UpdateChecker
{
    private static readonly HttpClient HttpClient = CrearHttpClient();

    public async Task<bool> ComprobarYActualizarAsync()
    {
        try
        {
            // ============================================================
            // 1. OBTENER VERSIÓN ACTUAL
            // ============================================================

            Version versionActual = ObtenerVersionActual();

            // ============================================================
            // 2. CONSULTAR ÚLTIMO RELEASE DE GITHUB
            // ============================================================

            using var response = await HttpClient.GetAsync(
                UpdateSettings.LatestReleaseUrl,
                HttpCompletionOption.ResponseHeadersRead);

            // Si GitHub no responde, no hay Internet o el Release
            // no está disponible, VIKING continúa funcionando normalmente.
            if (!response.IsSuccessStatusCode)
                return false;

            await using var stream =
                await response.Content.ReadAsStreamAsync();

            var release =
                await JsonSerializer.DeserializeAsync<GitHubRelease>(
                    stream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (release == null ||
                string.IsNullOrWhiteSpace(release.TagName))
            {
                return false;
            }

            // ============================================================
            // 3. CONVERTIR TAG DE GITHUB A VERSION
            // ============================================================

            string textoVersion =
                release.TagName
                    .Trim()
                    .TrimStart('v', 'V');

            if (!Version.TryParse(
                    textoVersion,
                    out Version? versionNueva))
            {
                return false;
            }

            // ============================================================
            // 4. COMPARAR VERSIONES
            // ============================================================

            if (versionNueva <= versionActual)
                return false;

            // ============================================================
            // 5. BUSCAR EL ASSET DE ACTUALIZACIÓN
            //
            // GitHub utiliza siempre:
            //
            // VIKING.zip
            //
            // La versión está determinada por el Tag del Release:
            //
            // v1.0.1 -> VIKING.zip
            // v1.0.2 -> VIKING.zip
            // v1.1.0 -> VIKING.zip
            // ============================================================

            var asset =
                release.Assets.FirstOrDefault(x =>
                    string.Equals(
                        x.Name,
                        UpdateSettings.ReleaseAssetName,
                        StringComparison.OrdinalIgnoreCase));

            if (asset == null ||
                string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                return false;
            }

            // ============================================================
            // 6. COMPROBAR QUE EXISTE EL ACTUALIZADOR
            // ============================================================

            string updaterPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    UpdateSettings.UpdaterFileName);

            if (!File.Exists(updaterPath))
                return false;

            // ============================================================
            // 7. AVISAR AL USUARIO
            // ============================================================

            MostrarAvisoActualizacion(
                versionActual,
                versionNueva);

            // ============================================================
            // 8. PREPARAR VIKINGUPDATER.EXE
            // ============================================================

            var startInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
            };

            // ID del proceso actual de VIKING.
            startInfo.ArgumentList.Add("--pid");
            startInfo.ArgumentList.Add(
                Environment.ProcessId.ToString());

            // Ruta exacta del ejecutable que debe reiniciarse.
            startInfo.ArgumentList.Add("--exe");
            startInfo.ArgumentList.Add(
                Environment.ProcessPath ?? "Viking.exe");

            // URL directa del ZIP publicado en GitHub.
            startInfo.ArgumentList.Add("--download-url");
            startInfo.ArgumentList.Add(
                asset.BrowserDownloadUrl);

            // Versión que se está instalando.
            startInfo.ArgumentList.Add("--version");
            startInfo.ArgumentList.Add(
                versionNueva.ToString());

            // ============================================================
            // 9. INICIAR ACTUALIZADOR
            // ============================================================

            Process.Start(startInfo);

            // El actualizador se encargará de:
            //
            // 1. Esperar a que VIKING se cierre.
            // 2. Descargar VIKING.zip.
            // 3. Extraer la nueva versión.
            // 4. Reemplazar los archivos de la aplicación.
            // 5. Mantener la base de datos.
            // 6. Volver a iniciar VIKING.
            //
            // Esa parte corresponde a VikingUpdater.exe.

            return true;
        }
        catch (Exception ex)
        {
            AsistenciaNatacion.Infrastructure.Logging.Logger.Warn("No se pudo comprobar la actualización.", ex);
            // ============================================================
            // IMPORTANTE
            // ============================================================
            //
            // Un problema con las actualizaciones JAMÁS debe impedir
            // que VIKING pueda abrirse y funcionar normalmente.
            //
            return false;
        }
    }

    // ================================================================
    // OBTENER VERSIÓN ACTUAL
    // ================================================================

    private static Version ObtenerVersionActual()
    {
        string? versionTexto =
            Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(versionTexto))
        {
            // Elimina información adicional como:
            //
            // 1.0.1+abc123
            //
            // dejando:
            //
            // 1.0.1
            string limpio =
                versionTexto
                    .Split('+')[0]
                    .Trim();

            if (Version.TryParse(
                    limpio,
                    out Version? versionInformativa))
            {
                return versionInformativa;
            }
        }

        return Assembly.GetEntryAssembly()?.GetName().Version
            ?? new Version(1, 0, 0);
    }

    // ================================================================
    // AVISO DE ACTUALIZACIÓN
    // ================================================================

    private static void MostrarAvisoActualizacion(
        Version versionActual,
        Version versionNueva)
    {
        string mensaje =
            $"Hay una nueva versión de VIKING disponible.\n\n" +
            $"Versión actual: {versionActual}\n" +
            $"Nueva versión: {versionNueva}\n\n" +
            "La actualización se descargará automáticamente.\n" +
            "VIKING se cerrará y volverá a abrir cuando termine.";

        MessageBox.Show(
            mensaje,
            "Actualización de VIKING",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ================================================================
    // HTTP CLIENT
    // ================================================================

    private static HttpClient CrearHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        client.DefaultRequestHeaders.UserAgent.Clear();

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(
                "VIKING",
                "1.0"));

        client.DefaultRequestHeaders.Accept.Clear();

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        return client;
    }
}
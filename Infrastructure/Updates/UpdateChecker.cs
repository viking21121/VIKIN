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
            Version versionActual = ObtenerVersionActual();

            using var response = await HttpClient.GetAsync(
                UpdateSettings.LatestReleaseUrl,
                HttpCompletionOption.ResponseHeadersRead);

            // No Internet, GitHub no disponible o repositorio sin Releases:
            // VIKING continúa funcionando normalmente.
            if (!response.IsSuccessStatusCode)
                return false;

            await using var stream = await response.Content.ReadAsStreamAsync();

            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                return false;

            string textoVersion = release.TagName.Trim().TrimStart('v', 'V');

            if (!Version.TryParse(textoVersion, out Version? versionNueva))
                return false;

            if (versionNueva <= versionActual)
                return false;

            var asset = release.Assets.FirstOrDefault(x =>
                string.Equals(
                    x.Name,
                    UpdateSettings.ReleaseAssetName,
                    StringComparison.OrdinalIgnoreCase));

            if (asset == null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
                return false;

            string updaterPath = Path.Combine(
                AppContext.BaseDirectory,
                UpdateSettings.UpdaterFileName);

            if (!File.Exists(updaterPath))
                return false;

            MostrarAvisoActualizacion(versionActual, versionNueva);

            var startInfo = new ProcessStartInfo
            {
                FileName = updaterPath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true
            };

            // ArgumentList evita problemas con espacios en las rutas.
            startInfo.ArgumentList.Add("--pid");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("--exe");
            startInfo.ArgumentList.Add(Environment.ProcessPath ?? "Viking.exe");
            startInfo.ArgumentList.Add("--download-url");
            startInfo.ArgumentList.Add(asset.BrowserDownloadUrl);
            startInfo.ArgumentList.Add("--version");
            startInfo.ArgumentList.Add(versionNueva.ToString());

            Process.Start(startInfo);

            return true;
        }
        catch
        {
            // Una actualización nunca debe impedir que el cliente use VIKING.
            return false;
        }
    }

    private static Version ObtenerVersionActual()
    {
        string? versionTexto =
            Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(versionTexto))
        {
            string limpio = versionTexto.Split('+')[0];

            if (Version.TryParse(limpio, out Version? versionInformativa))
                return versionInformativa;
        }

        return Assembly.GetEntryAssembly()?.GetName().Version
            ?? new Version(1, 0, 0);
    }

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

    private static HttpClient CrearHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("VIKING", "1.0"));

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        return client;
    }
}

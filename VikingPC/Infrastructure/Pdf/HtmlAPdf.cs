using System.Diagnostics;
using AsistenciaNatacion.Infrastructure.Logging;

namespace AsistenciaNatacion.Infrastructure.Pdf;

/// <summary>
/// Convierte una página HTML local (por ejemplo, un recibo) a PDF usando
/// Microsoft Edge en modo sin ventana, que viene incluido en Windows 10 y 11.
/// No añade ninguna librería. Si no hay Edge ni Chrome, devuelve null y quien
/// llama sigue con el HTML.
/// </summary>
public static class HtmlAPdf
{
    /// <returns>Ruta del PDF generado, junto al HTML, o null si no se pudo.</returns>
    public static string? Convertir(string rutaHtml, int tiempoMaximoMs = 25000)
    {
        string perfilTemporal = Path.Combine(
            Path.GetTempPath(), "viking_pdf_" + Guid.NewGuid().ToString("N"));

        try
        {
            string? navegador = BuscarNavegador();
            if (navegador == null)
            {
                Logger.Warn("No se encontró Microsoft Edge ni Google Chrome para generar el PDF.");
                return null;
            }

            string rutaPdf = Path.ChangeExtension(rutaHtml, ".pdf");
            if (File.Exists(rutaPdf))
                File.Delete(rutaPdf);

            var inicio = new ProcessStartInfo
            {
                FileName = navegador,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            inicio.ArgumentList.Add("--headless");
            inicio.ArgumentList.Add("--disable-gpu");
            inicio.ArgumentList.Add("--no-pdf-header-footer");
            // Perfil temporal: evita choques con una ventana de Edge ya abierta.
            inicio.ArgumentList.Add($"--user-data-dir={perfilTemporal}");
            inicio.ArgumentList.Add($"--print-to-pdf={rutaPdf}");
            inicio.ArgumentList.Add(new Uri(rutaHtml).AbsoluteUri);

            using var proceso = Process.Start(inicio);
            if (proceso == null)
                return null;

            var reloj = Stopwatch.StartNew();
            while (reloj.ElapsedMilliseconds < tiempoMaximoMs)
            {
                if (proceso.WaitForExit(200))
                    break;

                // Algunas versiones tardan en cerrarse aunque el PDF ya esté listo.
                if (File.Exists(rutaPdf) && new FileInfo(rutaPdf).Length > 0)
                {
                    Thread.Sleep(400);
                    break;
                }
            }

            if (!proceso.HasExited)
            {
                try { proceso.Kill(entireProcessTree: true); } catch { }
            }

            if (File.Exists(rutaPdf) && new FileInfo(rutaPdf).Length > 0)
                return rutaPdf;

            Logger.Warn("El navegador terminó sin generar el PDF del recibo.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Warn("No se pudo convertir el recibo a PDF.", ex);
            return null;
        }
        finally
        {
            try
            {
                if (Directory.Exists(perfilTemporal))
                    Directory.Delete(perfilTemporal, recursive: true);
            }
            catch
            {
                // Limpieza de mejor esfuerzo.
            }
        }
    }

    private static string? BuscarNavegador()
    {
        string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        string[] candidatos =
        {
            Path.Combine(pf86, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(pf, "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(pf, "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(pf86, "Google", "Chrome", "Application", "chrome.exe")
        };

        return candidatos.FirstOrDefault(File.Exists);
    }
}

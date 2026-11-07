using System.Text;

namespace AsistenciaNatacion.Infrastructure.Logging;

/// <summary>
/// Registro de eventos y errores a archivo, un archivo por día en la carpeta
/// <c>Logs</c> junto al ejecutable (<c>viking-AAAA-MM-DD.log</c>).
///
/// - Nunca lanza excepciones: un fallo al escribir el log no debe tumbar la app.
/// - Conserva los últimos <see cref="DiasRetencion"/> días y borra el resto.
/// - Es seguro desde varios hilos (la API móvil y la interfaz escriben a la vez).
/// </summary>
public static class Logger
{
    private const int DiasRetencion = 30;

    private static readonly object Candado = new();
    private static bool limpiezaHecha;

    public static string Carpeta { get; } =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    public static void Info(string mensaje) => Escribir("INFO", mensaje, null);

    public static void Warn(string mensaje, Exception? ex = null) => Escribir("WARN", mensaje, ex);

    public static void Error(string mensaje, Exception? ex = null) => Escribir("ERROR", mensaje, ex);

    private static void Escribir(string nivel, string mensaje, Exception? ex)
    {
        try
        {
            var linea = new StringBuilder()
                .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                .Append(" [").Append(nivel).Append("] ")
                .Append(mensaje);

            if (ex != null)
                linea.AppendLine().Append(ex);

            linea.AppendLine();

            lock (Candado)
            {
                Directory.CreateDirectory(Carpeta);

                string archivo = Path.Combine(
                    Carpeta, $"viking-{DateTime.Now:yyyy-MM-dd}.log");

                File.AppendAllText(archivo, linea.ToString(), Encoding.UTF8);

                if (!limpiezaHecha)
                {
                    limpiezaHecha = true;
                    LimpiarAntiguos();
                }
            }
        }
        catch
        {
            // El registro de eventos nunca debe interrumpir la aplicación.
        }
    }

    private static void LimpiarAntiguos()
    {
        try
        {
            var limite = DateTime.Now.AddDays(-DiasRetencion);

            foreach (var archivo in new DirectoryInfo(Carpeta).GetFiles("viking-*.log"))
            {
                if (archivo.LastWriteTime < limite)
                    archivo.Delete();
            }
        }
        catch
        {
            // Limpieza de mejor esfuerzo.
        }
    }
}

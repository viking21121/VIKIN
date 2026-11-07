namespace VikingMobile.Diagnostics;

/// <summary>
/// Guarda cualquier excepción no capturada en un archivo de texto
/// dentro de la propia app, y expone el contenido para mostrarlo en
/// pantalla. Existe porque, sin esto, un fallo durante el arranque
/// (antes de que exista cualquier página) hace que Android termine el
/// proceso en silencio, sin ningún mensaje ni en pantalla ni en el
/// depurador.
/// </summary>
public static class CrashLogger
{
    private static string RutaArchivo =>
        Path.Combine(FileSystem.AppDataDirectory, "viking_ultimo_error.txt");

    public static void Inicializar()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            Registrar("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Registrar("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    public static void Registrar(string origen, Exception? ex)
    {
        try
        {
            string texto =
                $"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} — {origen} ===\n" +
                (ex?.ToString() ?? "(sin excepción)") + "\n\n";

            File.AppendAllText(RutaArchivo, texto);
            AppLog.Error(origen, ex);
        }
        catch
        {
            // Si ni siquiera se puede escribir el log, no hay nada más que hacer.
        }
    }

    public static string LeerUltimoError()
    {
        try
        {
            return File.Exists(RutaArchivo)
                ? File.ReadAllText(RutaArchivo)
                : "No hay ningún error registrado todavía.";
        }
        catch (Exception ex)
        {
            return $"No se pudo leer el registro de errores: {ex.Message}";
        }
    }
}

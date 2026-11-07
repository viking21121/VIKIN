using System.Diagnostics;
using System.IO.Compression;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        string? exePath = null;

        try
        {
            var options = ParseArguments(args);

            if (!options.TryGetValue("pid", out var pidText) ||
                !int.TryParse(pidText, out int pid) ||
                !options.TryGetValue("exe", out exePath) ||
                !options.TryGetValue("download-url", out var downloadUrl))
            {
                return 2;
            }

            exePath = Path.GetFullPath(exePath);
            string baseDirectory = Path.GetDirectoryName(exePath)
                ?? AppContext.BaseDirectory;

            await EsperarProcesoAsync(pid);

            string tempZip = Path.Combine(
                Path.GetTempPath(),
                $"VIKING_update_{Guid.NewGuid():N}.zip");

            string tempDirectory = Path.Combine(
                Path.GetTempPath(),
                $"VIKING_update_{Guid.NewGuid():N}");

            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(10)
                };

                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "VIKING-Updater/1.0");

                await DescargarAsync(client, downloadUrl, tempZip);

                Directory.CreateDirectory(tempDirectory);
                ZipFile.ExtractToDirectory(
                    tempZip,
                    tempDirectory,
                    overwriteFiles: true);

                string sourceDirectory = DetectarDirectorioPublicacion(tempDirectory);

                // El cliente ya cerró VIKING. Este respaldo conserva la BD antes
                // de aplicar cualquier actualización de archivos.
                string backupDirectory = Path.Combine(
                    baseDirectory,
                    "Backups",
                    $"update_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

                Directory.CreateDirectory(backupDirectory);
                RespaldarBaseDeDatos(baseDirectory, backupDirectory);

                CopiarActualizacion(sourceDirectory, baseDirectory);

                EscribirLog(
                    baseDirectory,
                    $"Actualización completada correctamente. {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = baseDirectory,
                    UseShellExecute = true
                });

                return 0;
            }
            finally
            {
                TryDeleteFile(tempZip);
                TryDeleteDirectory(tempDirectory);
            }
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(exePath))
            {
                try
                {
                    string baseDirectory = Path.GetDirectoryName(exePath)
                        ?? AppContext.BaseDirectory;

                    EscribirLog(
                        baseDirectory,
                        $"ERROR durante actualización: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n{ex}");

                    // Si la actualización falla, no dejamos al cliente sin VIKING.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = baseDirectory,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // No podemos hacer más si incluso el reinicio falla.
                }
            }

            return 1;
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                continue;

            result[args[i][2..]] = args[++i];
        }

        return result;
    }

    private static async Task EsperarProcesoAsync(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            await Task.Run(() => process.WaitForExit());
        }
        catch (ArgumentException)
        {
            // El proceso ya terminó.
        }
    }

    private static async Task DescargarAsync(
        HttpClient client,
        string url,
        string destination)
    {
        using var response = await client.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();

        await using var input =
            await response.Content.ReadAsStreamAsync();

        await using var output = File.Create(destination);
        await input.CopyToAsync(output);
    }

    private static string DetectarDirectorioPublicacion(string directory)
    {
        string? exe = Directory.GetFiles(
                directory,
                "Viking.exe",
                SearchOption.AllDirectories)
            .FirstOrDefault();

        if (exe == null)
            throw new FileNotFoundException(
                "El paquete de actualización no contiene Viking.exe.");

        string sourceDirectory = Path.GetDirectoryName(exe)!;

        if (!File.Exists(Path.Combine(sourceDirectory, "VikingUpdater.exe")))
            throw new FileNotFoundException(
                "El paquete de actualización no contiene VikingUpdater.exe.");

        return sourceDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }

    private static void RespaldarBaseDeDatos(
        string baseDirectory,
        string backupDirectory)
    {
        string[] archivos =
        {
            "asistencia_natacion.db",
            "asistencia_natacion.db-wal",
            "asistencia_natacion.db-shm"
        };

        foreach (string nombre in archivos)
        {
            string origen = Path.Combine(baseDirectory, nombre);

            if (!File.Exists(origen))
                continue;

            File.Copy(
                origen,
                Path.Combine(backupDirectory, nombre),
                overwrite: true);
        }
    }

    private static void CopiarActualizacion(
        string sourceDirectory,
        string destinationDirectory)
    {
        foreach (string sourceFile in Directory.GetFiles(
                     sourceDirectory,
                     "*",
                     SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(
                sourceDirectory,
                sourceFile);

            if (EsRutaProtegida(relativePath))
                continue;

            string destinationFile = Path.Combine(
                destinationDirectory,
                relativePath);

            Directory.CreateDirectory(
                Path.GetDirectoryName(destinationFile)!);

            File.Copy(
                sourceFile,
                destinationFile,
                overwrite: true);
        }
    }

    private static bool EsRutaProtegida(string relativePath)
    {
        string normalized = relativePath.Replace('/', '\\');
        string[] partes = normalized.Split('\\');
        string primeraParte = partes[0];
        string nombreArchivo = Path.GetFileName(normalized);

        return primeraParte.Equals(
                   "Backups",
                   StringComparison.OrdinalIgnoreCase)
            || nombreArchivo.Equals(
                   "asistencia_natacion.db",
                   StringComparison.OrdinalIgnoreCase)
            || nombreArchivo.Equals(
                   "asistencia_natacion.db-wal",
                   StringComparison.OrdinalIgnoreCase)
            || nombreArchivo.Equals(
                   "asistencia_natacion.db-shm",
                   StringComparison.OrdinalIgnoreCase)
            || nombreArchivo.Equals(
                   "crash.log",
                   StringComparison.OrdinalIgnoreCase)
            // El updater que está ejecutándose no puede reemplazarse a sí mismo.
            // Se mantiene hasta una futura actualización del instalador.
            || nombreArchivo.Equals(
                   "VikingUpdater.exe",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void EscribirLog(
        string baseDirectory,
        string texto)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(baseDirectory, "update.log"),
                texto + Environment.NewLine + Environment.NewLine);
        }
        catch
        {
            // El log nunca debe impedir la actualización.
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }
}

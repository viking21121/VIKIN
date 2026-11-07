using VikingMobile.Application.Cliente;
using VikingMobile.Application.Communication;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Media;
using System.Text;

namespace VikingMobile;

public partial class MiCuentaPage : ContentPage
{
    public MiCuentaPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!ClienteSession.EstaActiva)
        {
            // La sesión se perdió (por ejemplo, la app se reinició):
            // volver al login de cliente en lugar de mostrar una
            // pantalla vacía.
            _ = Shell.Current.GoToAsync($"//{nameof(ClienteLoginPage)}");
            return;
        }

        MostrarCuenta(ClienteSession.Cuenta!);
        await CargarPagosPendientesAsync();
        await CargarLogoAsync();
    }

    private async Task CargarLogoAsync()
    {
        try
        {
            byte[]? logo = await AppState.ObtenerLogoPublicoAsync();
            if (logo == null)
                return;

            LogoEstablecimientoImagen.Source = ImageSource.FromStream(() => new MemoryStream(logo));
            LogoEstablecimientoImagen.IsVisible = true;
            MarcaTextoLabel.IsVisible = false;
        }
        catch
        {
            // Sin logo propio, se queda con el texto "VIKING" de siempre.
        }
    }

    private async void ContenidoRefresh_Refreshing(object sender, EventArgs e)
    {
        try
        {
            var cuenta = await AppState.ClienteService.RefrescarAsync();

            if (cuenta.Ok)
            {
                ClienteSession.ActualizarCuenta(cuenta);
                MostrarCuenta(cuenta);
            }
            else if (!string.IsNullOrWhiteSpace(cuenta.Mensaje))
            {
                await DisplayAlert("No se pudo actualizar", cuenta.Mensaje, "OK");
            }

            await CargarPagosPendientesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "OK");
        }
        finally
        {
            ContenidoRefresh.IsRefreshing = false;
        }
    }

    private async void CerrarSesionLabel_Tapped(object sender, TappedEventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Cerrar sesión",
            "¿Deseas cerrar tu sesión de Mi Cuenta?",
            "Sí",
            "Cancelar");

        if (!confirmar)
            return;

        ClienteSession.CerrarSesion();

        await Shell.Current.GoToAsync($"//{nameof(ModoSelectorPage)}");
    }

    private void MostrarCuenta(CuentaClienteResponse cuenta)
    {
        SaludoLabel.Text = string.IsNullOrWhiteSpace(cuenta.NombreAlumno)
            ? "Hola 👋"
            : $"Hola, {cuenta.NombreAlumno} 👋";

        OfflineLabel.IsVisible = cuenta.DesdeCache;

        ActividadLabel.Text = string.IsNullOrWhiteSpace(cuenta.Actividad)
            ? "🏊 Mi actividad"
            : $"🏊 {cuenta.Actividad}";

        PlanLabel.Text = string.IsNullOrWhiteSpace(cuenta.Plan)
            ? "Sin plan"
            : $"Plan: {cuenta.Plan}";

        VencimientoLabel.Text = string.IsNullOrWhiteSpace(cuenta.FechaVencimiento)
            ? "—"
            : cuenta.FechaVencimiento;

        ClasesLabel.Text = cuenta.ClasesRestantes?.ToString() ?? "—";

        SaldoLabel.Text = $"${cuenta.SaldoPendiente:0.##}";
        SaldoLabel.TextColor = cuenta.SaldoPendiente > 0
            ? Color.FromArgb("#F87171")
            : Colors.White;

        // Datos del carné digital
        CarnetNombreLabel.Text = string.IsNullOrWhiteSpace(cuenta.NombreAlumno) ? "Alumno" : cuenta.NombreAlumno;
        CarnetCodigoLabel.Text = $"ID: {cuenta.CodigoAlumno}";
        CarnetCedulaLabel.Text = string.IsNullOrWhiteSpace(cuenta.Cedula) ? "Cédula: (sin registrar)" : $"Cédula: {cuenta.Cedula}";
        CarnetTelefonoLabel.Text = string.IsNullOrWhiteSpace(cuenta.Telefono) ? "Teléfono: (sin registrar)" : $"Teléfono: {cuenta.Telefono}";

        // Cargar foto del alumno si está disponible
        CargarFotoAlumno(cuenta.CodigoAlumno);

        AplicarEstadoBadge(cuenta.Estado);
        AplicarBannerVencimiento(cuenta.Estado, cuenta.FechaVencimiento);

        RecibosCollection.ItemsSource = cuenta.Recibos;
        AsistenciasCollection.ItemsSource = cuenta.Asistencias;
    }

    private void CargarFotoAlumno(string codigo)
    {
        try
        {
            if (AppState.IsConnected && !string.IsNullOrWhiteSpace(codigo))
            {
                string url = $"{AppState.CommunicationServerUrl.TrimEnd('/')}/api/alumnos/{Uri.EscapeDataString(codigo)}/foto?t={DateTime.UtcNow.Ticks}";
                FotoAlumnoImage.Source = new UriImageSource
                {
                    Uri = new Uri(url),
                    CachingEnabled = false
                };
                FotoAlumnoImage.IsVisible = true;
                FotoPlaceholderLabel.IsVisible = false;
            }
            else
            {
                FotoAlumnoImage.IsVisible = false;
                FotoPlaceholderLabel.IsVisible = true;
            }
        }
        catch
        {
            FotoAlumnoImage.IsVisible = false;
            FotoPlaceholderLabel.IsVisible = true;
        }
    }

    private async void TomarFotoButton_Clicked(object sender, EventArgs e)
    {
        if (!AppState.IsConnected)
        {
            await DisplayAlert("Sin conexión", "Conecta con Viking PC para actualizar tu foto.", "OK");
            return;
        }

        string? opcion = await DisplayActionSheet(
            "Foto para tu carné",
            "Cancelar",
            null,
            "📷 Tomar foto con la cámara",
            "🖼️ Elegir de la galería");

        if (string.IsNullOrWhiteSpace(opcion) || opcion == "Cancelar")
            return;

        try
        {
            FileResult? archivo = opcion == "📷 Tomar foto con la cámara"
                ? await ElegirFotoAsync(usarCamara: true)
                : await ElegirFotoAsync(usarCamara: false);

            if (archivo == null) return;

            byte[] bytes;
            await using (Stream origen = await archivo.OpenReadAsync())
            using (var memoria = new MemoryStream())
            {
                await origen.CopyToAsync(memoria);
                bytes = memoria.ToArray();
            }

            var (ok, mensaje) = await AppState.ClienteService.SubirFotoAlumnoAsync(ClienteSession.CodigoAlumno, bytes);
            if (ok)
            {
                await DisplayAlert("Listo", "Foto de carné actualizada con éxito.", "OK");
                CargarFotoAlumno(ClienteSession.CodigoAlumno);
            }
            else
            {
                await DisplayAlert("Error", mensaje, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo procesar la foto: {ex.Message}", "OK");
        }
    }

    private async void EditarDatosButton_Clicked(object sender, EventArgs e)
    {
        if (!AppState.IsConnected)
        {
            await DisplayAlert("Sin conexión", "Conecta con Viking PC para actualizar tus datos.", "OK");
            return;
        }

        var cuenta = ClienteSession.Cuenta;
        string? cedula = await DisplayPromptAsync(
            "Cédula de Identidad",
            "Escribe o actualiza tu número de cédula:",
            "Siguiente",
            "Cancelar",
            initialValue: cuenta?.Cedula ?? "",
            keyboard: Keyboard.Text);

        if (cedula == null) return;

        string? telefono = await DisplayPromptAsync(
            "Número de Teléfono",
            "Escribe o actualiza tu número telefónico:",
            "Guardar",
            "Cancelar",
            initialValue: cuenta?.Telefono ?? "",
            keyboard: Keyboard.Telephone);

        if (telefono == null) return;

        var (ok, mensaje) = await AppState.ClienteService.ActualizarDatosAlumnoAsync(
            ClienteSession.CodigoAlumno, cedula.Trim(), telefono.Trim());

        if (ok)
        {
            await DisplayAlert("Listo", "Tus datos han sido actualizados.", "OK");
            if (cuenta != null)
            {
                cuenta.Cedula = cedula.Trim();
                cuenta.Telefono = telefono.Trim();
                MostrarCuenta(cuenta);
            }
        }
        else
        {
            await DisplayAlert("Error", mensaje, "OK");
        }
    }

    private void AplicarBannerVencimiento(string estado, string fechaVencimiento)
    {
        bool mostrar = estado is "por_vencer" or "moroso";

        if (!mostrar)
        {
            VencimientoBanner.IsVisible = false;
            return;
        }

        int? dias = DateTime.TryParseExact(
            fechaVencimiento, "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime fecha)
            ? (fecha.Date - DateTime.Today).Days
            : null;

        VencimientoBannerLabel.Text = estado switch
        {
            "moroso" when dias.HasValue => $"🔴 Tu plan venció hace {Math.Abs(dias.Value)} día(s)",
            "moroso" => "🔴 Tu plan está vencido",
            _ when dias is 0 => "⚠️ Tu plan vence hoy",
            _ when dias.HasValue => $"⚠️ Tu plan vence en {dias.Value} día(s)",
            _ => "⚠️ Tu plan está por vencer"
        };

        VencimientoBanner.IsVisible = true;
    }

    private async void VencimientoBanner_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ClientePagarPage));
    }

    private async void PagarButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ClientePagarPage));
    }

    private async Task CargarPagosPendientesAsync()
    {
        try
        {
            var (ok, mensaje, solicitudes) = await AppState.ClienteService.ObtenerPagosPendientesAsync();
            PagosPendientesCollection.ItemsSource = ok ? solicitudes : new List<SolicitudPagoClienteResumen>();
        }
        catch
        {
            PagosPendientesCollection.ItemsSource = new List<SolicitudPagoClienteResumen>();
        }
    }

    private async void SubirComprobanteButton_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not SolicitudPagoClienteResumen solicitud)
            return;

        string? opcion = await DisplayActionSheet(
            "Subir comprobante",
            "Cancelar",
            null,
            "📷 Tomar foto",
            "🖼️ Elegir de la galería");

        if (string.IsNullOrWhiteSpace(opcion) || opcion == "Cancelar")
            return;

        try
        {
            FileResult? archivo = opcion == "📷 Tomar foto"
                ? await ElegirFotoAsync(usarCamara: true)
                : await ElegirFotoAsync(usarCamara: false);

            if (archivo == null)
                return;

            byte[] bytes;
            await using (Stream origen = await archivo.OpenReadAsync())
            using (var memoria = new MemoryStream())
            {
                await origen.CopyToAsync(memoria);
                bytes = memoria.ToArray();
            }

            string contentType = archivo.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                  archivo.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? "image/jpeg"
                : "image/png";

            // La confirmación con vista previa vive en su propia página:
            // así el alumno ve la foto en grande antes de mandarla, en vez
            // de subirla a ciegas apenas la elige.
            ComprobantePendienteContexto.Establecer(solicitud.SolicitudId, bytes, contentType);
            await Shell.Current.GoToAsync(nameof(ConfirmarComprobantePage));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo adjuntar la imagen: {ex.Message}", "OK");
        }
    }

    private async Task<FileResult?> ElegirFotoAsync(bool usarCamara)
    {
        try
        {
            if (usarCamara)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert("Cámara no disponible", "Este dispositivo no permite tomar fotos aquí. Elige una de la galería.", "OK");
                    return null;
                }

                return await MediaPicker.Default.CapturePhotoAsync();
            }

            return await MediaPicker.Default.PickPhotoAsync();
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("No disponible", "Esta función no está disponible en tu dispositivo.", "OK");
            return null;
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permiso necesario", "Viking Mobile necesita permiso de cámara o galería para subir el comprobante.", "OK");
            return null;
        }
    }

    private void AplicarEstadoBadge(string estado)
    {
        switch (estado?.Trim().ToLowerInvariant())
        {
            case "por_vencer":
                EstadoBadge.BackgroundColor = Color.FromArgb("#B45309");
                EstadoBadgeLabel.Text = "POR VENCER";
                break;

            case "moroso":
                EstadoBadge.BackgroundColor = Color.FromArgb("#B91C1C");
                EstadoBadgeLabel.Text = "MOROSO";
                break;

            default:
                EstadoBadge.BackgroundColor = Color.FromArgb("#16803A");
                EstadoBadgeLabel.Text = "ACTIVO";
                break;
        }
    }
    private async void VerRecibo_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not ReciboResumen recibo)
            return;

        try
        {
            if (!AppState.IsConnected)
            {
                await DisplayAlert("Sin conexión", "Conéctate con Viking PC para consultar el recibo.", "OK");
                return;
            }

            string? html = await AppState.ClienteService.ObtenerReciboHtmlAsync(recibo);
            if (string.IsNullOrWhiteSpace(html))
            {
                await DisplayAlert("Recibo", "No se pudo obtener el recibo.", "OK");
                return;
            }

            string safeFolio = LimpiarNombreArchivo(recibo.Folio);
            string path = Path.Combine(FileSystem.Current.CacheDirectory, $"recibo_{safeFolio}.html");
            await File.WriteAllTextAsync(path, html, Encoding.UTF8);
            await Launcher.Default.OpenAsync(new OpenFileRequest($"Recibo {recibo.Folio}", new ReadOnlyFile(path)));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Recibo", $"No se pudo abrir el recibo: {ex.Message}", "OK");
        }
    }

    private async void DescargarRecibo_Clicked(object sender, EventArgs e)
    {
        if (sender is not Button button || button.BindingContext is not ReciboResumen recibo)
            return;

        try
        {
            if (!AppState.IsConnected)
            {
                await DisplayAlert("Sin conexión", "Conéctate con Viking PC para descargar el recibo.", "OK");
                return;
            }

            string? html = await AppState.ClienteService.ObtenerReciboHtmlAsync(recibo);
            if (string.IsNullOrWhiteSpace(html))
            {
                await DisplayAlert("Recibo", "No se pudo obtener el recibo.", "OK");
                return;
            }

            string safeFolio = LimpiarNombreArchivo(recibo.Folio);
            string path = Path.Combine(FileSystem.Current.AppDataDirectory, $"recibo_{safeFolio}.html");
            await File.WriteAllTextAsync(path, html, Encoding.UTF8);
            await Share.Default.RequestAsync(new ShareFileRequest { Title = $"Recibo {recibo.Folio}", File = new ShareFile(path) });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Recibo", $"No se pudo guardar el recibo: {ex.Message}", "OK");
        }
    }

    private static string LimpiarNombreArchivo(string valor)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            valor = valor.Replace(c, '_');
        return string.IsNullOrWhiteSpace(valor) ? "recibo" : valor.Trim();
    }

    private async void Inicio_Tapped(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Volver", "¿Deseas salir de Mi Cuenta?", "Sí", "Cancelar");
        if (confirmar)
        {
            ClienteSession.CerrarSesion();
            await Shell.Current.GoToAsync("//ModoSelectorPage");
        }
    }

}

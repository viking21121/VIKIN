using System.Globalization;
using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace VikingMobile;

public partial class AdminAlumnoDetallePage : ContentPage
{
    private AlumnoAdminResumen? _alumno;

    public AdminAlumnoDetallePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _alumno = AdminAlumnoContexto.AlumnoSeleccionado;

        if (_alumno == null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        MostrarAlumno(_alumno);
        await CargarPagosAsync();
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void MostrarFormularioButton_Clicked(object sender, EventArgs e)
    {
        FormularioBorder.IsVisible = !FormularioBorder.IsVisible;
        MostrarFormularioButton.Text = FormularioBorder.IsVisible
            ? "OCULTAR FORMULARIO"
            : "💵 REGISTRAR PAGO";
    }

    private async void ActualizarTasaButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            ActualizarTasaButton.IsEnabled = false;
            var (ok, tasa) = await AppState.AdminService.ObtenerTasaBcvAsync();

            if (ok)
                TasaEntry.Text = tasa.ToString("0.####", CultureInfo.InvariantCulture);
            else
                await DisplayAlert("Tasa BCV", "No se pudo obtener la tasa automáticamente. Escríbela a mano.", "OK");
        }
        finally
        {
            ActualizarTasaButton.IsEnabled = true;
        }
    }

    private async void GuardarPagoButton_Clicked(object sender, EventArgs e)
    {
        if (_alumno == null)
            return;

        if (!decimal.TryParse(MontoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
        {
            MostrarErrorFormulario("Escribe un monto en USD válido.");
            return;
        }

        if (!decimal.TryParse(TasaEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tasa) || tasa <= 0)
        {
            MostrarErrorFormulario("Escribe la tasa BCV del día.");
            return;
        }

        string metodo = MetodoPicker.SelectedItem as string ?? "Efectivo $";
        string referencia = ReferenciaEntry.Text?.Trim() ?? "";

        try
        {
            GuardarPagoButton.IsEnabled = false;
            GuardarPagoButton.Text = "GUARDANDO...";
            FormularioErrorLabel.IsVisible = false;

            var resultado = await AppState.AdminService.RegistrarPagoAsync(
                _alumno.Codigo, monto, tasa, metodo, referencia);

            if (!resultado.Ok)
            {
                MostrarErrorFormulario(resultado.Mensaje);
                return;
            }

            await DisplayAlert(
                "Pago registrado",
                $"Folio {resultado.Folio} · ${resultado.MontoUsd:0.##}",
                "OK");

            MontoEntry.Text = "";
            TasaEntry.Text = "";
            ReferenciaEntry.Text = "";
            FormularioBorder.IsVisible = false;
            MostrarFormularioButton.Text = "💵 REGISTRAR PAGO";

            // Refresca saldo/estado y el historial con el pago recién creado.
            var (ok, mensaje, alumnos) = await AppState.AdminService.ObtenerAlumnosAsync();
            if (ok)
            {
                var actualizado = alumnos.FirstOrDefault(a =>
                    string.Equals(a.Codigo, _alumno.Codigo, StringComparison.OrdinalIgnoreCase));

                if (actualizado != null)
                {
                    _alumno = actualizado;
                    AdminAlumnoContexto.Seleccionar(actualizado);
                    MostrarAlumno(actualizado);
                }
            }

            await CargarPagosAsync();
        }
        catch (Exception ex)
        {
            MostrarErrorFormulario($"Error: {ex.Message}");
        }
        finally
        {
            GuardarPagoButton.IsEnabled = true;
            GuardarPagoButton.Text = "GUARDAR PAGO";
        }
    }

    private void MostrarSolicitudButton_Clicked(object sender, EventArgs e)
    {
        SolicitudBorder.IsVisible = !SolicitudBorder.IsVisible;
        MostrarSolicitudButton.Text = SolicitudBorder.IsVisible
            ? "OCULTAR FORMULARIO"
            : "📤 SOLICITAR PAGO AL ALUMNO";
    }

    private async void EnviarSolicitudButton_Clicked(object sender, EventArgs e)
    {
        if (_alumno == null)
            return;

        if (!decimal.TryParse(SolicitudMontoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
        {
            MostrarErrorSolicitud("Escribe un monto en USD válido.");
            return;
        }

        string concepto = SolicitudConceptoEntry.Text?.Trim() ?? "";

        try
        {
            EnviarSolicitudButton.IsEnabled = false;
            EnviarSolicitudButton.Text = "ENVIANDO...";
            SolicitudErrorLabel.IsVisible = false;

            var (ok, mensaje, solicitudId) = await AppState.AdminService.SolicitarPagoAsync(
                _alumno.Codigo, monto, concepto);

            if (!ok)
            {
                MostrarErrorSolicitud(mensaje);
                return;
            }

            await DisplayAlert("Solicitud enviada", mensaje, "OK");

            SolicitudMontoEntry.Text = "";
            SolicitudConceptoEntry.Text = "";
            SolicitudBorder.IsVisible = false;
            MostrarSolicitudButton.Text = "📤 SOLICITAR PAGO AL ALUMNO";
        }
        catch (Exception ex)
        {
            MostrarErrorSolicitud($"Error: {ex.Message}");
        }
        finally
        {
            EnviarSolicitudButton.IsEnabled = true;
            EnviarSolicitudButton.Text = "ENVIAR SOLICITUD";
        }
    }

    private void MostrarErrorSolicitud(string mensaje)
    {
        SolicitudErrorLabel.Text = mensaje;
        SolicitudErrorLabel.IsVisible = true;
    }

    private async void Pago_Tapped(object sender, TappedEventArgs e)
    {
        if (_alumno == null)
            return;

        if (sender is not Grid grid || grid.BindingContext is not PagoAdminResumen pago)
            return;

        try
        {
            string? html = await AppState.AdminService.ObtenerReciboHtmlAsync(_alumno.Codigo, pago.Folio);

            if (string.IsNullOrWhiteSpace(html))
            {
                await DisplayAlert("Recibo", "No se pudo obtener el recibo.", "OK");
                return;
            }

            string path = Path.Combine(FileSystem.CacheDirectory, $"recibo_{pago.Folio}.html");
            File.WriteAllText(path, html);

            await Launcher.Default.OpenAsync(new OpenFileRequest(
                $"Recibo {pago.Folio}", new ReadOnlyFile(path)));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Recibo", $"No se pudo abrir el recibo: {ex.Message}", "OK");
        }
    }

    private async Task CargarPagosAsync()
    {
        if (_alumno == null)
            return;

        var (ok, mensaje, pagos) = await AppState.AdminService.ObtenerPagosAlumnoAsync(_alumno.Codigo);

        if (!ok)
        {
            await DisplayAlert("No se pudieron cargar los pagos", mensaje, "OK");
            PagosCollection.ItemsSource = new List<PagoAdminResumen>();
            return;
        }

        PagosCollection.ItemsSource = pagos;
    }

    private void MostrarAlumno(AlumnoAdminResumen alumno)
    {
        NombreLabel.Text = alumno.Nombre;
        CodigoLabel.Text = alumno.Codigo;
        CedulaLabel.Text = alumno.CedulaTexto;
        TelefonoLabel.Text = alumno.TelefonoTexto;
        InicialesPerfilLabel.Text = alumno.Iniciales;
        PlanLabel.Text = string.IsNullOrWhiteSpace(alumno.PlanNombre) ? "Sin plan" : alumno.PlanNombre;

        VencimientoLabel.Text = alumno.FechaVencimientoPlan?.ToString("dd/MM/yyyy") ?? "—";
        SaldoLabel.Text = $"${alumno.MontoPendiente:0.##}";

        Color estadoColor;
        switch (alumno.EstadoMembresia)
        {
            case "por_vencer":
                estadoColor = Color.FromArgb("#F59E0B");
                EstadoBadge.BackgroundColor = Color.FromArgb("#B45309");
                EstadoBadgeLabel.Text = "POR VENCER";
                break;
            case "moroso":
                estadoColor = Color.FromArgb("#EF4444");
                EstadoBadge.BackgroundColor = Color.FromArgb("#B91C1C");
                EstadoBadgeLabel.Text = "MOROSO";
                break;
            default:
                estadoColor = Color.FromArgb("#22C55E");
                EstadoBadge.BackgroundColor = Color.FromArgb("#16803A");
                EstadoBadgeLabel.Text = "ACTIVO";
                break;
        }

        FotoPerfilBorder.Stroke = estadoColor;
        CargarFotoAlumno(alumno.Codigo);
    }

    private void CargarFotoAlumno(string codigo)
    {
        try
        {
            if (AppState.IsConnected && !string.IsNullOrWhiteSpace(codigo))
            {
                string url = $"{AppState.CommunicationServerUrl.TrimEnd('/')}/api/alumnos/{Uri.EscapeDataString(codigo)}/foto?t={DateTime.UtcNow.Ticks}";
                FotoPerfilImage.Source = new UriImageSource
                {
                    Uri = new Uri(url),
                    CachingEnabled = false
                };
                FotoPerfilImage.IsVisible = true;
                InicialesPerfilLabel.IsVisible = false;
            }
            else
            {
                FotoPerfilImage.IsVisible = false;
                InicialesPerfilLabel.IsVisible = true;
            }
        }
        catch
        {
            FotoPerfilImage.IsVisible = false;
            InicialesPerfilLabel.IsVisible = true;
        }
    }

    private async void TomarFotoButton_Clicked(object sender, EventArgs e)
    {
        if (_alumno == null) return;

        if (!AppState.IsConnected)
        {
            await DisplayAlert("Sin conexión", "Conecta con Viking PC para actualizar la foto.", "OK");
            return;
        }

        string? opcion = await DisplayActionSheet(
            $"Foto de {_alumno.Nombre}",
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

            var (ok, mensaje) = await AppState.ClienteService.SubirFotoAlumnoAsync(_alumno.Codigo, bytes);
            if (ok)
            {
                await DisplayAlert("Listo", "Foto actualizada con éxito.", "OK");
                _alumno.TieneFoto = true;
                CargarFotoAlumno(_alumno.Codigo);
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

    private async Task<FileResult?> ElegirFotoAsync(bool usarCamara)
    {
        try
        {
            if (usarCamara)
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await DisplayAlert("Cámara no disponible", "Este dispositivo no permite capturar fotos directamente. Elige de la galería.", "OK");
                    return null;
                }
                return await MediaPicker.Default.CapturePhotoAsync();
            }

            return await MediaPicker.Default.PickPhotoAsync();
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("No disponible", "Función de cámara/galería no disponible en este dispositivo.", "OK");
            return null;
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permiso requerido", "Permite el acceso a la cámara o galería en la configuración del teléfono.", "OK");
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async void EditarDatosButton_Clicked(object sender, EventArgs e)
    {
        if (_alumno == null) return;

        if (!AppState.IsConnected)
        {
            await DisplayAlert("Sin conexión", "Conecta con Viking PC para actualizar los datos.", "OK");
            return;
        }

        string? cedula = await DisplayPromptAsync(
            "Cédula de Identidad",
            $"Cédula de {_alumno.Nombre}:",
            "Siguiente",
            "Cancelar",
            initialValue: _alumno.Cedula ?? "",
            keyboard: Keyboard.Text);

        if (cedula == null) return;

        string? telefono = await DisplayPromptAsync(
            "Teléfono",
            $"Teléfono de {_alumno.Nombre}:",
            "Guardar",
            "Cancelar",
            initialValue: _alumno.Telefono ?? "",
            keyboard: Keyboard.Telephone);

        if (telefono == null) return;

        var (ok, mensaje) = await AppState.ClienteService.ActualizarDatosAlumnoAsync(
            _alumno.Codigo, cedula.Trim(), telefono.Trim());

        if (ok)
        {
            await DisplayAlert("Listo", "Datos del alumno actualizados.", "OK");
            _alumno.Cedula = cedula.Trim();
            _alumno.Telefono = telefono.Trim();
            MostrarAlumno(_alumno);
        }
        else
        {
            await DisplayAlert("Error", mensaje, "OK");
        }
    }

    private async void WhatsAppButton_Clicked(object sender, EventArgs e)
    {
        if (_alumno == null) return;

        string numero = new string((_alumno.Telefono ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(numero))
        {
            await DisplayAlert("Sin teléfono", "Este alumno no tiene un número telefónico registrado. Toca '✏️ Datos' para agregarlo.", "OK");
            return;
        }

        if (numero.StartsWith("0") && numero.Length == 11)
        {
            numero = "58" + numero.Substring(1);
        }

        string mensaje = _alumno.EstadoMembresia switch
        {
            "moroso" => $"Hola {_alumno.Nombre}, te saludamos de Viking. Te recordamos cordialmente que tu plan presenta un saldo pendiente de ${_alumno.MontoPendiente:0.##}. Quedamos atentos.",
            "por_vencer" => $"Hola {_alumno.Nombre}, te saludamos de Viking. Te recordamos cordialmente que tu plan está próximo a vencer el {_alumno.FechaVencimientoPlan?.ToString("dd/MM/yyyy")}. ¡Te esperamos en tu entrenamiento!",
            _ => $"Hola {_alumno.Nombre}, te saludamos de Viking. ¿Cómo estás?"
        };

        string url = $"https://wa.me/{numero}?text={Uri.EscapeDataString(mensaje)}";
        try
        {
            await Launcher.Default.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            await DisplayAlert("WhatsApp", $"No se pudo abrir WhatsApp: {ex.Message}", "OK");
        }
    }

    private void MostrarErrorFormulario(string mensaje)
    {
        FormularioErrorLabel.Text = mensaje;
        FormularioErrorLabel.IsVisible = true;
    }
}

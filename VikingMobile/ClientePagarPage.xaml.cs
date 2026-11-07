using System.Globalization;
using VikingMobile.Application.Cliente;
using VikingMobile.Application.Communication;
using Microsoft.Maui.Media;

namespace VikingMobile;

public partial class ClientePagarPage : ContentPage
{
    private List<MetodoPagoClienteResumen> _metodos = new();
    private byte[]? _comprobanteBytes;
    private string _comprobanteContentType = "image/png";

    public ClientePagarPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var saldo = ClienteSession.Cuenta?.SaldoPendiente ?? 0;
        if (saldo > 0 && string.IsNullOrWhiteSpace(MontoEntry.Text))
            MontoEntry.Text = saldo.ToString("0.##", CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(ConceptoEntry.Text))
            ConceptoEntry.Text = "Mensualidad";

        await CargarMetodosAsync();
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task CargarMetodosAsync()
    {
        var (ok, mensaje, metodos) = await AppState.ClienteService.ObtenerMetodosPagoAsync();

        _metodos = ok ? metodos : new();

        MetodoPicker.ItemsSource = _metodos.Select(m => m.Metodo).ToList();
        SinMetodosLabel.IsVisible = _metodos.Count == 0;
        DatosBorder.IsVisible = false;
    }

    private void MetodoPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (MetodoPicker.SelectedIndex < 0 || MetodoPicker.SelectedIndex >= _metodos.Count)
        {
            DatosBorder.IsVisible = false;
            return;
        }

        DatosLabel.Text = _metodos[MetodoPicker.SelectedIndex].Datos;
        DatosBorder.IsVisible = !string.IsNullOrWhiteSpace(DatosLabel.Text);
    }

    private async void AdjuntarButton_Clicked(object sender, EventArgs e)
    {
        string? opcion = await DisplayActionSheet(
            "Adjuntar comprobante",
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

            await using Stream origen = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await origen.CopyToAsync(memoria);

            _comprobanteBytes = memoria.ToArray();
            _comprobanteContentType = archivo.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                       archivo.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? "image/jpeg"
                : "image/png";

            PreviewImagen.Source = ImageSource.FromStream(() => new MemoryStream(_comprobanteBytes));
            PreviewBorder.IsVisible = true;

            AdjuntarButton.Text = "✅ COMPROBANTE ADJUNTADO — TOCA PARA CAMBIARLO";
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo adjuntar la imagen: {ex.Message}");
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

    private async void EnviarButton_Clicked(object sender, EventArgs e)
    {
        if (MetodoPicker.SelectedIndex < 0 || MetodoPicker.SelectedIndex >= _metodos.Count)
        {
            MostrarError("Selecciona un método de pago.");
            return;
        }

        if (!decimal.TryParse(MontoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
        {
            MostrarError("Escribe un monto en USD válido.");
            return;
        }

        if (_comprobanteBytes == null || _comprobanteBytes.Length == 0)
        {
            MostrarError("Adjunta el comprobante de tu pago antes de enviar.");
            return;
        }

        string metodo = _metodos[MetodoPicker.SelectedIndex].Metodo;
        string concepto = ConceptoEntry.Text?.Trim() ?? "Mensualidad";

        try
        {
            EnviarButton.IsEnabled = false;
            EnviarButton.Text = "ENVIANDO...";
            ErrorLabel.IsVisible = false;

            var (ok, mensaje) = await AppState.ClienteService.CrearPagoAsync(
                monto, concepto, metodo, _comprobanteBytes, _comprobanteContentType);

            if (!ok)
            {
                MostrarError(mensaje);
                return;
            }

            await DisplayAlert("Pago enviado", mensaje, "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
        finally
        {
            EnviarButton.IsEnabled = true;
            EnviarButton.Text = "ENVIAR PAGO";
        }
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}

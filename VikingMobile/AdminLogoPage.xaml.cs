using VikingMobile.Application.Communication;
using Microsoft.Maui.Media;

namespace VikingMobile;

public partial class AdminLogoPage : ContentPage
{
    private byte[]? _logoBytes;
    private string _contentType = "image/png";

    public AdminLogoPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            byte[]? logo = await AppState.AdminService.ObtenerLogoPropioAsync();
            if (logo != null)
                MostrarPreview(logo);
        }
        catch
        {
            // Se queda con el estado "Sin logo todavía".
        }
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void ElegirButton_Clicked(object sender, EventArgs e)
    {
        string? opcion = await DisplayActionSheet(
            "Logo del establecimiento",
            "Cancelar",
            null,
            "📷 Tomar foto",
            "🖼️ Elegir de la galería");

        if (string.IsNullOrWhiteSpace(opcion) || opcion == "Cancelar")
            return;

        try
        {
            FileResult? archivo = opcion == "📷 Tomar foto"
                ? (MediaPicker.Default.IsCaptureSupported ? await MediaPicker.Default.CapturePhotoAsync() : null)
                : await MediaPicker.Default.PickPhotoAsync();

            if (archivo == null)
                return;

            await using Stream origen = await archivo.OpenReadAsync();
            using var memoria = new MemoryStream();
            await origen.CopyToAsync(memoria);

            _logoBytes = memoria.ToArray();
            _contentType = archivo.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           archivo.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                ? "image/jpeg"
                : "image/png";

            MostrarPreview(_logoBytes);
            GuardarButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MostrarError($"No se pudo elegir la imagen: {ex.Message}");
        }
    }

    private async void GuardarButton_Clicked(object sender, EventArgs e)
    {
        if (_logoBytes == null)
            return;

        try
        {
            GuardarButton.IsEnabled = false;
            GuardarButton.Text = "GUARDANDO...";
            ErrorLabel.IsVisible = false;

            var (ok, mensaje) = await AppState.AdminService.GuardarLogoAsync(_logoBytes, _contentType);

            if (!ok)
            {
                MostrarError(mensaje);
                return;
            }

            await DisplayAlert("Listo", "El logo se actualizó correctamente.", "OK");
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
        finally
        {
            GuardarButton.IsEnabled = true;
            GuardarButton.Text = "GUARDAR";
        }
    }

    private void MostrarPreview(byte[] bytes)
    {
        PreviewImagen.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        PreviewImagen.IsVisible = true;
        SinLogoLabel.IsVisible = false;
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}

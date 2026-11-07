using VikingMobile.Application.Cliente;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class ConfirmarComprobantePage : ContentPage
{
    public ConfirmarComprobantePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        byte[]? bytes = ComprobantePendienteContexto.Bytes;

        if (bytes == null || bytes.Length == 0)
        {
            _ = Shell.Current.GoToAsync("..");
            return;
        }

        PreviewImagen.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
    }

    private async void CancelarButton_Clicked(object sender, EventArgs e)
    {
        ComprobantePendienteContexto.Limpiar();
        await Shell.Current.GoToAsync("..");
    }

    private async void EnviarButton_Clicked(object sender, EventArgs e)
    {
        byte[]? bytes = ComprobantePendienteContexto.Bytes;
        long solicitudId = ComprobantePendienteContexto.SolicitudId;
        string contentType = ComprobantePendienteContexto.ContentType;

        if (bytes == null || bytes.Length == 0)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        try
        {
            EnviarButton.IsEnabled = false;
            EnviarButton.Text = "ENVIANDO...";

            var (ok, mensaje) = await AppState.ClienteService.SubirComprobanteAsync(
                solicitudId, bytes, contentType);

            await DisplayAlert(ok ? "Comprobante enviado" : "No se pudo subir", mensaje, "OK");

            ComprobantePendienteContexto.Limpiar();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo subir el comprobante: {ex.Message}", "OK");
        }
        finally
        {
            EnviarButton.IsEnabled = true;
            EnviarButton.Text = "ENVIAR COMPROBANTE";
        }
    }
}

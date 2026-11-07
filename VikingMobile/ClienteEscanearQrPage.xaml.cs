using VikingMobile.Application.Cliente;
using ZXing.Net.Maui;

namespace VikingMobile;

public partial class ClienteEscanearQrPage : ContentPage
{
    private bool _procesando;

    public ClienteEscanearQrPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _procesando = false;
        BarcodeReaderView.IsDetecting = true;
        StatusLabel.Text = "Escanea tu código QR";
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_procesando)
            return;

        var barcode = e.Results.FirstOrDefault();
        if (barcode == null)
            return;

        string contenido = barcode.Value?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(contenido))
            return;

        _procesando = true;
        BarcodeReaderView.IsDetecting = false;

        string codigo = ExtraerCodigoAlumno(contenido);

        if (string.IsNullOrWhiteSpace(codigo))
        {
            _procesando = false;
            BarcodeReaderView.IsDetecting = true;
            MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = "QR no válido, intenta de nuevo");
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ClienteQrContexto.Establecer(codigo);
            await Shell.Current.GoToAsync("..");
        });
    }

    // Mismo formato que ya usa el escáner de asistencia (MainPage), para
    // que sea literalmente el mismo QR que el alumno ya tiene impreso.
    private string ExtraerCodigoAlumno(string contenido)
    {
        if (contenido.StartsWith("VIKING|ALUMNO|"))
        {
            string[] partes = contenido.Split('|');
            if (partes.Length >= 3)
                return partes[2].Trim();
        }

        return contenido.Trim();
    }
}

using System.Globalization;
using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminPagoRevisionPage : ContentPage
{
    private SolicitudPagoAdminResumen? _solicitud;

    public AdminPagoRevisionPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _solicitud = AdminPagoContexto.SolicitudSeleccionada;

        if (_solicitud == null)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        MostrarSolicitud(_solicitud);
        await CargarComprobanteAsync();
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
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

    private void MostrarSolicitud(SolicitudPagoAdminResumen solicitud)
    {
        NombreLabel.Text = solicitud.NombreAlumno;
        CodigoLabel.Text = solicitud.CodigoAlumno;
        ConceptoLabel.Text = string.IsNullOrWhiteSpace(solicitud.Concepto) ? "Mensualidad" : solicitud.Concepto;
        FechaLabel.Text = solicitud.FechaCreacion;
        MontoLabel.Text = $"${solicitud.MontoUsd:0.##}";
    }

    private async Task CargarComprobanteAsync()
    {
        if (_solicitud == null)
            return;

        CargandoImagen.IsVisible = true;
        CargandoImagen.IsRunning = true;
        ComprobanteImagen.IsVisible = false;
        SinComprobanteLabel.IsVisible = false;

        try
        {
            var (ok, mensaje, imagen, _) = await AppState.AdminService.ObtenerComprobantePagoAsync(_solicitud.SolicitudId);

            if (!ok || imagen == null)
            {
                SinComprobanteLabel.Text = ok
                    ? "El alumno todavía no ha subido su comprobante."
                    : mensaje;
                SinComprobanteLabel.IsVisible = true;
                return;
            }

            ComprobanteImagen.Source = ImageSource.FromStream(() => new MemoryStream(imagen));
            ComprobanteImagen.IsVisible = true;
        }
        catch (Exception ex)
        {
            SinComprobanteLabel.Text = $"No se pudo cargar el comprobante: {ex.Message}";
            SinComprobanteLabel.IsVisible = true;
        }
        finally
        {
            CargandoImagen.IsVisible = false;
            CargandoImagen.IsRunning = false;
        }
    }

    private async void AprobarButton_Clicked(object sender, EventArgs e)
    {
        if (_solicitud == null)
            return;

        string metodo = MetodoPicker.SelectedItem as string ?? "Pagomovil";

        decimal.TryParse(TasaEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tasa);
        string referencia = ReferenciaEntry.Text?.Trim() ?? "";

        bool confirmar = await DisplayAlert(
            "Aprobar pago",
            $"¿Confirmas que {_solicitud.NombreAlumno} pagó ${_solicitud.MontoUsd:0.##} por {metodo}?",
            "Sí, aprobar",
            "Cancelar");

        if (!confirmar)
            return;

        try
        {
            AprobarButton.IsEnabled = false;
            RechazarButton.IsEnabled = false;
            AprobarButton.Text = "APROBANDO...";
            ErrorLabel.IsVisible = false;

            var (ok, mensaje, folio) = await AppState.AdminService.AprobarPagoAsync(
                _solicitud.SolicitudId, metodo, tasa, referencia);

            if (!ok)
            {
                MostrarError(mensaje);
                return;
            }

            await DisplayAlert("Pago aprobado", $"Folio {folio}", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
        finally
        {
            AprobarButton.IsEnabled = true;
            RechazarButton.IsEnabled = true;
            AprobarButton.Text = "APROBAR Y REGISTRAR PAGO";
        }
    }

    private async void RechazarButton_Clicked(object sender, EventArgs e)
    {
        if (_solicitud == null)
            return;

        string comentario = await DisplayPromptAsync(
            "Rechazar comprobante",
            "¿Por qué se rechaza? (el alumno lo verá)",
            "Rechazar",
            "Cancelar",
            "El comprobante no es legible, por favor súbelo de nuevo.");

        if (comentario == null)
            return;

        try
        {
            AprobarButton.IsEnabled = false;
            RechazarButton.IsEnabled = false;
            RechazarButton.Text = "RECHAZANDO...";
            ErrorLabel.IsVisible = false;

            var (ok, mensaje) = await AppState.AdminService.RechazarPagoAsync(_solicitud.SolicitudId, comentario);

            if (!ok)
            {
                MostrarError(mensaje);
                return;
            }

            await DisplayAlert("Solicitud rechazada", mensaje, "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
        finally
        {
            AprobarButton.IsEnabled = true;
            RechazarButton.IsEnabled = true;
            RechazarButton.Text = "RECHAZAR COMPROBANTE";
        }
    }

    private void MostrarError(string mensaje)
    {
        ErrorLabel.Text = mensaje;
        ErrorLabel.IsVisible = true;
    }
}

using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminPagosPendientesPage : ContentPage
{
    private List<SolicitudPagoAdminResumen> _todas = new();

    public AdminPagosPendientesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void ListaRefresh_Refreshing(object sender, EventArgs e)
    {
        await CargarAsync();
        ListaRefresh.IsRefreshing = false;
    }

    private void BuscarEntry_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltro();

    private async void Solicitud_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not SolicitudPagoAdminResumen solicitud)
            return;

        AdminPagoContexto.Seleccionar(solicitud);
        await Shell.Current.GoToAsync(nameof(AdminPagoRevisionPage));
    }

    private async Task CargarAsync()
    {
        var (ok, mensaje, solicitudes) = await AppState.AdminService.ObtenerPagosPendientesAsync();

        if (!ok)
        {
            await DisplayAlert("No se pudo cargar", mensaje, "OK");
            _todas = new();
        }
        else
        {
            _todas = solicitudes;
        }

        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        string busqueda = (BuscarEntry.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(busqueda))
        {
            SolicitudesCollection.ItemsSource = _todas;
            return;
        }

        SolicitudesCollection.ItemsSource = _todas
            .Where(s =>
                s.NombreAlumno.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                s.CodigoAlumno.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}

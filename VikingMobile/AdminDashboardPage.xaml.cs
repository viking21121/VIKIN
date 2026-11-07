using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminDashboardPage : ContentPage
{
    public AdminDashboardPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!AdminSession.EstaActiva)
        {
            _ = Shell.Current.GoToAsync($"//{nameof(AdminLoginPage)}");
            return;
        }

        MostrarDashboard(AdminSession.Dashboard!);
        await ActualizarContadorPendientesAsync();
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

    private async Task ActualizarContadorPendientesAsync()
    {
        try
        {
            var (ok, _, solicitudes) = await AppState.AdminService.ObtenerPagosPendientesAsync();
            int cantidad = ok ? solicitudes.Count : 0;

            PagosPendientesBadge.IsVisible = cantidad > 0;
            PagosPendientesBadgeLabel.Text = cantidad.ToString();
        }
        catch
        {
            PagosPendientesBadge.IsVisible = false;
        }
    }

    private async void ContenidoRefresh_Refreshing(object sender, EventArgs e)
    {
        try
        {
            var dashboard = await AppState.AdminService.RefrescarAsync();

            if (dashboard.Ok)
            {
                AdminSession.ActualizarDashboard(dashboard);
                MostrarDashboard(dashboard);
            }
            else if (!string.IsNullOrWhiteSpace(dashboard.Mensaje))
            {
                await DisplayAlert("No se pudo actualizar", dashboard.Mensaje, "OK");
            }

            await ActualizarContadorPendientesAsync();
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

    private async void AsistenciasGrid_Tapped(object sender, TappedEventArgs e)
    {
        // Reutiliza el flujo de Configuración → Scanner que ya
        // existía en la app antes del Modo Administrador.
        await Shell.Current.GoToAsync("//ConfiguracionPage");
    }

    private async void AlumnosGrid_Tapped(object sender, TappedEventArgs e)
    {
        AdminAlumnosPage.FiltroInicial = "todos";
        await Shell.Current.GoToAsync(nameof(AdminAlumnosPage));
    }

    private async void ReportesGrid_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminReportesPage));
    }

    private async void PagosPendientesGrid_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminPagosPendientesPage));
    }

    private async void MetodosPagoGrid_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminMetodosPagoPage));
    }

    private async void LogoGrid_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminLogoPage));
    }

    private async void PorVencerCard_Tapped(object sender, TappedEventArgs e)
    {
        AdminAlumnosPage.FiltroInicial = "por_vencer";
        await Shell.Current.GoToAsync(nameof(AdminAlumnosPage));
    }

    private async void MorososCard_Tapped(object sender, TappedEventArgs e)
    {
        AdminAlumnosPage.FiltroInicial = "moroso";
        await Shell.Current.GoToAsync(nameof(AdminAlumnosPage));
    }

    private async void CerrarSesionLabel_Tapped(object sender, TappedEventArgs e)
    {
        bool confirmar = await DisplayAlert(
            "Cerrar sesión",
            "¿Deseas cerrar tu sesión de administrador?",
            "Sí",
            "Cancelar");

        if (!confirmar)
            return;

        AdminSession.CerrarSesion();

        await Shell.Current.GoToAsync($"//{nameof(ModoSelectorPage)}");
    }

    private void MostrarDashboard(AdminDashboardResponse dashboard)
    {
        EstablecimientoLabel.Text = string.IsNullOrWhiteSpace(dashboard.Establecimiento)
            ? "🏢 Mi establecimiento"
            : $"🏢 {dashboard.Establecimiento}";

        TotalAlumnosLabel.Text = dashboard.TotalAlumnos.ToString();
        ActivosLabel.Text = dashboard.Activos.ToString();
        PorVencerLabel.Text = dashboard.PorVencer.ToString();
        MorososLabel.Text = dashboard.Morosos.ToString();

        RecaudadoLabel.Text = $"{dashboard.Moneda}{dashboard.RecaudadoMes:0.##}";
    }
    private async void Inicio_Tapped(object sender, EventArgs e)
    {
        bool confirmar = await DisplayAlert("Cerrar sesión", "¿Deseas volver al inicio?", "Sí", "Cancelar");
        if (confirmar)
        {
            AdminSession.CerrarSesion();
            await Shell.Current.GoToAsync("//ModoSelectorPage");
        }
    }

}

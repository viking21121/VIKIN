using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminReportesPage : ContentPage
{
    private static readonly string[] MesesEs =
    {
        "", "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    private readonly List<(int Anio, int Mes)> _periodos = new();
    private bool _cargando;

    public AdminReportesPage()
    {
        InitializeComponent();
        ConstruirSelectorDeMeses();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarTodoAsync();
    }

    private void ConstruirSelectorDeMeses()
    {
        var etiquetas = new List<string>();
        var hoy = DateTime.Today;

        for (int i = 0; i < 12; i++)
        {
            var fecha = hoy.AddMonths(-i);
            _periodos.Add((fecha.Year, fecha.Month));
            etiquetas.Add($"{MesesEs[fecha.Month]} {fecha.Year}");
        }

        // Evita que fijar el índice inicial dispare una carga de red
        // desde el constructor: OnAppearing ya se encarga de la
        // primera carga cuando la página realmente se muestra.
        _cargando = true;
        MesPicker.ItemsSource = etiquetas;
        MesPicker.SelectedIndex = 0;
        _cargando = false;
    }

    private async void MesPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_cargando)
            return;

        await CargarReportesAsync();
    }

    private async void ContenidoRefresh_Refreshing(object sender, EventArgs e)
    {
        await CargarTodoAsync();
        ContenidoRefresh.IsRefreshing = false;
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void PorVencer_Tapped(object sender, TappedEventArgs e)
    {
        AdminAlumnosPage.FiltroInicial = "por_vencer";
        await Shell.Current.GoToAsync(nameof(AdminAlumnosPage));
    }

    private async void Morosos_Tapped(object sender, TappedEventArgs e)
    {
        AdminAlumnosPage.FiltroInicial = "moroso";
        await Shell.Current.GoToAsync(nameof(AdminAlumnosPage));
    }

    private async Task CargarTodoAsync()
    {
        await CargarAlumnosAsync();
        await CargarReportesAsync();
    }

    private async Task CargarAlumnosAsync()
    {
        var (ok, mensaje, alumnos) = await AppState.AdminService.ObtenerAlumnosAsync();

        if (!ok)
        {
            PorVencerLabel.Text = "—";
            MorososLabel.Text = "—";
            return;
        }

        PorVencerLabel.Text = alumnos.Count(a => a.EstadoMembresia == "por_vencer").ToString();
        MorososLabel.Text = alumnos.Count(a => a.EstadoMembresia == "moroso").ToString();
    }

    private async Task CargarReportesAsync()
    {
        if (MesPicker.SelectedIndex < 0 || MesPicker.SelectedIndex >= _periodos.Count)
            return;

        _cargando = true;

        try
        {
            var (anio, mes) = _periodos[MesPicker.SelectedIndex];

            var reportes = await AppState.AdminService.ObtenerReportesAsync(anio, mes);

            if (!reportes.Ok)
            {
                await DisplayAlert("No se pudo cargar el reporte", reportes.Mensaje, "OK");
                return;
            }

            RecaudadoLabel.Text = $"${reportes.RecaudadoTotal:0.##}";
            CantidadPagosLabel.Text = $"{reportes.CantidadPagos} pagos registrados";
            MetodosCollection.ItemsSource = reportes.PagosPorMetodo;

            AsistenciasTotalLabel.Text = reportes.AsistenciasTotal.ToString();
            AlumnosAsistieronLabel.Text = reportes.AlumnosConAsistencia.ToString();
            DiasCollection.ItemsSource = reportes.AsistenciasPorDia;
        }
        finally
        {
            _cargando = false;
        }
    }
}

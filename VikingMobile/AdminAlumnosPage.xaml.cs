using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminAlumnosPage : ContentPage
{
    /// <summary>"todos" | "por_vencer" | "moroso". Se puede fijar antes
    /// de navegar aquí (por ejemplo, desde las tarjetas del dashboard)
    /// para abrir la lista ya filtrada.</summary>
    public static string FiltroInicial = "todos";

    private List<AlumnoAdminResumen> _todos = new();
    private string _filtroActual = "todos";

    public AdminAlumnosPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _filtroActual = FiltroInicial;
        FiltroInicial = "todos";
        AplicarEstiloFiltro();

        await CargarAlumnosAsync();
    }

    private async void VolverLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void ListaRefresh_Refreshing(object sender, EventArgs e)
    {
        await CargarAlumnosAsync();
        ListaRefresh.IsRefreshing = false;
    }

    private void BuscarEntry_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltros();

    private void FiltroTodos_Tapped(object sender, TappedEventArgs e)
    {
        _filtroActual = "todos";
        AplicarEstiloFiltro();
        AplicarFiltros();
    }

    private void FiltroPorVencer_Tapped(object sender, TappedEventArgs e)
    {
        _filtroActual = "por_vencer";
        AplicarEstiloFiltro();
        AplicarFiltros();
    }

    private void FiltroMoroso_Tapped(object sender, TappedEventArgs e)
    {
        _filtroActual = "moroso";
        AplicarEstiloFiltro();
        AplicarFiltros();
    }

    private async void Alumno_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not Border border || border.BindingContext is not AlumnoAdminResumen alumno)
            return;

        AdminAlumnoContexto.Seleccionar(alumno);
        await Shell.Current.GoToAsync(nameof(AdminAlumnoDetallePage));
    }

    private async Task CargarAlumnosAsync()
    {
        var (ok, mensaje, alumnos) = await AppState.AdminService.ObtenerAlumnosAsync();

        if (!ok)
        {
            await DisplayAlert("No se pudo cargar", mensaje, "OK");
            _todos = new();
        }
        else
        {
            _todos = alumnos;
        }

        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        string busqueda = (BuscarEntry.Text ?? "").Trim();

        var filtrados = _todos.Where(a =>
            _filtroActual == "todos" || a.EstadoMembresia == _filtroActual);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            filtrados = filtrados.Where(a =>
                a.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                a.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
        }

        AlumnosCollection.ItemsSource = filtrados.ToList();
    }

    private void AplicarEstiloFiltro()
    {
        (Border activo, Border pv, Border mo) = (FiltroTodosBorder, FiltroPorVencerBorder, FiltroMorosoBorder);

        foreach (var b in new[] { activo, pv, mo })
        {
            b.BackgroundColor = Color.FromArgb("#171717");
            b.Stroke = Color.FromArgb("#444444");
        }

        FiltroTodosLabel.TextColor = Colors.White;

        Border seleccionado = _filtroActual switch
        {
            "por_vencer" => pv,
            "moroso" => mo,
            _ => activo
        };

        seleccionado.BackgroundColor = Colors.White;
        seleccionado.Stroke = Colors.White;

        if (seleccionado == activo)
            FiltroTodosLabel.TextColor = Colors.Black;

        TituloLabel.Text = _filtroActual switch
        {
            "por_vencer" => "⚠️ Por vencer",
            "moroso" => "🔴 Morosos",
            _ => "👥 Alumnos"
        };
    }
}

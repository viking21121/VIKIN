using VikingMobile.Application.Admin;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminMetodosPagoPage : ContentPage
{
    private readonly List<(MetodoPagoAdminResumen Metodo, Switch ActivoSwitch, Editor DatosEditor)> _bloques = new();

    public AdminMetodosPagoPage()
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

    private async Task CargarAsync()
    {
        var (ok, mensaje, metodos) = await AppState.AdminService.ObtenerMetodosPagoAsync();

        if (!ok)
        {
            await DisplayAlert("No se pudo cargar", mensaje, "OK");
            return;
        }

        ConstruirBloques(metodos);
    }

    private void ConstruirBloques(List<MetodoPagoAdminResumen> metodos)
    {
        MetodosStack.Children.Clear();
        _bloques.Clear();

        foreach (var metodo in metodos)
        {
            var activoSwitch = new Switch
            {
                IsToggled = metodo.Activo,
                OnColor = Color.FromArgb("#4ADE80"),
                ThumbColor = Colors.White
            };

            var datosEditor = new Editor
            {
                Text = metodo.Datos,
                TextColor = Colors.White,
                PlaceholderColor = Color.FromArgb("#666666"),
                Placeholder = "Ej: Banco Mercantil\nTeléfono: 0414-1234567\nCédula: V-12345678\nTitular: Juan Pérez",
                AutoSize = EditorAutoSizeOption.TextChanges,
                MinimumHeightRequest = 70
            };

            var encabezado = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };

            var nombreLabel = new Label
            {
                Text = metodo.Metodo,
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };

            Grid.SetColumn(nombreLabel, 0);
            Grid.SetColumn(activoSwitch, 1);
            encabezado.Children.Add(nombreLabel);
            encabezado.Children.Add(activoSwitch);

            var contenido = new VerticalStackLayout
            {
                Spacing = 10,
                Children = { encabezado, datosEditor }
            };

            var border = new Border
            {
                Padding = 16,
                BackgroundColor = Color.FromArgb("#171717"),
                Stroke = Color.FromArgb("#444444"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Content = contenido
            };

            MetodosStack.Children.Add(border);
            _bloques.Add((metodo, activoSwitch, datosEditor));
        }
    }

    private async void GuardarButton_Clicked(object sender, EventArgs e)
    {
        var metodos = _bloques
            .Select(b => new MetodoPagoAdminResumen
            {
                Metodo = b.Metodo.Metodo,
                Clave = b.Metodo.Clave,
                Datos = b.DatosEditor.Text?.Trim() ?? "",
                Activo = b.ActivoSwitch.IsToggled
            })
            .ToList();

        try
        {
            GuardarButton.IsEnabled = false;
            GuardarButton.Text = "GUARDANDO...";
            EstadoLabel.IsVisible = false;

            var (ok, mensaje) = await AppState.AdminService.GuardarMetodosPagoAsync(metodos);

            if (!ok)
            {
                EstadoLabel.Text = mensaje;
                EstadoLabel.IsVisible = true;
                return;
            }

            await DisplayAlert("Guardado", "Los métodos de pago se actualizaron correctamente.", "OK");
        }
        catch (Exception ex)
        {
            EstadoLabel.Text = $"Error: {ex.Message}";
            EstadoLabel.IsVisible = true;
        }
        finally
        {
            GuardarButton.IsEnabled = true;
            GuardarButton.Text = "GUARDAR";
        }
    }
}

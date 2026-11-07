using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class ModoSelectorPage : ContentPage
{
    public ModoSelectorPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            byte[]? logo = await AppState.ObtenerLogoPublicoAsync();

            if (logo != null)
            {
                LogoEstablecimientoImagen.Source = ImageSource.FromStream(() => new MemoryStream(logo));
                LogoEstablecimientoImagen.IsVisible = true;
                LogoTextoLabel.IsVisible = false;
                MobileTextoLabel.IsVisible = false;
            }
        }
        catch
        {
            // Sin logo propio, se queda con el texto "VIKING MOBILE" de siempre.
        }
    }

    private async void AdministradorBorder_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AdminLoginPage));
    }

    private async void ClienteBorder_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ClienteLoginPage));
    }

    private async void EscanerLabel_Tapped(object sender, TappedEventArgs e)
    {
        // Ruta absoluta: reutiliza exactamente el flujo de
        // Configuración → Scanner que ya existía en la app.
        await Shell.Current.GoToAsync("//ConfiguracionPage");
    }

    private async void PrivacidadLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PoliticaPrivacidadPage));
    }
}

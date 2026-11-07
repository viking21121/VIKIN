using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class CorreoConfiguracionPage : ContentPage
{
    public CorreoConfiguracionPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CorreoEntry.Text = AppState.AuthService.SenderEmail;
        PasswordEntry.Text = "";
        EstadoLabel.IsVisible = false;
        try
        {
            if (await AppState.AuthService.TieneCorreoConfiguradoAsync())
                EstadoLabel.Text = "🟢 Correo configurado. Puedes reemplazar la contraseña de aplicación.";
            else
                EstadoLabel.Text = "🟡 Falta configurar la contraseña de aplicación.";
            EstadoLabel.IsVisible = true;
        }
        catch { }
    }

    private async void Guardar_Tapped(object sender, EventArgs e)
    {
        string password = PasswordEntry.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(password))
        {
            EstadoLabel.Text = "Ingresa la contraseña de aplicación de Gmail.";
            EstadoLabel.IsVisible = true;
            return;
        }

        await AppState.AuthService.ConfigurarCorreoAsync(password);
        EstadoLabel.Text = "🟢 Configuración guardada en este dispositivo.";
        EstadoLabel.IsVisible = true;
        await DisplayAlert("Viking", "Correo de verificación configurado.", "OK");
    }

    private async void Atras_Tapped(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}

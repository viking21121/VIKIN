using VikingMobile.Application.Activities;
using VikingMobile.Application.Admin;
using VikingMobile.Application.Auth;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class AdminLoginPage : ContentPage
{
    private bool _procesando;

    public AdminLoginPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EntrarButton.IsEnabled = true;
        EstadoLabel.IsVisible = false;

        if (AdminSession.EstaActiva)
            _ = Shell.Current.GoToAsync(nameof(AdminDashboardPage));
    }

    private async void EntrarButton_Clicked(object sender, EventArgs e)
    {
        if (_procesando) return;

        string usuario = UsuarioEntry.Text?.Trim() ?? "";
        string pin = PinEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(pin))
        {
            MostrarError("Ingresa el establecimiento y tu PIN.");
            return;
        }

        var cuenta = AppState.AuthService.ObtenerCuenta(MobileAccountRole.Administrador, usuario);
        if (cuenta == null)
        {
            MostrarError("Este establecimiento todavía no tiene un PIN Mobile. Pulsa CONFIGURAR MI PIN.");
            return;
        }

        if (!AppState.AuthService.VerificarPin(cuenta, pin))
        {
            MostrarError("Establecimiento o PIN incorrectos.");
            return;
        }

        try
        {
            _procesando = true;
            EntrarButton.IsEnabled = false;
            EntrarButton.Text = "VERIFICANDO...";

            if (!await AppState.ConnectAsync())
            {
                MostrarError("El administrador necesita conexión con Viking PC.");
                return;
            }

            var dashboard = await AppState.AdminService.IniciarSesionAsync(usuario);
            if (!dashboard.Ok)
            {
                MostrarError(dashboard.Mensaje);
                return;
            }

            AdminSession.Establecer(usuario, dashboard);
            await Shell.Current.GoToAsync(nameof(AdminDashboardPage));
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
        finally
        {
            _procesando = false;
            EntrarButton.Text = "ENTRAR";
            EntrarButton.IsEnabled = true;
        }
    }

    private async void CrearPin_Tapped(object sender, EventArgs e)
    {
        string usuario = UsuarioEntry.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(usuario))
        {
            MostrarError("Escribe el nombre exacto del establecimiento.");
            return;
        }

        if (ActividadContexto.ActividadId is null or 0)
        {
            MostrarError("Selecciona una actividad en Configuración antes de configurar tu PIN.");
            return;
        }

        var identidad = await AppState.AuthService.ObtenerIdentidadAsync(
            MobileAccountRole.Administrador, usuario);

        if (!identidad.Ok)
        {
            MostrarError(identidad.Mensaje);
            return;
        }

        string? correo = await DisplayPromptAsync(
            "Crear PIN de administrador",
            "Escribe el correo donde recibirás el código de verificación.",
            "Enviar código",
            "Cancelar",
            keyboard: Keyboard.Email);

        if (string.IsNullOrWhiteSpace(correo)) return;

        var envio = await AppState.AuthService.EnviarCodigoRegistroAsync(
            MobileAccountRole.Administrador, usuario, correo);

        if (!envio.Ok)
        {
            MostrarError(envio.Mensaje);
            return;
        }

        string? code = await DisplayPromptAsync(
            "Verificación",
            "Escribe el código de 6 dígitos que recibiste.",
            "Verificar",
            "Cancelar",
            keyboard: Keyboard.Numeric);

        if (!AppState.AuthService.VerificarCodigo(
            MobileAccountRole.Administrador, usuario, code ?? ""))
        {
            MostrarError("Código inválido, vencido o con demasiados intentos.");
            return;
        }

        string? pin = await DisplayPromptAsync(
            "Nuevo PIN",
            "Crea un PIN de 4 a 8 dígitos.",
            "Guardar",
            "Cancelar",
            keyboard: Keyboard.Numeric);

        if (!MobileAuthService.PinValido(pin ?? ""))
        {
            MostrarError("El PIN debe tener de 4 a 8 dígitos.");
            return;
        }

        if (AppState.AuthService.CrearCuentaConCodigo(
            MobileAccountRole.Administrador, usuario, code ?? "", pin!))
            await DisplayAlert("Listo", "PIN de administrador creado correctamente.", "OK");
        else
            MostrarError("No se pudo crear el PIN.");
    }

    private async void RecuperarPin_Tapped(object sender, EventArgs e)
    {
        string usuario = UsuarioEntry.Text?.Trim() ?? "";
        var cuenta = AppState.AuthService.ObtenerCuenta(
            MobileAccountRole.Administrador, usuario);

        if (cuenta == null)
        {
            MostrarError("No existe una cuenta Mobile para ese establecimiento.");
            return;
        }

        var envio = await AppState.AuthService.EnviarCodigoAsync(
            MobileAccountRole.Administrador, usuario);

        if (!envio.Ok)
        {
            MostrarError(envio.Mensaje);
            return;
        }

        string? code = await DisplayPromptAsync(
            "Recuperar PIN",
            "Escribe el código que recibiste por correo.",
            "Verificar",
            "Cancelar",
            keyboard: Keyboard.Numeric);

        if (!AppState.AuthService.VerificarCodigo(
            MobileAccountRole.Administrador, usuario, code ?? ""))
        {
            MostrarError("Código inválido o vencido.");
            return;
        }

        string? pin = await DisplayPromptAsync(
            "Nuevo PIN",
            "Escribe tu nuevo PIN de 4 a 8 dígitos.",
            "Guardar",
            "Cancelar",
            keyboard: Keyboard.Numeric);

        if (!MobileAuthService.PinValido(pin ?? ""))
        {
            MostrarError("PIN inválido.");
            return;
        }

        if (AppState.AuthService.CambiarPinConCodigo(
            MobileAccountRole.Administrador, usuario, pin!))
            await DisplayAlert("Listo", "PIN actualizado.", "OK");
        else
            MostrarError("No se pudo actualizar el PIN.");
    }

    private async void Atras_Tapped(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//ModoSelectorPage");

    private void MostrarError(string mensaje)
    {
        EstadoLabel.Text = mensaje;
        EstadoLabel.IsVisible = true;
    }
}

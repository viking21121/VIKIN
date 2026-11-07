using VikingMobile.Application.Activities;
using VikingMobile.Application.Auth;
using VikingMobile.Application.Cliente;
using VikingMobile.Application.Communication;

namespace VikingMobile;

public partial class ClienteLoginPage : ContentPage
{
    private bool _procesando;

    public ClienteLoginPage() => InitializeComponent();

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ContinuarButton.IsEnabled = true;
        EstadoLabel.IsVisible = false;

        string? codigoEscaneado = ClienteQrContexto.RecuperarYLimpiar();
        if (!string.IsNullOrWhiteSpace(codigoEscaneado))
            CodigoEntry.Text = codigoEscaneado.ToUpperInvariant();

        if (ClienteSession.EstaActiva)
            _ = Shell.Current.GoToAsync(nameof(MiCuentaPage));
    }

    private async void EscanearQrLabel_Tapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ClienteEscanearQrPage));
    }

    private async void ContinuarButton_Clicked(object sender, EventArgs e)
    {
        if (_procesando) return;

        string codigo = CodigoEntry.Text?.Trim().ToUpperInvariant() ?? "";
        string pin = PinEntry.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(pin))
        {
            MostrarError("Ingresa tu código y tu PIN.");
            return;
        }

        var cuenta = AppState.AuthService.ObtenerCuenta(MobileAccountRole.Cliente, codigo);
        if (cuenta == null)
        {
            MostrarError("Este alumno todavía no tiene un PIN Mobile. Pulsa CONFIGURAR MI PIN.");
            return;
        }

        if (!AppState.AuthService.VerificarPin(cuenta, pin))
        {
            MostrarError("Código o PIN incorrectos.");
            return;
        }

        try
        {
            _procesando = true;
            ContinuarButton.IsEnabled = false;
            ContinuarButton.Text = "CARGANDO...";

            if (AppState.IsConnected)
            {
                var cuentaPc = await AppState.ClienteService.IniciarSesionAsync(codigo);
                if (!cuentaPc.Ok)
                {
                    MostrarError(cuentaPc.Mensaje);
                    return;
                }

                ClienteSession.Establecer(codigo, cuentaPc);
            }
            else
            {
                var cached = await AppState.ClienteService.IniciarSesionAsync(codigo);
                if (!cached.Ok)
                {
                    MostrarError("Sin conexión y no hay información guardada para este alumno.");
                    return;
                }

                ClienteSession.Establecer(codigo, cached);
            }

            await Shell.Current.GoToAsync(nameof(MiCuentaPage));
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
        finally
        {
            _procesando = false;
            ContinuarButton.Text = "CONTINUAR";
            ContinuarButton.IsEnabled = true;
        }
    }

    private async void CrearPin_Tapped(object sender, EventArgs e)
    {
        string codigo = CodigoEntry.Text?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrWhiteSpace(codigo))
        {
            MostrarError("Primero escribe tu código de alumno.");
            return;
        }

        if (ActividadContexto.ActividadId is null or 0)
        {
            MostrarError("Selecciona una actividad en Configuración antes de configurar tu PIN.");
            return;
        }

        var resultado = await AppState.AuthService.ObtenerIdentidadAsync(MobileAccountRole.Cliente, codigo);
        if (!resultado.Ok)
        {
            MostrarError(resultado.Mensaje);
            return;
        }

        string? correo = await DisplayPromptAsync(
            "Crear PIN",
            "Escribe el correo donde recibirás el código de verificación.",
            "Enviar código",
            "Cancelar",
            keyboard: Keyboard.Email);

        if (string.IsNullOrWhiteSpace(correo)) return;

        var envio = await AppState.AuthService.EnviarCodigoRegistroAsync(
            MobileAccountRole.Cliente, codigo, correo);

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

        if (!AppState.AuthService.VerificarCodigo(MobileAccountRole.Cliente, codigo, code ?? ""))
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
            MobileAccountRole.Cliente, codigo, code ?? "", pin!))
            await DisplayAlert("Listo", "Tu PIN fue creado correctamente.", "OK");
        else
            MostrarError("No se pudo crear el PIN.");
    }

    private async void RecuperarPin_Tapped(object sender, EventArgs e)
    {
        string codigo = CodigoEntry.Text?.Trim().ToUpperInvariant() ?? "";
        var cuenta = AppState.AuthService.ObtenerCuenta(MobileAccountRole.Cliente, codigo);

        if (cuenta == null)
        {
            MostrarError("No existe una cuenta Mobile para ese código.");
            return;
        }

        var envio = await AppState.AuthService.EnviarCodigoAsync(
            MobileAccountRole.Cliente, codigo);

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
            MobileAccountRole.Cliente, codigo, code ?? ""))
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
            MobileAccountRole.Cliente, codigo, pin!))
            await DisplayAlert("Listo", "Tu PIN fue actualizado.", "OK");
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

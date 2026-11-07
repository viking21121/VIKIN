namespace VikingMobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Rutas del Modo Cliente y Modo Administrador. No se agregan
        // como ShellContent para no aparecer en el flyout: se navega
        // a ellas explícitamente desde ModoSelectorPage y los logins.
        Routing.RegisterRoute(nameof(ClienteLoginPage), typeof(ClienteLoginPage));
        Routing.RegisterRoute(nameof(ClienteEscanearQrPage), typeof(ClienteEscanearQrPage));
        Routing.RegisterRoute(nameof(MiCuentaPage), typeof(MiCuentaPage));
        Routing.RegisterRoute(nameof(ClientePagarPage), typeof(ClientePagarPage));
        Routing.RegisterRoute(nameof(ConfirmarComprobantePage), typeof(ConfirmarComprobantePage));
        Routing.RegisterRoute(nameof(AdminLoginPage), typeof(AdminLoginPage));
        Routing.RegisterRoute(nameof(AdminDashboardPage), typeof(AdminDashboardPage));
        Routing.RegisterRoute(nameof(AdminAlumnosPage), typeof(AdminAlumnosPage));
        Routing.RegisterRoute(nameof(AdminAlumnoDetallePage), typeof(AdminAlumnoDetallePage));
        Routing.RegisterRoute(nameof(AdminReportesPage), typeof(AdminReportesPage));
        Routing.RegisterRoute(nameof(AdminPagosPendientesPage), typeof(AdminPagosPendientesPage));
        Routing.RegisterRoute(nameof(AdminPagoRevisionPage), typeof(AdminPagoRevisionPage));
        Routing.RegisterRoute(nameof(AdminMetodosPagoPage), typeof(AdminMetodosPagoPage));
        Routing.RegisterRoute(nameof(AdminLogoPage), typeof(AdminLogoPage));
        Routing.RegisterRoute(nameof(CorreoConfiguracionPage), typeof(CorreoConfiguracionPage));
        Routing.RegisterRoute(nameof(PoliticaPrivacidadPage), typeof(PoliticaPrivacidadPage));
    }
}
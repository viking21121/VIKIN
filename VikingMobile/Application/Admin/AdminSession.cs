namespace VikingMobile.Application.Admin;

public static class AdminSession
{
    public static string Usuario { get; private set; } = "";
    public static int ActividadId { get; private set; }
    public static AdminDashboardResponse? Dashboard { get; private set; }
    public static bool EstaActiva => Dashboard != null;

    public static void Establecer(string usuario, AdminDashboardResponse dashboard)
    {
        Usuario = usuario.Trim();
        ActividadId = dashboard.ActividadId;
        Dashboard = dashboard;
    }

    public static void ActualizarDashboard(AdminDashboardResponse dashboard)
    {
        ActividadId = dashboard.ActividadId;
        Dashboard = dashboard;
    }

    public static void CerrarSesion()
    {
        Usuario = "";
        ActividadId = 0;
        Dashboard = null;
    }
}

using Microsoft.Extensions.DependencyInjection;
using VikingMobile.Diagnostics;

namespace VikingMobile
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App()
        {
            // Registrado antes que cualquier otra cosa: si algo truena
            // más adelante (incluso durante InitializeComponent), esto
            // ya queda enganchado. Nunca debe lanzar por sí mismo.
            CrashLogger.Inicializar();

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                CrashLogger.Registrar("App.InitializeComponent", ex);
                throw;
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                return new Window(new AppShell());
            }
            catch (Exception ex)
            {
                // Sin esto, un fallo aquí (por ejemplo al construir el
                // Shell o navegar a la primera página) hace que Android
                // termine el proceso en silencio, sin ningún mensaje.
                CrashLogger.Registrar("App.CreateWindow", ex);
                return new Window(new ErrorArranquePage(ex));
            }
        }
    }
}

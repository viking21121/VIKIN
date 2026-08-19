using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Infrastructure.SQLite;
using AsistenciaNatacion.Infrastructure.Updates;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                ApplicationConfiguration.Initialize();

                var actualizador = new UpdateChecker();
                if (actualizador.ComprobarYActualizarAsync().GetAwaiter().GetResult())
                    return;

                Database.Inicializar();
                
                // La selección de actividad se muestra siempre al iniciar.
                // Esto permite que el mismo producto pueda operar varias
                // actividades (Natación, CrossFit, Funcional, etc.) desde
                // una experiencia comercial consistente.
                using var selector = new SeleccionarActividadForm();

                if (selector.ShowDialog() != DialogResult.OK)
                    return;

                int actividadIdSeleccionada = selector.ActividadIdSeleccionada;

                ContextoActividad.ActividadId = actividadIdSeleccionada;

                System.Windows.Forms.Application.Run(
                    new MainForm(ContextoActividad.ActividadId));
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                File.WriteAllText(logPath, ex.ToString());
                MessageBox.Show($"Error fatal al iniciar la aplicación:\n\n{ex.Message}\n\nDetalles guardados en:\n{logPath}", "Error de inicio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
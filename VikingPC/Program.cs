using AsistenciaNatacion.Application.Mobile;
using AsistenciaNatacion.Infrastructure.Mobile;
using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Infrastructure.SQLite;
using AsistenciaNatacion.Infrastructure.Logging;
using AsistenciaNatacion.Presentation.Forms;
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

                // Iniciar API para Viking Mobile
                var registrarAsistenciaMovil =
    new RegistrarAsistenciaMovil(
        AppServices.CrearObtenerAlumnoPorCodigo(),
        AppServices.CrearMarcarAsistencia(),
        AppServices.CrearObtenerEstadoAlumno());

                VikingMobileApiHost.Iniciar(
     registrarAsistenciaMovil.Ejecutar,
     () => AppServices.CrearObtenerActividades().Ejecutar());

                // Instalación nueva (sin alumnos): asistente de bienvenida, una sola vez.
                // Un fallo aquí nunca debe impedir que la aplicación abra.
                try
                {
                    if (BienvenidaForm.Corresponde())
                    {
                        using var bienvenida = new BienvenidaForm(ContextoActividad.ActividadId);
                        bienvenida.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("El asistente de bienvenida falló.", ex);
                }

                System.Windows.Forms.Application.Run(
                    new MainForm(ContextoActividad.ActividadId));
            }
            catch (Exception ex)
            {
                Logger.Error("Error fatal al iniciar la aplicación.", ex);
                MessageBox.Show($"Error fatal al iniciar la aplicación:\n\n{ex.Message}\n\nDetalles guardados en la carpeta:\n{Logger.Carpeta}", "Error de inicio", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
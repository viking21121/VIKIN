using AsistenciaNatacion.Application.App;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class ImportarAsistenciaForm : Form
    {
        private readonly DateTimePicker dtpFecha = new();
        private readonly Button btnSeleccionarArchivo = new();
        private readonly Label lblInfo = new();
        public event Action? AsistenciasCargadas;

        public ImportarAsistenciaForm()
        {
            Text = "📥 Cargar Asistencias desde Celular";
            Size = new Size(480, 260);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lblFecha = new Label
            {
                Text = "Selecciona la fecha de la clase:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(30, 20),
                AutoSize = true
            };

            dtpFecha.Font = new Font("Segoe UI", 11);
            dtpFecha.Location = new Point(30, 45);
            dtpFecha.Size = new Size(400, 30);
            dtpFecha.Value = DateTime.Today;

            btnSeleccionarArchivo.Text = "📁 Seleccionar Archivo (.txt / .csv) y Procesar";
            btnSeleccionarArchivo.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            btnSeleccionarArchivo.Location = new Point(30, 95);
            btnSeleccionarArchivo.Size = new Size(400, 45);
            btnSeleccionarArchivo.BackColor = ColorTranslator.FromHtml("#0288d1");
            btnSeleccionarArchivo.ForeColor = Color.White;
            btnSeleccionarArchivo.FlatStyle = FlatStyle.Flat;
            btnSeleccionarArchivo.FlatAppearance.BorderSize = 0;
            btnSeleccionarArchivo.Click += BtnSeleccionarArchivo_Click;

            lblInfo.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
            lblInfo.Location = new Point(30, 150);
            lblInfo.Size = new Size(400, 40);
            lblInfo.ForeColor = Color.Gray;
            lblInfo.Text = "Exporta el historial de escaneos desde la app de tu teléfono en formato .txt o .csv y cárgala aquí al llegar a casa.";

            Controls.AddRange(new Control[] { lblFecha, dtpFecha, btnSeleccionarArchivo, lblInfo });
        }

        private void BtnSeleccionarArchivo_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Archivos de texto (*.txt;*.csv)|*.txt;*.csv";
            openFileDialog.Title = "Seleccionar archivo de asistencias del celular";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                DateTime fechaClase = dtpFecha.Value.Date;
                var resultado = AppServices.CrearCargarAsistenciasDesdeArchivo().Ejecutar(openFileDialog.FileName, fechaClase, ContextoActividad.ActividadId);

                MessageBox.Show(
                    $"¡Proceso Finalizado!\n\n" +
                    $"📅 Fecha: {fechaClase:dd/MM/yyyy}\n" +
                    $"✅ Asistencias registradas con éxito: {resultado.exito}\n" +
                    $"❌ Códigos no encontrados: {resultado.fallidos}",
                    "Carga Masiva Exitosa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                AsistenciasCargadas?.Invoke();
                Close();
            }
        }
    }
}
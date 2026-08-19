using AsistenciaNatacion.Application.App;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class AsistenciaHoyForm : Form
    {
        private readonly Alumno alumno;
        private readonly DateTimePicker dtpFecha = new();

        public event Action? Marcado;

        public AsistenciaHoyForm(Alumno alumno)
        {
            this.alumno = alumno;

            Text = $"Asistencia - {alumno.Nombre}";
            Size = new Size(430, 245);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var lblNombre = new Label
            {
                Text = alumno.Nombre,
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0b5394"),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            Controls.Add(lblNombre);

            var lblFecha = new Label
            {
                Text = "Fecha de asistencia:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 58)
            };
            Controls.Add(lblFecha);

            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.Value = DateTime.Today;
            dtpFecha.MaxDate = DateTime.Today;
            dtpFecha.MinDate = DateTime.Today.AddYears(-20);
            dtpFecha.Location = new Point(155, 53);
            dtpFecha.Width = 220;
            Controls.Add(dtpFecha);

            var lblAyuda = new Label
            {
                Text = "Puedes registrar o corregir asistencias de fechas anteriores.",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(20, 90)
            };
            Controls.Add(lblAyuda);

            var btnPresente = new Button
            {
                Text = "✔ Marcar presente",
                BackColor = ColorTranslator.FromHtml("#2e7d32"),
                ForeColor = Color.White,
                Location = new Point(20, 130),
                Width = 180,
                Height = 40
            };
            btnPresente.Click += (s, e) => Marcar(true);
            Controls.Add(btnPresente);

            var btnAusente = new Button
            {
                Text = "✘ Marcar ausente",
                BackColor = ColorTranslator.FromHtml("#c62828"),
                ForeColor = Color.White,
                Location = new Point(210, 130),
                Width = 180,
                Height = 40
            };
            btnAusente.Click += (s, e) => Marcar(false);
            Controls.Add(btnAusente);
        }

        private void Marcar(bool presente)
        {
            DateTime fecha = dtpFecha.Value.Date;

            AppServices.CrearMarcarAsistencia()
                .Ejecutar(alumno.Id, fecha, presente);

            string estado = presente ? "presente" : "ausente";

            MessageBox.Show(
                $"{alumno.Nombre} marcado como {estado} el {fecha:dd/MM/yyyy}.",
                "Registrado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            Marcado?.Invoke();
            Close();
        }
    }
}

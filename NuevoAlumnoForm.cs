using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Domain;
using AsistenciaNatacion.Presentation.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class NuevoAlumnoForm : Form
    {
        private readonly TextBox txtNombre = new();
        private readonly TextBox txtTelefono = new();
        private readonly TextBox txtCedula = new();
        private readonly TextBox txtCodigo = new();
        private readonly TextBox txtClasesMes = new();
        private readonly ComboBox cbPlan = new();
        private readonly DateTimePicker dtpFechaInscripcion = new();
        private readonly DateTimePicker dtpFechaProximoPago = new();
        private readonly int actividadId;
        private readonly List<PlanActividad> planes;
        private PlanActividad? planSeleccionado;

        public event Action? Guardado;

        private const int LabelX = 20;
        private const int FieldX = 160;
        private const int RowHeight = 35;

        public NuevoAlumnoForm(int actividadId)
        {
            this.actividadId = actividadId;
            planes = AppServices.CrearObtenerPlanesPorActividad().Ejecutar(actividadId);

            Text = "Nuevo alumno";
            Size = new Size(680, 650);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            KeyPreview = true;
            KeyDown += Form_KeyDown;

            var lblTitulo = new Label
            {
                Text = "Nuevo alumno",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0b5394"),
                AutoSize = true,
                Location = new Point(20, 15)
            };
            Controls.Add(lblTitulo);

            int y = 60;

            AddLabel("Nombre:", y);
            txtNombre.Location = new Point(FieldX, y - 3);
            txtNombre.Width = 300;
            Controls.Add(txtNombre);
            y += RowHeight;

            AddLabel("Cédula:", y);
            txtCedula.Location = new Point(FieldX, y - 3);
            txtCedula.Width = 300;
            Controls.Add(txtCedula);
            y += RowHeight;

            AddLabel("Teléfono:", y);
            txtTelefono.Location = new Point(FieldX, y - 3);
            txtTelefono.Width = 300;
            Controls.Add(txtTelefono);
            y += RowHeight;

            AddLabel("Código (QR):", y);
            txtCodigo.Location = new Point(FieldX, y - 3);
            txtCodigo.Width = 120;
            txtCodigo.Text = AppServices.CrearObtenerSiguienteCodigoAlumno().EjecutarGlobal();
            Controls.Add(txtCodigo);

            Controls.Add(new Label
            {
                Text = "(Único por alumno — se usa para la asistencia rápida)",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(txtCodigo.Right + 15, y)
            });
            y += RowHeight;

            AddLabel("Plan:", y);
            cbPlan.Location = new Point(FieldX, y - 6);
            cbPlan.Size = new Size(470, 32);
            cbPlan.DropDownStyle = ComboBoxStyle.DropDownList;
            cbPlan.DataSource = planes;
            cbPlan.SelectedIndexChanged += (s, e) => ActualizarPlanSeleccionado();
            Controls.Add(cbPlan);
            y += RowHeight + 35;

            AddLabel("Clases del mes:", y);
            txtClasesMes.Location = new Point(FieldX, y - 3);
            txtClasesMes.Width = 90;
            txtClasesMes.ReadOnly = true;
            Controls.Add(txtClasesMes);

            Controls.Add(new Label
            {
                Text = "(Definido por el plan; si no usa clases mensuales, no aplica)",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(FieldX + 100, y + 1)
            });
            y += RowHeight + 12;

            var grupoCiclo = new GroupBox
            {
                Text = "📅 Ciclo de pago individual",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0b5394"),
                Location = new Point(20, y),
                Size = new Size(620, 135)
            };

            grupoCiclo.Controls.Add(new Label
            {
                Text = "Fecha de inscripción:",
                AutoSize = true,
                Location = new Point(18, 30),
                ForeColor = Color.Black
            });

            dtpFechaInscripcion.Format = DateTimePickerFormat.Short;
            dtpFechaInscripcion.Value = DateTime.Today;
            dtpFechaInscripcion.MaxDate = DateTime.Today;
            dtpFechaInscripcion.Location = new Point(175, 25);
            dtpFechaInscripcion.Width = 145;
            dtpFechaInscripcion.ValueChanged += (s, e) =>
            {
                dtpFechaProximoPago.Value = ObtenerProximoPagoPredeterminado();
            };
            grupoCiclo.Controls.Add(dtpFechaInscripcion);

            grupoCiclo.Controls.Add(new Label
            {
                Text = "Próximo pago:",
                AutoSize = true,
                Location = new Point(350, 30),
                ForeColor = Color.Black
            });

            dtpFechaProximoPago.Format = DateTimePickerFormat.Short;
            dtpFechaProximoPago.Value = DateTime.Today.AddMonths(1);
            dtpFechaProximoPago.MinDate = DateTime.Today.AddYears(-20);
            dtpFechaProximoPago.MaxDate = DateTime.Today.AddYears(20);
            dtpFechaProximoPago.Location = new Point(450, 25);
            dtpFechaProximoPago.Width = 145;
            grupoCiclo.Controls.Add(dtpFechaProximoPago);

            grupoCiclo.Controls.Add(new Label
            {
                Text = "Cada alumno puede tener su propio día de pago. Puedes registrar alumnos hoy y elegir cuándo les corresponde pagar.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(570, 45),
                Location = new Point(18, 67)
            });

            Controls.Add(grupoCiclo);
            y += 155;

            var btnGuardar = new Button
            {
                Text = "Guardar",
                BackColor = ColorTranslator.FromHtml("#0b5394"),
                ForeColor = Color.White,
                Location = new Point(FieldX, y),
                Width = 120,
                Height = 34
            };
            btnGuardar.Click += (s, e) => Guardar();
            Controls.Add(btnGuardar);

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Location = new Point(btnGuardar.Right + 12, y),
                Width = 110,
                Height = 34
            };
            btnCancelar.Click += (s, e) => Close();
            Controls.Add(btnCancelar);

            if (planes.Count == 0)
            {
                btnGuardar.Enabled = false;
                MessageBox.Show(
                    "Esta actividad no tiene planes configurados. Crea al menos un plan antes de registrar alumnos.",
                    "Sin planes",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            ActualizarPlanSeleccionado();
            AppTheme.Aplicar(this);
        }

        private void AddLabel(string texto, int y)
            => Controls.Add(new Label
            {
                Text = texto,
                Location = new Point(LabelX, y),
                AutoSize = true
            });

        private DateTime ObtenerProximoPagoPredeterminado()
            => dtpFechaInscripcion.Value.Date.AddMonths(1);

        private void ActualizarPlanSeleccionado()
        {
            planSeleccionado = cbPlan.SelectedItem as PlanActividad;
            txtClasesMes.Text = planSeleccionado?.ClasesMes?.ToString() ?? "No aplica";
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Close();
            }
        }

        private void Guardar()
        {
            string nombre = txtNombre.Text.Trim();
            string telefono = txtTelefono.Text.Trim();
            string cedulaTexto = txtCedula.Text.Trim();
            string codigo = txtCodigo.Text.Trim();
            string? cedula = string.IsNullOrWhiteSpace(cedulaTexto) ? null : cedulaTexto;

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show(
                    "El nombre es obligatorio.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(codigo))
            {
                MessageBox.Show(
                    "El código es obligatorio (se usa para la asistencia rápida).",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (planSeleccionado == null)
            {
                MessageBox.Show(
                    "Selecciona un plan.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            DateTime fechaInscripcion = dtpFechaInscripcion.Value.Date;
            DateTime fechaProximoPago = dtpFechaProximoPago.Value.Date;

            if (fechaProximoPago <= fechaInscripcion)
            {
                MessageBox.Show(
                    "El próximo pago debe ser posterior a la fecha de inscripción.",
                    "Fechas inválidas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int alumnoId = AppServices.CrearRegistrarAlumno().Ejecutar(
                    nombre,
                    telefono,
                    cedula,
                    codigo,
                    planSeleccionado.Id,
                    actividadId,
                    fechaInscripcion,
                    fechaProximoPago);

                Guardado?.Invoke();

                var alumnoCreado = AppServices.CrearObtenerAlumno().Ejecutar(alumnoId);

                var opcionWa = MessageBox.Show(
                    $"¡{nombre} fue registrado con éxito!\n\n" +
                    $"📅 Inscripción: {fechaInscripcion:dd/MM/yyyy}\n" +
                    $"💳 Próximo pago: {fechaProximoPago:dd/MM/yyyy}\n\n" +
                    "¿Deseas abrir WhatsApp para enviarle la bienvenida y su Código QR?",
                    "Registro Exitoso",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (opcionWa == DialogResult.Yes && alumnoCreado != null)
                {
                    WhatsAppHelper.EnviarBienvenida(alumnoCreado);
                }
                else if (MessageBox.Show(
                    "¿Quieres ver / guardar la imagen de su código QR ahora?",
                    "Código QR",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    new QrForm(nombre, codigo).ShowDialog(this);
                }

                Close();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}

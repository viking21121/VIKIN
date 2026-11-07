using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Domain;
using AsistenciaNatacion.Presentation.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion;

public class EditarAlumnoForm : Form
{
    private readonly Alumno alumno;
    private readonly TextBox txtClasesMes = new();
    private readonly TextBox txtTelefono = new();
    private readonly ComboBox cbPlan = new();
    private readonly Label lblDetallePlan = new();
    private readonly DateTimePicker dtpFechaInscripcion = new();
    private readonly DateTimePicker dtpFechaProximoPago = new();
    private readonly List<PlanActividad> planes;
    private PlanActividad? planSeleccionado;

    public event Action? Guardado;

    public EditarAlumnoForm(Alumno alumno)
    {
        this.alumno = alumno;

        planes = AppServices
            .CrearObtenerPlanesPorActividad()
            .Ejecutar(alumno.ActividadId);

        Text = $"Editar cliente · {alumno.Nombre}";
        ClientSize = new Size(720, 585);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        KeyPreview = true;
        KeyDown += Form_KeyDown;

        // ---------- HEADER ----------

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = AppTheme.Primary
        };

        Controls.Add(header);

        header.Controls.Add(new Label
        {
            Text = "Editar cliente",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(24, 17)
        });

        header.Controls.Add(new Label
        {
            Text = "Actualiza su plan o datos de contacto sin alterar su identidad histórica.",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(220, 235, 248),
            AutoSize = true,
            Location = new Point(26, 52)
        });

        // ---------- IDENTIDAD ----------

        var identidad = new GroupBox
        {
            Text = "Identidad protegida",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AppTheme.Text,
            Location = new Point(24, 108),
            Size = new Size(672, 102)
        };

        Controls.Add(identidad);

        identidad.Controls.Add(new Label
        {
            Text = $"Nombre: {alumno.Nombre}",
            AutoSize = true,
            Location = new Point(16, 27)
        });

        identidad.Controls.Add(new Label
        {
            Text = $"Cédula: {(string.IsNullOrWhiteSpace(alumno.Cedula) ? "Sin registrar" : alumno.Cedula)}",
            AutoSize = true,
            Location = new Point(16, 56)
        });

        identidad.Controls.Add(new Label
        {
            Text = $"Código QR: {alumno.Codigo}",
            AutoSize = true,
            Location = new Point(300, 27)
        });

        identidad.Controls.Add(new Label
        {
            Text = "Estos datos no se editan desde esta pantalla para proteger historial y trazabilidad.",
            ForeColor = AppTheme.Muted,
            AutoSize = false,
            Width = 195,
            Height = 45,
            Location = new Point(300, 52)
        });

        // ---------- BOTÓN QR ----------

        var btnQr = new Button
        {
            Text = "Ver / reimprimir QR",
            Width = 150,
            Height = 30,
            Location = new Point(505, 22),
            BackColor = AppTheme.Surface
        };

        btnQr.Click += (s, e) =>
            new QrForm(alumno.Nombre, alumno.Codigo).ShowDialog(this);

        identidad.Controls.Add(btnQr);

        var btnCarnet = new Button
        {
            Text = "Carnet impreso…",
            Width = 150,
            Height = 30,
            Location = new Point(505, 60),
            BackColor = AppTheme.Surface
        };

        btnCarnet.Click += (s, e) =>
            new AsistenciaNatacion.Presentation.Forms.DatosCarnetForm(alumno).ShowDialog(this);

        identidad.Controls.Add(btnCarnet);

        // ---------- EDITOR ----------

        var editor = new GroupBox
        {
            Text = "Datos administrables",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AppTheme.Text,
            Location = new Point(24, 225),
            Size = new Size(672, 245)
        };

        Controls.Add(editor);

        // PLAN

        editor.Controls.Add(new Label
        {
            Text = "Plan:",
            AutoSize = true,
            Location = new Point(16, 30)
        });

        cbPlan.Location = new Point(120, 25);
        cbPlan.Width = 520;
        cbPlan.DropDownStyle = ComboBoxStyle.DropDownList;
        cbPlan.DataSource = planes;
        cbPlan.SelectedIndexChanged += (s, e) =>
            ActualizarPlanSeleccionado();

        editor.Controls.Add(cbPlan);

        // DETALLE PLAN

        editor.Controls.Add(new Label
        {
            Text = "Detalle:",
            AutoSize = true,
            Location = new Point(16, 69)
        });

        lblDetallePlan.AutoSize = false;
        lblDetallePlan.Width = 520;
        lblDetallePlan.Height = 28;
        lblDetallePlan.Location = new Point(120, 68);
        lblDetallePlan.ForeColor = AppTheme.Primary;

        editor.Controls.Add(lblDetallePlan);

        // CLASES

        editor.Controls.Add(new Label
        {
            Text = "Clases / mes:",
            AutoSize = true,
            Location = new Point(16, 108)
        });

        txtClasesMes.Location = new Point(125, 104);
        txtClasesMes.Width = 90;
        txtClasesMes.ReadOnly = true;

        editor.Controls.Add(txtClasesMes);

        // TELÉFONO

        editor.Controls.Add(new Label
        {
            Text = "Teléfono:",
            AutoSize = true,
            Location = new Point(250, 108)
        });

        txtTelefono.Text = alumno.Telefono ?? "";
        txtTelefono.Location = new Point(333, 104);
        txtTelefono.Width = 200;

        editor.Controls.Add(txtTelefono);

        // INSCRIPCIÓN

        editor.Controls.Add(new Label
        {
            Text = "Inscripción:",
            AutoSize = true,
            Location = new Point(16, 151)
        });

        dtpFechaInscripcion.Format = DateTimePickerFormat.Short;
        dtpFechaInscripcion.MaxDate = DateTime.Today;
        dtpFechaInscripcion.MinDate = DateTime.Today.AddYears(-20);

        dtpFechaInscripcion.Value =
            DateTime.TryParse(
                alumno.FechaInscripcion,
                out var fechaInscripcion)
                ? fechaInscripcion.Date
                : DateTime.Today;

        dtpFechaInscripcion.Location = new Point(120, 146);
        dtpFechaInscripcion.Width = 145;

        editor.Controls.Add(dtpFechaInscripcion);

        // PRÓXIMO PAGO

        editor.Controls.Add(new Label
        {
            Text = "Próximo pago:",
            AutoSize = true,
            Location = new Point(300, 151)
        });

        dtpFechaProximoPago.Format = DateTimePickerFormat.Short;
        dtpFechaProximoPago.MinDate = DateTime.Today.AddYears(-20);
        dtpFechaProximoPago.MaxDate = DateTime.Today.AddYears(20);

        dtpFechaProximoPago.Value =
            alumno.FechaVencimientoPlan?.Date
            ?? dtpFechaInscripcion.Value.Date.AddMonths(1);

        dtpFechaProximoPago.Location = new Point(435, 146);
        dtpFechaProximoPago.Width = 145;

        editor.Controls.Add(dtpFechaProximoPago);

        editor.Controls.Add(new Label
        {
            Text = "Puedes ajustar el ciclo individual sin cambiar el historial de pagos.",
            ForeColor = AppTheme.Muted,
            AutoSize = true,
            Location = new Point(120, 184)
        });

        // ---------- BOTÓN GUARDAR ----------

        var btnGuardar = new Button
        {
            Text = "Guardar cambios",
            Width = 145,
            Height = 34,
            Location = new Point(120, 506),
            BackColor = AppTheme.Primary,
            ForeColor = Color.White
        };

        btnGuardar.Click += (s, e) => Guardar();

        Controls.Add(btnGuardar);

        // ---------- BOTÓN DAR DE BAJA ----------

        var btnBaja = new Button
        {
            Text = "Dar de baja",
            Width = 120,
            Height = 34,
            Location = new Point(277, 506),
            BackColor = ColorTranslator.FromHtml("#B91C1C"),
            ForeColor = Color.White
        };

        btnBaja.Click += (s, e) => DarDeBaja();

        Controls.Add(btnBaja);

        // ---------- NUEVO: WHATSAPP ----------

        var btnWhatsApp = new Button
        {
            Text = "📲 Reenviar bienvenida",
            Width = 155,
            Height = 34,
            Location = new Point(405, 506),
            BackColor = ColorTranslator.FromHtml("#075E54"),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnWhatsApp.FlatAppearance.BorderSize = 0;

        btnWhatsApp.Click += (s, e) =>
        {
            // Usamos los datos actuales del formulario para que,
            // si el teléfono fue cambiado, se utilice el nuevo número.
            var alumnoActualizado = new Alumno
            {
                Id = alumno.Id,
                Nombre = alumno.Nombre,
                Cedula = alumno.Cedula,
                Codigo = alumno.Codigo,
                Telefono = txtTelefono.Text.Trim(),
                PlanId = planSeleccionado?.Id ?? alumno.PlanId,
                ActividadId = alumno.ActividadId,
                ClasesMes = planSeleccionado?.ClasesMes ?? alumno.ClasesMes,
                FechaInscripcion = dtpFechaInscripcion.Value.Date.ToString("yyyy-MM-dd"),
                FechaInicioPlan = dtpFechaInscripcion.Value.Date,
                FechaVencimientoPlan = dtpFechaProximoPago.Value.Date
            };

            WhatsAppHelper.EnviarBienvenida(alumnoActualizado);
        };

        Controls.Add(btnWhatsApp);

        // ---------- CANCELAR ----------

        var btnCancelar = new Button
        {
            Text = "Cancelar",
            Width = 100,
            Height = 34,
            Location = new Point(602, 506)
        };

        btnCancelar.Click += (s, e) => Close();

        Controls.Add(btnCancelar);

        // ---------- CARGAR PLAN ACTUAL ----------

        if (planes.Count > 0)
        {
            var planActual = planes.FirstOrDefault(
                p => p.Id == alumno.PlanId);

            if (planActual != null)
            {
                cbPlan.SelectedItem = planActual;
            }
            else
            {
                cbPlan.SelectedIndex = 0;
            }
        }
        else
        {
            cbPlan.SelectedIndex = -1;
        }

        ActualizarPlanSeleccionado();

        // ---------- VALIDACIÓN DE PLANES ----------

        if (planes.Count == 0)
        {
            btnGuardar.Enabled = false;

            MessageBox.Show(
                "Esta actividad no tiene planes configurados. Crea un plan antes de actualizar al cliente.",
                "Sin planes disponibles",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        // ---------- TEMA ----------

        AppTheme.Aplicar(this);

        btnGuardar.BackColor = AppTheme.Primary;
        btnGuardar.ForeColor = Color.White;

        btnBaja.BackColor =
            ColorTranslator.FromHtml("#B91C1C");

        btnBaja.ForeColor = Color.White;

        btnWhatsApp.BackColor =
            ColorTranslator.FromHtml("#075E54");

        btnWhatsApp.ForeColor = Color.White;
    }

    private void ActualizarPlanSeleccionado()
    {
        planSeleccionado = cbPlan.SelectedItem as PlanActividad;

        txtClasesMes.Text =
            planSeleccionado?.ClasesMes?.ToString()
            ?? "No aplica";

        lblDetallePlan.Text =
            planSeleccionado?.DescripcionCompleta
            ?? "Selecciona un plan";
    }

    private void Guardar()
    {
        if (planSeleccionado == null)
        {
            MessageBox.Show(
                "Selecciona un plan.",
                "Plan requerido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        try
        {
            DateTime fechaInicio =
                dtpFechaInscripcion.Value.Date;

            DateTime fechaProximoPago =
                dtpFechaProximoPago.Value.Date;

            if (fechaProximoPago <= fechaInicio)
            {
                MessageBox.Show(
                    "El próximo pago debe ser posterior a la fecha de inscripción.",
                    "Fechas inválidas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            AppServices.CrearActualizarAlumno().Ejecutar(
                alumno.Id,
                planSeleccionado.Id,
                txtTelefono.Text.Trim(),
                SesionUsuario.OperadorActual);

            AppServices.CrearActualizarCicloPago().Ejecutar(
                alumno.Id,
                fechaInicio,
                fechaProximoPago,
                SesionUsuario.OperadorActual);

            Guardado?.Invoke();

            Close();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                ex.Message,
                "No fue posible guardar",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
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

    private void DarDeBaja()
    {
        var r = MessageBox.Show(
            $"¿Dar de baja a {alumno.Nombre}?\n\n" +
            "El historial de pagos y asistencias se conservará.",
            "Confirmar baja",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (r != DialogResult.Yes)
            return;

        try
        {
            AppServices.CrearDesactivarAlumno().Ejecutar(
                alumno.Id,
                SesionUsuario.OperadorActual);

            Guardado?.Invoke();

            Close();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                ex.Message,
                "No fue posible completar la baja",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
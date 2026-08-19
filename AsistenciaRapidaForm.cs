using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Application.UseCases;
using System;
using System.Drawing;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class AsistenciaRapidaForm : Form
    {
        private readonly TextBox txtCodigo = new();
        private readonly Label lblMensaje = new();
        private readonly Button btnImportar = new();
        private readonly DateTimePicker dtpFecha = new();
        private readonly System.Windows.Forms.Timer timerLimpiar = new();
        private bool procesando = false;
        public event Action? AsistenciaRegistrada;

        public AsistenciaRapidaForm()
        {
            Text = "⚡ Escáner de Asistencia Rápida";
            Size = new Size(540, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            KeyPreview = true;
            KeyDown += Form_KeyDown;

            var lblInstrucciones = new Label
            {
                Text = "Apunta el teléfono al QR:",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(30, 18),
                AutoSize = true
            };

            // Botón de Carga Offline en la esquina superior derecha
            btnImportar.Text = "📥 Cargar Offline";
            btnImportar.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btnImportar.Location = new Point(360, 12);
            btnImportar.Size = new Size(130, 30);
            btnImportar.BackColor = ColorTranslator.FromHtml("#0288d1");
            btnImportar.ForeColor = Color.White;
            btnImportar.FlatStyle = FlatStyle.Flat;
            btnImportar.FlatAppearance.BorderSize = 0;
            btnImportar.Click += (s, e) => AbrirImportadorOffline();

            var lblFecha = new Label
            {
                Text = "Fecha de asistencia:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(30, 48),
                AutoSize = true
            };

            dtpFecha.Format = DateTimePickerFormat.Short;
            dtpFecha.Value = DateTime.Today;
            dtpFecha.MaxDate = DateTime.Today;
            dtpFecha.MinDate = DateTime.Today.AddYears(-20);
            dtpFecha.Location = new Point(170, 43);
            dtpFecha.Size = new Size(170, 28);

            txtCodigo.Font = new Font("Segoe UI", 14);
            txtCodigo.Location = new Point(30, 85);
            txtCodigo.Size = new Size(440, 35);
            txtCodigo.TextChanged += TxtCodigo_TextChanged;

            lblMensaje.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblMensaje.Location = new Point(30, 135);
            lblMensaje.Size = new Size(440, 180);
            lblMensaje.TextAlign = ContentAlignment.MiddleCenter;
            lblMensaje.BackColor = Color.LightGray;
            lblMensaje.Text = "Esperando escaneo desde el celular...";

            timerLimpiar.Interval = 10000;
            timerLimpiar.Tick += (s, e) =>
            {
                timerLimpiar.Stop();
                PrepararParaSiguienteEscaneo();
            };

            Controls.AddRange(new Control[] {
                lblInstrucciones,
                btnImportar,
                lblFecha,
                dtpFecha,
                txtCodigo,
                lblMensaje
            });

            Shown += (s, e) => ActivarFoco();
            Click += (s, e) => ActivarFoco();
            lblMensaje.Click += (s, e) => ActivarFoco();
        }

        private void ActivarFoco()
        {
            txtCodigo.Focus();
            txtCodigo.SelectAll();
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

        private void PrepararParaSiguienteEscaneo()
        {
            procesando = false;
            txtCodigo.Clear();
            ActivarFoco();
            lblMensaje.BackColor = Color.LightGray;
            lblMensaje.ForeColor = Color.Black;
            lblMensaje.Text = "Esperando escaneo desde el celular...";
        }

        private void AbrirImportadorOffline()
        {
            timerLimpiar.Stop();
            var form = new ImportarAsistenciaForm();
            form.AsistenciasCargadas += () =>
            {
                AsistenciaRegistrada?.Invoke();
                PrepararParaSiguienteEscaneo();
            };
            form.ShowDialog(this);
        }

        private void TxtCodigo_TextChanged(object? sender, EventArgs e)
        {
            if (procesando) return;

            string codigo = txtCodigo.Text.Trim();

            if (codigo.Length >= 3)
            {
                var alumno = AppServices.CrearObtenerAlumnoPorCodigo().Ejecutar(codigo, ContextoActividad.ActividadId);

                if (alumno != null)
                {
                    procesando = true;
                    ProcesarEscaneo(alumno);
                }
            }
        }

        private void ProcesarEscaneo(Alumno alumno)
        {
            DateTime fechaAsistencia = dtpFecha.Value.Date;

            // 1. Registrar la asistencia en la fecha seleccionada.
            AppServices.CrearMarcarAsistencia()
                .Ejecutar(alumno.Id, fechaAsistencia, true);

            // 2. Traer el estado actualizado del alumno para el mes de la asistencia.
            var estado = AppServices.CrearObtenerEstadoAlumno()
                .Ejecutar(alumno.Id, fechaAsistencia.Year, fechaAsistencia.Month);

            if (estado == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"✅ ¡Bienvenido/a {alumno.Nombre}!");
            sb.AppendLine($"Asistencia marcada para el {fechaAsistencia:dd/MM/yyyy}.");
            sb.AppendLine();

            bool requiereAtencion = false;

            // --- AVISOS DE CLASES DEL MES ---
            if (estado.Tomadas > estado.ClasesMes)
            {
                sb.AppendLine($"🚨 CLASES EXCEDIDAS: Lleva {estado.Tomadas} de {estado.ClasesMes} clases.");
                requiereAtencion = true;
            }
            else if (estado.Restantes == 0)
            {
                sb.AppendLine($"⚠️ ÚLTIMA CLASE: Con la de hoy completó sus {estado.ClasesMes} clases del mes.");
                requiereAtencion = true;
            }
            else if (estado.Restantes == 1)
            {
                sb.AppendLine($"⚠️ AVISO: Le queda 1 clase restante este mes.");
                requiereAtencion = true;
            }
            else
            {
                sb.AppendLine($"🏊 Clases: {estado.Tomadas} / {estado.ClasesMes} (quedan {estado.Restantes})");
            }

            // --- AVISOS DE PAGO PENDIENTE ---
            if (!estado.EstaPagado)
            {
                sb.AppendLine($"⚠️ PAGO PENDIENTE: Falta por pagar ${estado.MontoRestante:0.##}");
                requiereAtencion = true;
            }
            else
            {
                sb.AppendLine("💳 Cuota al día ($0 pendientes)");
            }

            lblMensaje.Text = sb.ToString().TrimEnd();

            // Cambiar color de fondo y reproducir sonido según la gravedad
            if (requiereAtencion)
            {
                SystemSounds.Exclamation.Play();
                lblMensaje.BackColor = ColorTranslator.FromHtml("#fff8e1");
                lblMensaje.ForeColor = ColorTranslator.FromHtml("#e65100");
            }
            else
            {
                SystemSounds.Asterisk.Play();
                lblMensaje.BackColor = ColorTranslator.FromHtml("#e8f5e9");
                lblMensaje.ForeColor = ColorTranslator.FromHtml("#2e7d32");
            }

            AsistenciaRegistrada?.Invoke();
            timerLimpiar.Start();
        }
    }
}
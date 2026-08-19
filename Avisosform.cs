using AsistenciaNatacion.Application.App;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class AvisosForm : Form
    {
        private readonly List<EstadoAlumnoMes> estados;
        public event Action? AbonoRegistrado;

        public AvisosForm(List<EstadoAlumnoMes> estados)
        {
            this.estados = estados;

            Text = "🔔 Avisos del Mes y Cobranza";
            Size = new Size(520, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int y = 15;

            // ---------- 1. Alumnos por vencer sus clases ----------
            var lblClases = new Label
            {
                Text = "⚠️ A punto de terminar sus clases del mes:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#d97706"),
                AutoSize = true,
                Location = new Point(15, y)
            };
            Controls.Add(lblClases);
            y += 25;

            var lstClases = new ListView
            {
                Location = new Point(15, y),
                Size = new Size(475, 140),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            lstClases.Columns.Add("Alumno", 220);
            lstClases.Columns.Add("Clases Restantes", 130);
            lstClases.Columns.Add("Estado", 100);

            var porVencerClases = estados
                .Where(e => e.ControlaClases && e.Estado != EstadoMes.Excedido && e.Restantes <= 1)
                .OrderBy(e => e.Restantes)
                .ToList();

            foreach (var est in porVencerClases)
            {
                var item = new ListViewItem(est.Nombre);
                item.SubItems.Add(est.Restantes == 1 ? "1 clase" : "0 clases");
                item.SubItems.Add(est.Restantes == 0 ? "⚠️ Agotado" : "🟡 Por vencer");
                item.Tag = est;
                lstClases.Items.Add(item);
            }
            Controls.Add(lstClases);

            y += 148;

            var btnRecordarClases = new Button
            {
                Text = "📲 Recordar Clases por WhatsApp",
                Location = new Point(15, y),
                Size = new Size(220, 30),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#075e54"),
                FlatStyle = FlatStyle.Flat
            };
            btnRecordarClases.FlatAppearance.BorderSize = 0;
            btnRecordarClases.Click += (s, e) => EnviarAvisoClasesWhatsApp(lstClases);
            Controls.Add(btnRecordarClases);

            y += 40;

            // ---------- 2. Alumnos con pago pendiente ----------
            var lblPagos = new Label
            {
                Text = "💵 Con pago pendiente o parcial:",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#c62828"),
                AutoSize = true,
                Location = new Point(15, y)
            };
            Controls.Add(lblPagos);
            y += 25;

            var lstPagos = new ListView
            {
                Location = new Point(15, y),
                Size = new Size(475, 140),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            lstPagos.Columns.Add("Alumno", 220);
            lstPagos.Columns.Add("Falta ($)", 110);
            lstPagos.Columns.Add("Condición", 120);

            var pendientesPago = estados
                .Where(e => !e.EstaPagado || e.MembresiaVencida)
                .OrderByDescending(e => e.MembresiaVencida)
                .ThenByDescending(e => e.MontoRestante)
                .ToList();

            foreach (var est in pendientesPago)
            {
                var item = new ListViewItem(est.Nombre);
                item.SubItems.Add($"${est.MontoRestante:0.##}");

                string condicion = est.MembresiaVencida
                    ? "🔴 Vencido"
                    : est.MontoAbonado > 0
                        ? "🟡 Abono Parcial"
                        : "❌ Sin Pagar";

                item.SubItems.Add(condicion);
                item.Tag = est;
                lstPagos.Items.Add(item);
            }
            Controls.Add(lstPagos);

            y += 148;

            var btnCobrar = new Button
            {
                Text = "💵 Registrar Abono",
                Location = new Point(15, y),
                Size = new Size(150, 30),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#2e7d32"),
                FlatStyle = FlatStyle.Flat
            };
            btnCobrar.FlatAppearance.BorderSize = 0;
            btnCobrar.Click += (s, e) => AbrirAbonoSeleccionado(lstPagos);
            Controls.Add(btnCobrar);

            var btnRecordarPago = new Button
            {
                Text = "📲 Recordar Cobro (WhatsApp)",
                Location = new Point(175, y),
                Size = new Size(210, 30),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#075e54"),
                FlatStyle = FlatStyle.Flat
            };
            btnRecordarPago.FlatAppearance.BorderSize = 0;
            btnRecordarPago.Click += (s, e) => EnviarAvisoCobroWhatsApp(lstPagos);
            Controls.Add(btnRecordarPago);

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(390, y),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9f)
            };
            btnCerrar.Click += (s, e) => Close();
            Controls.Add(btnCerrar);
        }

        private void EnviarAvisoClasesWhatsApp(ListView listView)
        {
            if (listView.SelectedItems.Count == 0 || listView.SelectedItems[0].Tag is not EstadoAlumnoMes est)
            {
                MessageBox.Show("Selecciona un alumno de la lista superior.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var alumno = AppServices.CrearObtenerAlumno().Ejecutar(est.AlumnoId);
            if (alumno == null || string.IsNullOrWhiteSpace(alumno.Telefono))
            {
                MessageBox.Show("El alumno no tiene teléfono registrado.", "Sin teléfono", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var configuracion = AppServices.CrearObtenerConfiguracion().Ejecutar(alumno.ActividadId);
            string nombreEstablecimiento = string.IsNullOrWhiteSpace(configuracion.Nombre)
                ? "el establecimiento"
                : configuracion.Nombre;
            string actividad = string.IsNullOrWhiteSpace(configuracion.TipoEstablecimiento)
                ? "la actividad"
                : configuracion.TipoEstablecimiento;

            string mensaje = $"¡Hola *{est.Nombre}*! 👋🏼\n\n" +
                             $"Te escribimos de *{nombreEstablecimiento}* para recordarte que te queda{(est.Restantes == 1 ? " *" + est.Restantes + " clase*" : "n *" + est.Restantes + " clases*")} disponibles de tu plan de *{actividad}*.\n\n" +
                             $"¡Te esperamos en los próximos entrenamientos!";

            AbrirWhatsapp(alumno.Telefono, mensaje);
        }

        private void EnviarAvisoCobroWhatsApp(ListView listView)
        {
            if (listView.SelectedItems.Count == 0 || listView.SelectedItems[0].Tag is not EstadoAlumnoMes est)
            {
                MessageBox.Show("Selecciona un alumno de la lista de pagos pendientes.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var alumno = AppServices.CrearObtenerAlumno().Ejecutar(est.AlumnoId);
            if (alumno == null || string.IsNullOrWhiteSpace(alumno.Telefono))
            {
                MessageBox.Show("El alumno no tiene teléfono registrado.", "Sin teléfono", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var configuracion = AppServices.CrearObtenerConfiguracion().Ejecutar(alumno.ActividadId);
            string nombreEstablecimiento = string.IsNullOrWhiteSpace(configuracion.Nombre)
                ? "el establecimiento"
                : configuracion.Nombre;
            string actividad = string.IsNullOrWhiteSpace(configuracion.TipoEstablecimiento)
                ? "la actividad"
                : configuracion.TipoEstablecimiento;

            string mensaje = $"¡Hola *{est.Nombre}*! 👋🏼\n\n" +
                             $"Esperamos que estés teniendo un excelente día.\n\n" +
                             $"Te escribimos de *{nombreEstablecimiento}* para recordarte que mantienes un saldo pendiente del mes por un monto de *${est.MontoRestante:0.##}* en *{actividad}*.\n\n" +
                             $"Agradecemos tu apoyo para realizar el pago correspondiente. ¡Muchas gracias!";

            AbrirWhatsapp(alumno.Telefono, mensaje);
        }

        private void AbrirAbonoSeleccionado(ListView listView)
        {
            if (listView.SelectedItems.Count == 0 || listView.SelectedItems[0].Tag is not EstadoAlumnoMes est)
            {
                MessageBox.Show("Selecciona un alumno con saldo pendiente.", "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var hoy = DateTime.Today;
            var form = new RegistrarAbonoForm(est, hoy.Year, hoy.Month);
            form.AbonoRegistrado += () => AbonoRegistrado?.Invoke();
            form.ShowDialog(this);
            Close();
        }

        private void AbrirWhatsapp(string rawPhone, string mensaje)
        {
            string telefonoLimpio = System.Text.RegularExpressions.Regex.Replace(rawPhone, @"[^\d]", "");
            if (telefonoLimpio.StartsWith("0") && telefonoLimpio.Length == 11) telefonoLimpio = "58" + telefonoLimpio.Substring(1);
            else if (telefonoLimpio.Length == 10 && (telefonoLimpio.StartsWith("4") || telefonoLimpio.StartsWith("2"))) telefonoLimpio = "58" + telefonoLimpio;

            string url = $"https://api.whatsapp.com/send?phone={telefonoLimpio}&text={Uri.EscapeDataString(mensaje)}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
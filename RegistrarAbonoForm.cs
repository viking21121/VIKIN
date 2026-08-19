using AsistenciaNatacion.Application.App;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class RegistrarAbonoForm : Form
    {
        private readonly int alumnoId;
        private readonly int anio;
        private readonly int mes;
        private readonly EstadoAlumnoMes estado;
        private readonly RegistrarPago registrarPago;

        private readonly NumericUpDown txtMonto = new();
        private readonly NumericUpDown nudTasaBcv = new();
        private readonly Button btnRefrescarBcv = new();
        private readonly Label lblMontoBs = new();
        private readonly Label lblEstadoTasa = new();

        private readonly ComboBox cbMetodoPago = new();
        private readonly TextBox txtReferencia = new();
        private readonly Label lblReferencia = new();
        private readonly CheckBox chkEnviarWhatsapp = new();

        public event Action? AbonoRegistrado;

        public RegistrarAbonoForm(EstadoAlumnoMes estado, int anio, int mes)
        {
            this.estado = estado;
            this.alumnoId = estado.AlumnoId;
            this.anio = anio;
            this.mes = mes;
            registrarPago = AppServices.CrearRegistrarPago();
            KeyPreview = true;
            KeyDown += Form_KeyDown;

            Text = $"💳 Registrar Abono - {estado.Nombre}";
            Size = new Size(410, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // 1. Información general del estado de cuenta
            var lblInfo = new Label
            {
                Text =
                    $"Cuota del ciclo: ${estado.CuotaTotal}   |   Abonado: ${estado.MontoAbonado}\n" +
                    $"Falta por pagar: ${estado.MontoRestante}" +
                    (estado.FechaVencimientoPlan.HasValue
                        ? $"   |   Próximo pago: {estado.FechaVencimientoPlan.Value:dd/MM/yyyy}"
                        : ""),
                Location = new Point(20, 15),
                Size = new Size(350, 40),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0b5394")
            };

            // 2. Campo: Monto a Abonar en Dólares ($)
            var lblMonto = new Label
            {
                Text = "Monto a Abonar ($):",
                Location = new Point(20, 65),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true
            };

            txtMonto.Location = new Point(20, 87);
            txtMonto.Size = new Size(155, 27);
            txtMonto.Font = new Font("Segoe UI", 10);
            txtMonto.DecimalPlaces = 2;
            txtMonto.Maximum = estado.MontoRestante > 0 ? estado.MontoRestante : 9999;
            txtMonto.Value = estado.MontoRestante > 0 ? estado.MontoRestante : 0;
            txtMonto.ValueChanged += (s, e) => RecalcularBs();

            // 3. Campo: Tasa BCV (Bs/$)
            var lblTasa = new Label
            {
                Text = "Tasa BCV (Bs/$):",
                Location = new Point(200, 65),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true
            };

            nudTasaBcv.Location = new Point(200, 87);
            nudTasaBcv.Size = new Size(122, 27);
            nudTasaBcv.Font = new Font("Segoe UI", 10);
            nudTasaBcv.DecimalPlaces = 2;
            nudTasaBcv.Maximum = 9999;
            nudTasaBcv.Value = 36.50m;
            nudTasaBcv.ValueChanged += (s, e) => RecalcularBs();

            btnRefrescarBcv.Text = "⟲";
            btnRefrescarBcv.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnRefrescarBcv.Location = new Point(327, 86);
            btnRefrescarBcv.Size = new Size(33, 29);
            btnRefrescarBcv.BackColor = ColorTranslator.FromHtml("#f0f0f0");
            btnRefrescarBcv.FlatStyle = FlatStyle.Flat;
            btnRefrescarBcv.FlatAppearance.BorderSize = 1;
            btnRefrescarBcv.FlatAppearance.BorderColor = Color.LightGray;
            btnRefrescarBcv.Click += async (s, e) => await CargarTasaBcvAsync();

            lblEstadoTasa.Location = new Point(200, 117);
            lblEstadoTasa.Font = new Font("Segoe UI", 8f, FontStyle.Italic);
            lblEstadoTasa.ForeColor = Color.Gray;
            lblEstadoTasa.Text = "🔄 Obteniendo BCV...";
            lblEstadoTasa.AutoSize = true;

            // 4. Método de Pago y Referencia
            var lblMetodo = new Label
            {
                Text = "Método de Pago:",
                Location = new Point(20, 140),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true
            };

            cbMetodoPago.Location = new Point(20, 162);
            cbMetodoPago.Size = new Size(155, 27);
            cbMetodoPago.Font = new Font("Segoe UI", 9.5f);
            cbMetodoPago.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMetodoPago.Items.AddRange(new object[] {
                "Pago Móvil",
                "Transferencia",
                "Efectivo ($)",
                "Efectivo (Bs)",
                "Zelle",
                "Otro"
            });
            cbMetodoPago.SelectedIndex = 0;
            cbMetodoPago.SelectedIndexChanged += CbMetodoPago_SelectedIndexChanged;

            lblReferencia.Text = "Nº Referencia:";
            lblReferencia.Location = new Point(200, 140);
            lblReferencia.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            AutoSize = true;

            txtReferencia.Location = new Point(200, 162);
            txtReferencia.Size = new Size(160, 27);
            txtReferencia.Font = new Font("Segoe UI", 9.5f);

            // 5. Panel con el Resultado en Bolívares
            var panelResultado = new Panel
            {
                Location = new Point(20, 205),
                Size = new Size(340, 65),
                BackColor = ColorTranslator.FromHtml("#e8f5e9"),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTituloBs = new Label
            {
                Text = "TOTAL A COBRAR EN BOLÍVARES (BS):",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#2e7d32"),
                Location = new Point(10, 8),
                AutoSize = true
            };

            lblMontoBs.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblMontoBs.ForeColor = ColorTranslator.FromHtml("#1b5e20");
            lblMontoBs.Location = new Point(10, 26);
            lblMontoBs.AutoSize = true;

            panelResultado.Controls.AddRange(new Control[] { lblTituloBs, lblMontoBs });

            // 6. Opciones adicionales
            chkEnviarWhatsapp.Text = "📲 Enviar recibo por WhatsApp";
            chkEnviarWhatsapp.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            chkEnviarWhatsapp.ForeColor = ColorTranslator.FromHtml("#075e54");
            chkEnviarWhatsapp.Location = new Point(20, 280);
            chkEnviarWhatsapp.Size = new Size(340, 22);
            chkEnviarWhatsapp.Checked = true;

            var chkImprimirRecibo = new CheckBox
            {
                Text = "🖨️ Generar recibo imprimible (HTML / PDF)",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = ColorTranslator.FromHtml("#0b5394"),
                Location = new Point(20, 304),
                Size = new Size(340, 22),
                Checked = true // Mostrar el recibo automáticamente después de guardar
            };

            // 7. Botones de Acción
            var btnGuardar = new Button
            {
                Text = "💵 Guardar (Enter)",
                Location = new Point(20, 340),
                Size = new Size(170, 38),
                BackColor = ColorTranslator.FromHtml("#2e7d32"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => Guardar(chkImprimirRecibo.Checked);

            var btnCancelar = new Button
            {
                Text = "Cancelar (Esc)",
                Location = new Point(200, 340),
                Size = new Size(160, 38),
                Font = new Font("Segoe UI", 9.5f)
            };
            btnCancelar.Click += (s, e) => Close();

            // Configurar soporte de Enter y Esc
            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            Controls.AddRange(new Control[] {
                lblInfo, lblMonto, txtMonto, lblTasa, nudTasaBcv, btnRefrescarBcv,
                lblEstadoTasa, lblMetodo, cbMetodoPago, lblReferencia, txtReferencia,
                panelResultado, chkEnviarWhatsapp, chkImprimirRecibo, btnGuardar, btnCancelar
            });

            Shown += async (s, e) =>
            {
                RecalcularBs();
                await CargarTasaBcvAsync();
            };
        }

        private void CbMetodoPago_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string metodo = cbMetodoPago.SelectedItem?.ToString() ?? "";
            if (metodo.StartsWith("Efectivo"))
            {
                txtReferencia.Enabled = false;
                txtReferencia.Clear();
            }
            else
            {
                txtReferencia.Enabled = true;
                txtReferencia.Focus();
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

        private async System.Threading.Tasks.Task CargarTasaBcvAsync()
        {
            btnRefrescarBcv.Enabled = false;
            lblEstadoTasa.ForeColor = Color.Gray;
            lblEstadoTasa.Text = "🔄 Obteniendo BCV...";

            decimal? tasa = await ServicioBcv.ObtenerTasaBcvAsync();

            if (tasa.HasValue && tasa.Value > 0)
            {
                nudTasaBcv.Value = tasa.Value;
                lblEstadoTasa.ForeColor = ColorTranslator.FromHtml("#2e7d32");
                lblEstadoTasa.Text = "🌐 Tasa BCV (Automática)";
            }
            else
            {
                lblEstadoTasa.ForeColor = ColorTranslator.FromHtml("#c62828");
                lblEstadoTasa.Text = "⚠️ Tasa Manual (Sin internet)";
            }

            btnRefrescarBcv.Enabled = true;
            RecalcularBs();
        }

        private void RecalcularBs()
        {
            decimal totalBs = txtMonto.Value * nudTasaBcv.Value;
            lblMontoBs.Text = $"{totalBs:N2} Bs.";
        }

        private void Guardar(bool imprimirRecibo = false)
        {
            if (txtMonto.Value <= 0)
            {
                MessageBox.Show("Ingresa un monto mayor a 0.", "Monto inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string metodo = cbMetodoPago.SelectedItem?.ToString() ?? "Efectivo ($)";
            string referencia = txtReferencia.Text.Trim();

            if (!metodo.StartsWith("Efectivo") && string.IsNullOrWhiteSpace(referencia))
            {
                MessageBox.Show("Por favor ingresa el número de referencia del pago.", "Referencia Requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReferencia.Focus();
                return;
            }

            if (!string.IsNullOrWhiteSpace(referencia))
            {
                string detalle; bool existe = AppServices.CrearExisteReferenciaPago().Ejecutar(referencia, out detalle);
                if (existe)
                {
                    MessageBox.Show(
                        $"🚨 ¡ATENCIÓN! La referencia #{referencia} ya fue registrada previamente.\n\nDetalle: {detalle}",
                        "Referencia Duplicada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    txtReferencia.Focus();
                    txtReferencia.SelectAll();
                    return;
                }
            }

            // 1. Guardar abono en la base de datos
            var pagoGuardado = registrarPago.Ejecutar(
    alumnoId,
    anio,
    mes,
    txtMonto.Value,
    DateTime.Now,
    metodo,
    referencia,
    nudTasaBcv.Value,
    txtMonto.Value * nudTasaBcv.Value);

            AbonoRegistrado?.Invoke();

            // 2. Enviar recibo por WhatsApp si la casilla está marcada
            if (chkEnviarWhatsapp.Checked)
            {
                EnviarReciboWhatsapp(metodo, referencia);
            }

            // 3. Generar y mostrar el recibo después de guardar correctamente.
            //    Se hace después de RegistrarAbono para garantizar que el pago ya exista.
            if (imprimirRecibo)
            {
                try
                {
                    var alumnoInfo = AppServices.CrearObtenerAlumno().Ejecutar(alumnoId);

                    string nombreAlumno = alumnoInfo?.Nombre ?? estado.Nombre;
                    string codigoAlumno = alumnoInfo?.Codigo ?? "";
                    string? cedulaAlumno = alumnoInfo?.Cedula;

                    decimal nuevoAbonado = estado.MontoAbonado + txtMonto.Value;
                    decimal nuevoRestante = Math.Max(estado.CuotaTotal - nuevoAbonado, 0);

                    ImpresionRecibo.MostrarRecibo(
                        nombreAlumno,
                        codigoAlumno,
                        cedulaAlumno,
                        anio,
                        mes,
                        txtMonto.Value,
                        nudTasaBcv.Value,
                        txtMonto.Value * nudTasaBcv.Value,
                        metodo,
                        referencia,
                        estado.CuotaTotal,
                       nuevoAbonado,
nuevoRestante,
pagoGuardado.Folio,
pagoGuardado.FechaPago

                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "El abono fue guardado correctamente, pero no se pudo generar o abrir el recibo.\n\n" +
                        ex.Message,
                        "Recibo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            Close();
        }

        private void EnviarReciboWhatsapp(string metodo, string referencia)
        {
            var alumnoInfo = AppServices.CrearObtenerAlumno().Ejecutar(alumnoId);
            if (alumnoInfo == null || string.IsNullOrWhiteSpace(alumnoInfo.Telefono))
            {
                MessageBox.Show("Abono guardado, pero el alumno no tiene un número de teléfono registrado.", "Sin Teléfono", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string telefonoLimpio = FormatearTelefonoVenezuela(alumnoInfo.Telefono);
            if (string.IsNullOrEmpty(telefonoLimpio))
            {
                MessageBox.Show("El número de teléfono del alumno no tiene un formato válido.", "Teléfono Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal nuevoAbonado = estado.MontoAbonado + txtMonto.Value;
            decimal nuevoRestante = Math.Max(estado.CuotaTotal - nuevoAbonado, 0);

            // Generar plantilla del mensaje según la actividad actual
            var configuracion = AppServices.CrearObtenerConfiguracion().Ejecutar();
            string nombreEstablecimiento = string.IsNullOrWhiteSpace(configuracion.Nombre)
                ? configuracion.TipoEstablecimiento
                : configuracion.Nombre;
            string actividad = string.IsNullOrWhiteSpace(configuracion.TipoEstablecimiento)
                ? "Actividad"
                : configuracion.TipoEstablecimiento;

            string mensaje = $"🏷️ *{nombreEstablecimiento} - Comprobante de Pago*\n" +
                             $"🏋️ *Actividad:* {actividad}\n\n" +
                             $"👤 *Alumno:* {estado.Nombre}\n" +
                             $"📅 *Fecha:* {DateTime.Now:dd/MM/yyyy hh:mm tt}\n" +
                             $"💵 *Monto Abonado:* ${txtMonto.Value:0.##} ({lblMontoBs.Text})\n" +
                             $"💳 *Método:* {metodo}\n" +
                             (!string.IsNullOrWhiteSpace(referencia) ? $"🔢 *Referencia:* #{referencia}\n" : "") +
                             $"\n📊 *Estado del ciclo de pago:*\n" +
                             $"• Cuota Total: ${estado.CuotaTotal:0.##}\n" +
                             $"• Total Abonado: ${nuevoAbonado:0.##}\n" +
                             $"• Resta por Pagar: ${nuevoRestante:0.##}\n" +
                             (estado.FechaVencimientoPlan.HasValue
                                 ? $"• Próximo pago: {estado.FechaVencimientoPlan.Value:dd/MM/yyyy}\n"
                                 : "") +
                             "\n" +
                             $"¡Muchas gracias por su pago! 🙌";

            string url = $"https://api.whatsapp.com/send?phone={telefonoLimpio}&text={Uri.EscapeDataString(mensaje)}";

            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo abrir WhatsApp automáticamente: {ex.Message}", "Error al abrir link", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string FormatearTelefonoVenezuela(string rawPhone)
        {
            // Deja solo los dígitos
            string soloNumeros = Regex.Replace(rawPhone, @"[^\d]", "");

            if (string.IsNullOrEmpty(soloNumeros)) return "";

            // Si empieza por 0 (ej. 04141234567) -> convierte a 584141234567
            if (soloNumeros.StartsWith("0") && soloNumeros.Length == 11)
            {
                return "58" + soloNumeros.Substring(1);
            }

            // Si empieza directamente por 4 (ej. 4141234567) -> convierte a 584141234567
            if (soloNumeros.Length == 10 && (soloNumeros.StartsWith("4") || soloNumeros.StartsWith("2")))
            {
                return "58" + soloNumeros;
            }

            // Si ya incluye el código de país (ej. 584141234567)
            if (soloNumeros.StartsWith("58") && soloNumeros.Length == 12)
            {
                return soloNumeros;
            }

            return soloNumeros;
        }
    }
}
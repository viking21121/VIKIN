using AsistenciaNatacion.Application.App;
using System;
using System.Drawing;
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

        private readonly ComboBox cbMonedaBcv = new();

        private readonly ComboBox cbMetodoPago = new();
        private readonly TextBox txtReferencia = new();
        private readonly Label lblReferencia = new();
        private readonly CheckBox chkEnviarWhatsapp = new();

        private readonly Label lblTituloResultado = new();

        public event Action? AbonoRegistrado;

        public RegistrarAbonoForm(
            EstadoAlumnoMes estado,
            int anio,
            int mes)
        {
            this.estado = estado;
            this.alumnoId = estado.AlumnoId;
            this.anio = anio;
            this.mes = mes;

            registrarPago = AppServices.CrearRegistrarPago();

            KeyPreview = true;
            KeyDown += Form_KeyDown;

            Text = $"💳 Registrar Abono - {estado.Nombre}";
            Size = new Size(435, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // ============================================================
            // 1. INFORMACIÓN GENERAL
            // ============================================================

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
                Font = new Font(
                    "Segoe UI",
                    9.5f,
                    FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0b5394")
            };

            // ============================================================
            // 2. MONTO A ABONAR
            // ============================================================

            var lblMonto = new Label
            {
                Text = "Monto a Abonar ($):",
                Location = new Point(20, 65),
                Font = new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold),
                AutoSize = true
            };

            txtMonto.Location = new Point(20, 87);
            txtMonto.Size = new Size(155, 27);
            txtMonto.Font = new Font("Segoe UI", 10);
            txtMonto.DecimalPlaces = 2;

            txtMonto.Maximum =
                estado.MontoRestante > 0
                    ? estado.MontoRestante
                    : 9999;

            txtMonto.Value =
                estado.MontoRestante > 0
                    ? estado.MontoRestante
                    : 0;

            txtMonto.ValueChanged += (s, e) =>
                RecalcularBs();

            // ============================================================
            // 3. MONEDA BCV
            // ============================================================

            var lblMonedaBcv = new Label
            {
                Text = "Moneda BCV:",
                Location = new Point(20, 125),
                Font = new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold),
                AutoSize = true
            };

            cbMonedaBcv.Location = new Point(20, 147);
            cbMonedaBcv.Size = new Size(155, 27);
            cbMonedaBcv.Font = new Font("Segoe UI", 9.5f);
            cbMonedaBcv.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cbMonedaBcv.Items.AddRange(
                new object[]
                {
                    "🇺🇸 Dólar BCV",
                    "🇪🇺 Euro BCV"
                });

            cbMonedaBcv.SelectedIndex = 0;

            cbMonedaBcv.SelectedIndexChanged +=
                async (s, e) =>
                {
                    if (!MetodoEsPagoDirectoEnDolares())
                        await CargarTasaBcvAsync();
                };

            // ============================================================
            // 4. TASA BCV
            // ============================================================

            var lblTasa = new Label
            {
                Text = "Tasa BCV (Bs/$):",
                Location = new Point(200, 65),
                Font = new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold),
                AutoSize = true
            };

            nudTasaBcv.Location = new Point(200, 87);
            nudTasaBcv.Size = new Size(122, 27);
            nudTasaBcv.Font = new Font("Segoe UI", 10);
            nudTasaBcv.DecimalPlaces = 2;
            nudTasaBcv.Maximum = 9999;
            nudTasaBcv.Value = 36.50m;

            nudTasaBcv.ValueChanged += (s, e) =>
                RecalcularBs();

            btnRefrescarBcv.Text = "⟲";
            btnRefrescarBcv.Font =
                new Font(
                    "Segoe UI",
                    10f,
                    FontStyle.Bold);

            btnRefrescarBcv.Location =
                new Point(327, 86);

            btnRefrescarBcv.Size =
                new Size(33, 29);

            btnRefrescarBcv.BackColor =
                ColorTranslator.FromHtml("#f0f0f0");

            btnRefrescarBcv.FlatStyle =
                FlatStyle.Flat;

            btnRefrescarBcv.FlatAppearance.BorderSize = 1;

            btnRefrescarBcv.FlatAppearance.BorderColor =
                Color.LightGray;

            btnRefrescarBcv.Click +=
                async (s, e) =>
                    await CargarTasaBcvAsync();

            lblEstadoTasa.Location =
                new Point(200, 117);

            lblEstadoTasa.Font =
                new Font(
                    "Segoe UI",
                    8f,
                    FontStyle.Italic);

            lblEstadoTasa.ForeColor =
                Color.Gray;

            lblEstadoTasa.Text =
                "🔄 Obteniendo BCV...";

            lblEstadoTasa.AutoSize = true;

            // ============================================================
            // 5. MÉTODO DE PAGO Y REFERENCIA
            // ============================================================

            var lblMetodo = new Label
            {
                Text = "Método de Pago:",
                Location = new Point(20, 180),
                Font = new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold),
                AutoSize = true
            };

            cbMetodoPago.Location =
                new Point(20, 202);

            cbMetodoPago.Size =
                new Size(155, 27);

            cbMetodoPago.Font =
                new Font("Segoe UI", 9.5f);

            cbMetodoPago.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cbMetodoPago.Items.AddRange(
                new object[]
                {
                    "Pago Móvil",
                    "Transferencia",
                    "Efectivo ($)",
                    "Efectivo (Bs)",
                    "Zelle",
                    "Binance Pay",
                    "Otro"
                });

            cbMetodoPago.SelectedIndex = 0;

            cbMetodoPago.SelectedIndexChanged +=
                CbMetodoPago_SelectedIndexChanged;

            lblReferencia.Text =
                "Nº Referencia:";

            lblReferencia.Location =
                new Point(200, 180);

            lblReferencia.Font =
                new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold);

            lblReferencia.AutoSize = true;

            txtReferencia.Location =
                new Point(200, 202);

            txtReferencia.Size =
                new Size(160, 27);

            txtReferencia.Font =
                new Font(
                    "Segoe UI",
                    9.5f);

            // ============================================================
            // 6. RESULTADO
            // ============================================================

            var panelResultado = new Panel
            {
                Location = new Point(20, 245),
                Size = new Size(340, 65),
                BackColor =
                    ColorTranslator.FromHtml("#e8f5e9"),
                BorderStyle =
                    BorderStyle.FixedSingle
            };

            lblTituloResultado.Text =
                "TOTAL A COBRAR EN BOLÍVARES (BS):";

            lblTituloResultado.Font =
                new Font(
                    "Segoe UI",
                    8f,
                    FontStyle.Bold);

            lblTituloResultado.ForeColor =
                ColorTranslator.FromHtml("#2e7d32");

            lblTituloResultado.Location =
                new Point(10, 8);

            lblTituloResultado.AutoSize = true;

            lblMontoBs.Font =
                new Font(
                    "Segoe UI",
                    15,
                    FontStyle.Bold);

            lblMontoBs.ForeColor =
                ColorTranslator.FromHtml("#1b5e20");

            lblMontoBs.Location =
                new Point(10, 26);

            lblMontoBs.AutoSize = true;

            panelResultado.Controls.AddRange(
                new Control[]
                {
                    lblTituloResultado,
                    lblMontoBs
                });

            // ============================================================
            // 7. OPCIONES
            // ============================================================

            chkEnviarWhatsapp.Text =
                "📲 Enviar recibo por WhatsApp";

            chkEnviarWhatsapp.Font =
                new Font(
                    "Segoe UI",
                    9f,
                    FontStyle.Bold);

            chkEnviarWhatsapp.ForeColor =
                ColorTranslator.FromHtml("#075e54");

            chkEnviarWhatsapp.Location =
                new Point(20, 320);

            chkEnviarWhatsapp.Size =
                new Size(340, 22);

            chkEnviarWhatsapp.Checked = true;

            var chkImprimirRecibo =
                new CheckBox
                {
                    Text =
                        "🖨️ Generar recibo imprimible (HTML / PDF)",

                    Font =
                        new Font(
                            "Segoe UI",
                            8.5f),

                    ForeColor =
                        ColorTranslator.FromHtml("#0b5394"),

                    Location =
                        new Point(20, 344),

                    Size =
                        new Size(340, 22),

                    Checked = true
                };

            // ============================================================
            // 8. BOTONES
            // ============================================================

            var btnGuardar =
                new Button
                {
                    Text =
                        "💵 Guardar (Enter)",

                    Location =
                        new Point(20, 380),

                    Size =
                        new Size(170, 38),

                    BackColor =
                        ColorTranslator.FromHtml("#2e7d32"),

                    ForeColor =
                        Color.White,

                    FlatStyle =
                        FlatStyle.Flat,

                    Font =
                        new Font(
                            "Segoe UI",
                            9.5f,
                            FontStyle.Bold)
                };

            btnGuardar.FlatAppearance.BorderSize = 0;

            btnGuardar.Click += (s, e) =>
                Guardar(
                    chkImprimirRecibo.Checked);

            var btnCancelar =
                new Button
                {
                    Text =
                        "Cancelar (Esc)",

                    Location =
                        new Point(200, 380),

                    Size =
                        new Size(160, 38),

                    Font =
                        new Font(
                            "Segoe UI",
                            9.5f)
                };

            btnCancelar.Click +=
                (s, e) => Close();

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;

            // ============================================================
            // 9. AGREGAR CONTROLES
            // ============================================================

            Controls.AddRange(
                new Control[]
                {
                    lblInfo,
                    lblMonto,
                    txtMonto,

                    lblMonedaBcv,
                    cbMonedaBcv,

                    lblTasa,
                    nudTasaBcv,
                    btnRefrescarBcv,
                    lblEstadoTasa,

                    lblMetodo,
                    cbMetodoPago,
                    lblReferencia,
                    txtReferencia,

                    panelResultado,

                    chkEnviarWhatsapp,
                    chkImprimirRecibo,

                    btnGuardar,
                    btnCancelar
                });

            // ============================================================
            // 10. CARGA INICIAL
            // ============================================================

            Shown += async (s, e) =>
            {
                RecalcularBs();
                await CargarTasaBcvAsync();
            };
        }

        // ================================================================
        // MÉTODO DE PAGO
        // ================================================================

        private void CbMetodoPago_SelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            string metodo =
                cbMetodoPago.SelectedItem?.ToString()
                ?? "";

            bool pagoDirecto =
                MetodoEsPagoDirectoEnDolares();

            // ------------------------------------------------------------
            // REFERENCIA
            // ------------------------------------------------------------

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

            // ------------------------------------------------------------
            // BCV
            // ------------------------------------------------------------

            nudTasaBcv.Enabled =
                !pagoDirecto;

            btnRefrescarBcv.Enabled =
                !pagoDirecto;

            cbMonedaBcv.Enabled =
                !pagoDirecto;

            // ------------------------------------------------------------
            // PAGO DIRECTO EN DÓLARES
            // ------------------------------------------------------------

            if (pagoDirecto)
            {
                lblTituloResultado.Text =
                    "TOTAL RECIBIDO EN DIVISA:";

                lblMontoBs.Text =
                    $"{txtMonto.Value:N2} USD";

                lblMontoBs.Font =
                    new Font(
                        "Segoe UI",
                        15,
                        FontStyle.Bold);

                lblMontoBs.ForeColor =
                    ColorTranslator.FromHtml("#075e54");

                lblEstadoTasa.ForeColor =
                    Color.Gray;

                lblEstadoTasa.Text =
                    "💵 Pago recibido directamente en dólares";
            }
            else
            {
                lblTituloResultado.Text =
                    "TOTAL A COBRAR EN BOLÍVARES (BS):";

                lblMontoBs.Font =
                    new Font(
                        "Segoe UI",
                        15,
                        FontStyle.Bold);

                lblMontoBs.ForeColor =
                    ColorTranslator.FromHtml("#1b5e20");

                RecalcularBs();

                // Al volver a un método que necesita BCV,
                // actualizamos la tasa.
                _ = CargarTasaBcvAsync();
            }
        }

        // ================================================================
        // DETERMINAR SI ES PAGO DIRECTO EN DÓLARES
        // ================================================================

        private bool MetodoEsPagoDirectoEnDolares()
        {
            string metodo =
                cbMetodoPago.SelectedItem?.ToString()
                ?? "";

            return metodo == "Zelle" ||
                   metodo == "Binance Pay" ||
                   metodo == "Efectivo ($)";
        }

        // ================================================================
        // OBTENER MONTO EN BOLÍVARES
        // ================================================================

        private decimal ObtenerMontoBs()
        {
            if (MetodoEsPagoDirectoEnDolares())
                return 0;

            return
                txtMonto.Value *
                nudTasaBcv.Value;
        }

        // ================================================================
        // TECLA ESC
        // ================================================================

        private void Form_KeyDown(
            object? sender,
            KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Close();
            }
        }

        // ================================================================
        // CARGAR TASA BCV
        // ================================================================

        private async System.Threading.Tasks.Task
            CargarTasaBcvAsync()
        {
            if (MetodoEsPagoDirectoEnDolares())
            {
                return;
            }

            btnRefrescarBcv.Enabled = false;
            cbMonedaBcv.Enabled = false;

            lblEstadoTasa.ForeColor =
                Color.Gray;

            lblEstadoTasa.Text =
                "🔄 Obteniendo tasa BCV...";

            MonedaBcv monedaSeleccionada =
                cbMonedaBcv.SelectedIndex == 1
                    ? MonedaBcv.Euro
                    : MonedaBcv.Dolar;

            decimal? tasa =
                await ServicioBcv.ObtenerTasaBcvAsync(
                    monedaSeleccionada);

            if (tasa.HasValue &&
                tasa.Value > 0)
            {
                nudTasaBcv.Value =
                    tasa.Value;

                lblEstadoTasa.ForeColor =
                    ColorTranslator.FromHtml("#2e7d32");

                lblEstadoTasa.Text =
                    monedaSeleccionada == MonedaBcv.Euro
                        ? "🌐 Tasa Euro BCV (Automática)"
                        : "🌐 Tasa Dólar BCV (Automática)";
            }
            else
            {
                lblEstadoTasa.ForeColor =
                    ColorTranslator.FromHtml("#c62828");

                lblEstadoTasa.Text =
                    "⚠️ Tasa Manual (Sin internet)";
            }

            cbMonedaBcv.Enabled = true;
            btnRefrescarBcv.Enabled = true;

            RecalcularBs();
        }

        // ================================================================
        // RECALCULAR BOLÍVARES
        // ================================================================

        private void RecalcularBs()
        {
            if (MetodoEsPagoDirectoEnDolares())
            {
                lblTituloResultado.Text =
                    "TOTAL RECIBIDO EN DIVISA:";

                lblMontoBs.Text =
                    $"{txtMonto.Value:N2} USD";

                return;
            }

            lblTituloResultado.Text =
                "TOTAL A COBRAR EN BOLÍVARES (BS):";

            decimal totalBs =
                txtMonto.Value *
                nudTasaBcv.Value;

            lblMontoBs.Text =
                $"{totalBs:N2} Bs.";
        }

        // ================================================================
        // GUARDAR
        // ================================================================

        private void Guardar(
            bool imprimirRecibo = false)
        {
            if (txtMonto.Value <= 0)
            {
                MessageBox.Show(
                    "Ingresa un monto mayor a 0.",
                    "Monto inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string metodo =
                cbMetodoPago.SelectedItem?.ToString()
                ?? "Efectivo ($)";

            string referencia =
                txtReferencia.Text.Trim();

            // ============================================================
            // REFERENCIA OBLIGATORIA
            // ============================================================

            if (!metodo.StartsWith("Efectivo") &&
                string.IsNullOrWhiteSpace(referencia))
            {
                MessageBox.Show(
                    "Por favor ingresa el número de referencia del pago.",
                    "Referencia Requerida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtReferencia.Focus();

                return;
            }

            // ============================================================
            // REFERENCIA DUPLICADA
            // ============================================================

            if (!string.IsNullOrWhiteSpace(referencia))
            {
                string detalle;

                bool existe =
                    AppServices.CrearExisteReferenciaPago()
                        .Ejecutar(
                            referencia,
                            out detalle);

                if (existe)
                {
                    MessageBox.Show(
                        $"🚨 ¡ATENCIÓN! La referencia #{referencia} ya fue registrada previamente.\n\n" +
                        $"Detalle: {detalle}",
                        "Referencia Duplicada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    txtReferencia.Focus();
                    txtReferencia.SelectAll();

                    return;
                }
            }

            // ============================================================
            // CALCULAR MONTO EN BS
            // ============================================================

            decimal montoBs =
                ObtenerMontoBs();

            // Para pagos directos en dólares,
            // la tasa BCV no aplica.
            decimal tasaBcv =
                MetodoEsPagoDirectoEnDolares()
                    ? 0
                    : nudTasaBcv.Value;

            // ============================================================
            // GUARDAR ABONO
            // ============================================================

            var pagoGuardado =
                registrarPago.Ejecutar(
                    alumnoId,
                    anio,
                    mes,
                    txtMonto.Value,
                    DateTime.Now,
                    metodo,
                    referencia,
                    tasaBcv,
                    montoBs);

            AbonoRegistrado?.Invoke();

            // ============================================================
            // WHATSAPP
            // ============================================================

            if (chkEnviarWhatsapp.Checked)
            {
                try
                {
                    var alumnoInfo =
                        AppServices.CrearObtenerAlumno()
                            .Ejecutar(alumnoId);

                    if (alumnoInfo != null)
                    {
                        decimal nuevoAbonado =
                            estado.MontoAbonado +
                            txtMonto.Value;

                        decimal nuevoRestante =
                            Math.Max(
                                estado.CuotaTotal -
                                nuevoAbonado,
                                0);

                        WhatsAppHelper.EnviarRecibo(
                            alumnoInfo,
                            pagoGuardado,
                            estado.CuotaTotal,
                            nuevoAbonado,
                            nuevoRestante);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "El abono fue guardado correctamente, " +
                        "pero no se pudo preparar el mensaje de WhatsApp.\n\n" +
                        ex.Message,
                        "WhatsApp",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            // ============================================================
            // RECIBO
            // ============================================================

            if (imprimirRecibo)
            {
                try
                {
                    var alumnoInfo =
                        AppServices.CrearObtenerAlumno()
                            .Ejecutar(alumnoId);

                    string nombreAlumno =
                        alumnoInfo?.Nombre ??
                        estado.Nombre;

                    string codigoAlumno =
                        alumnoInfo?.Codigo ??
                        "";

                    string? cedulaAlumno =
                        alumnoInfo?.Cedula;

                    decimal nuevoAbonado =
                        estado.MontoAbonado +
                        txtMonto.Value;

                    decimal nuevoRestante =
                        Math.Max(
                            estado.CuotaTotal -
                            nuevoAbonado,
                            0);

                    ImpresionRecibo.MostrarRecibo(
                        nombreAlumno,
                        codigoAlumno,
                        cedulaAlumno,
                        anio,
                        mes,
                        txtMonto.Value,
                        tasaBcv,
                        montoBs,
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
    }
}
using AsistenciaNatacion.Application.App;
﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class HistorialMesesForm : Form
    {
        private readonly int actividadId;
        private readonly ComboBox cbMes = new();
        private readonly NumericUpDown nudAnio = new();
        private readonly Button btnConsultar = new();
        private readonly TextBox txtBuscarCodigo = new();
        private readonly DataGridView dgvHistorial = new();
        private readonly DataGridView dgvRecibos = new();
        private readonly ObtenerPagosAlumnoPeriodo obtenerPagosAlumnoPeriodo;
        private List<Pago> pagosAlumnoSeleccionado = new();
        private Label lblRecibos = new();
        private Button btnVerRecibo = new();
        private Button btnWhatsAppRecibo = new();
        private readonly ObtenerPagoPorFolio obtenerPagoPorFolio;

        // Guardamos la lista con el tipo exacto que retorna la base de datos
        private List<EstadoAlumnoMes> listaCompletaMes = new();

        public HistorialMesesForm(int actividadId)
        {
            this.actividadId = actividadId;
            Text = "📊 Historial de Asistencias y Pagos por Mes";
            Size = new Size(950, 780);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ColorTranslator.FromHtml("#f8f9fa");
            KeyPreview = true;
            KeyDown += Form_KeyDown;

            obtenerPagosAlumnoPeriodo =
    AppServices.CrearObtenerPagosAlumnoPeriodo();
            obtenerPagoPorFolio =
    AppServices.CrearObtenerPagoPorFolio();

            var panelTop = new FlowLayoutPanel
            {
                Location = new Point(20, 15),
                Size = new Size(894, 40),
                AutoSize = false,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            // 1. Label Mes
            var lblMes = new Label
            {
                Text = "Mes:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            // 2. ComboBox Mes
            cbMes.Font = new Font("Segoe UI", 10);
            cbMes.Size = new Size(130, 28);
            cbMes.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMes.Margin = new Padding(0, 2, 15, 0);
            cbMes.Items.AddRange(new string[] {
                "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
            });
            cbMes.SelectedIndex = DateTime.Today.Month - 1;

            // 3. Label Año
            var lblAnio = new Label
            {
                Text = "Año:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            // 4. NumericUpDown Año
            nudAnio.Font = new Font("Segoe UI", 10);
            nudAnio.Size = new Size(75, 28);
            nudAnio.Minimum = 2024;
            nudAnio.Maximum = 2035;
            nudAnio.Value = DateTime.Today.Year;
            nudAnio.Margin = new Padding(0, 2, 15, 0);

            // 5. Botón Consultar
            btnConsultar.Text = "🔍 Consultar";
            btnConsultar.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnConsultar.Size = new Size(110, 30);
            btnConsultar.BackColor = ColorTranslator.FromHtml("#0288d1");
            btnConsultar.ForeColor = Color.White;
            btnConsultar.FlatStyle = FlatStyle.Flat;
            btnConsultar.FlatAppearance.BorderSize = 0;
            btnConsultar.Margin = new Padding(0, 1, 20, 0);
            btnConsultar.Click += (s, e) => CargarDatosHistorial();

            // 6. Label Buscar Código / Cédula
            var lblBuscar = new Label
            {
                Text = "Filtrar Código:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            // 7. TextBox Buscar Código / Cédula
            txtBuscarCodigo.Font = new Font("Segoe UI", 10);
            txtBuscarCodigo.Size = new Size(130, 28);
            txtBuscarCodigo.Margin = new Padding(0, 2, 0, 0);
            txtBuscarCodigo.TextChanged += (s, e) => AplicarFiltroCodigo();

            panelTop.Controls.AddRange(new Control[] {
                lblMes, cbMes, lblAnio, nudAnio, btnConsultar, lblBuscar, txtBuscarCodigo
            });

            // 8. Tabla de Resultados
            dgvHistorial.Location = new Point(20, 60);
            dgvHistorial.Size = new Size(894, 395);
            dgvHistorial.ReadOnly = true;
            dgvHistorial.AllowUserToAddRows = false;
            dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistorial.RowHeadersVisible = false;
            dgvHistorial.BackgroundColor = Color.White;
            dgvHistorial.BorderStyle = BorderStyle.FixedSingle;


            lblRecibos.Text = "Recibos del alumno seleccionado";
            lblRecibos.Location = new Point(20, 455);
            lblRecibos.Size = new Size(400, 20);
            lblRecibos.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            Controls.Add(lblRecibos);
            dgvHistorial.EnableHeadersVisualStyles = false;
            dgvHistorial.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#0b5394");
            dgvHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistorial.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvHistorial.ColumnHeadersHeight = 32;

            dgvRecibos.Location = new Point(20, 475);
            dgvRecibos.Size = new Size(894, 220);
            dgvRecibos.ReadOnly = true;
            dgvRecibos.AllowUserToAddRows = false;
            dgvRecibos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvRecibos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvRecibos.SelectionChanged += DgvRecibos_SelectionChanged;
            dgvRecibos.RowHeadersVisible = false;
            dgvRecibos.BackgroundColor = Color.White;
            dgvRecibos.BorderStyle = BorderStyle.FixedSingle;

            dgvRecibos.EnableHeadersVisualStyles = false;
            dgvRecibos.ColumnHeadersDefaultCellStyle.BackColor =
                ColorTranslator.FromHtml("#0b5394");
            dgvRecibos.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;
            dgvRecibos.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvRecibos.ColumnHeadersHeight = 32;

            Controls.Add(dgvRecibos);

            Controls.AddRange(new Control[] { panelTop, dgvHistorial });

            Shown += (s, e) => CargarDatosHistorial();
            dgvHistorial.CellClick += DgvHistorial_CellClick;

            btnVerRecibo.Text = "🧾 Ver recibo seleccionado";
            btnVerRecibo.Location = new Point(20, 690);
            btnVerRecibo.Size = new Size(210, 35);
            btnVerRecibo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnVerRecibo.Enabled = false;

            btnVerRecibo.Click += BtnVerRecibo_Click;

            Controls.Add(btnVerRecibo);

            btnWhatsAppRecibo.Text = "📱 Reenviar por WhatsApp";
            btnWhatsAppRecibo.Location = new Point(245, 690);
            btnWhatsAppRecibo.Size = new Size(210, 35);
            btnWhatsAppRecibo.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnWhatsAppRecibo.Enabled = false;

            btnWhatsAppRecibo.Click += BtnWhatsAppRecibo_Click;

            Controls.Add(btnWhatsAppRecibo);

        }

        private void DgvHistorial_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;
            if (dgvHistorial.SelectedRows.Count == 0)
                return;

            var fila = dgvHistorial.SelectedRows[0];

            string? codigo = fila.Cells["Código"].Value?.ToString();

            if (string.IsNullOrWhiteSpace(codigo))
                return;

            var alumno = listaCompletaMes.FirstOrDefault(
                x => x.Codigo == codigo);

            if (alumno == null)
                return;

            int alumnoId = alumno.AlumnoId;
            int mes = cbMes.SelectedIndex + 1;
            int anio = (int)nudAnio.Value;

            pagosAlumnoSeleccionado = obtenerPagosAlumnoPeriodo.Ejecutar(alumnoId,anio,mes);
            MostrarRecibosAlumno();
        }



        private void DgvRecibos_SelectionChanged(object? sender, EventArgs e)
        {
            bool hayReciboSeleccionado =
                dgvRecibos.SelectedRows.Count > 0 &&
                pagosAlumnoSeleccionado.Count > 0;

            btnVerRecibo.Enabled = hayReciboSeleccionado;
            btnWhatsAppRecibo.Enabled = hayReciboSeleccionado;
        }

        private void BtnVerRecibo_Click(
    object? sender,
    EventArgs e)
        {
            if (dgvRecibos.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Selecciona un recibo primero.",
                    "Recibo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var pagosOrdenados = pagosAlumnoSeleccionado
                .OrderByDescending(p => p.FechaPago)
                .ToList();

            int indice = dgvRecibos.SelectedRows[0].Index;

            if (indice < 0 || indice >= pagosOrdenados.Count)
                return;

            Pago pagoSeleccionado = pagosOrdenados[indice];

            if (string.IsNullOrWhiteSpace(pagoSeleccionado.Folio))
            {
                MessageBox.Show(
                    "El recibo seleccionado no tiene folio.",
                    "Recibo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var pagoConsultado = obtenerPagoPorFolio.Ejecutar(
                pagoSeleccionado.Folio);

            if (pagoConsultado == null)
            {
                MessageBox.Show(
                    "No se encontró el recibo con el folio seleccionado.",
                    "Recibo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            Pago pago = pagoConsultado;

            // Obtener información actual del alumno
            var alumnoInfo = AppServices.CrearObtenerAlumno().Ejecutar(pago.AlumnoId);

            if (alumnoInfo == null)
            {
                MessageBox.Show(
                    "No se pudo encontrar la información del alumno.",
                    "Recibo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Obtener la cuota correspondiente a ese mes
            var estado = AppServices.CrearObtenerEstadoAlumno().Ejecutar(
                pago.AlumnoId,
                pago.Anio,
                pago.Mes);

            if (estado == null)
            {
                MessageBox.Show(
                    "No se pudo obtener el estado de cuenta del alumno.",
                    "Recibo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // Ordenamos cronológicamente los pagos
            // para calcular cuánto llevaba abonado hasta este recibo.
            var pagosCronologicos = pagosAlumnoSeleccionado
                .OrderBy(p => p.FechaPago)
                .ThenBy(p => p.Id)
                .ToList();

            decimal nuevoAbonado = 0;

            foreach (var pagoAnterior in pagosCronologicos)
            {
                nuevoAbonado += pagoAnterior.MontoUsd;

                if (pagoAnterior.Id == pago.Id)
                    break;
            }

            decimal nuevoRestante =
                Math.Max(estado.CuotaTotal - nuevoAbonado, 0);

            ImpresionRecibo.MostrarRecibo(
     alumnoInfo.Nombre,
     alumnoInfo.Codigo,
     alumnoInfo.Cedula,
     pago.Anio,
     pago.Mes,
     pago.MontoUsd,
     pago.TasaBcv,
     pago.MontoBs,
     pago.MetodoPago,
     pago.Referencia,
     estado.CuotaTotal,
     nuevoAbonado,
     nuevoRestante,
     pago.Folio,
     pago.FechaPago);
        }

        private void BtnWhatsAppRecibo_Click(
    object? sender,
    EventArgs e)
        {
            if (dgvRecibos.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Selecciona un recibo primero.",
                    "WhatsApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var pagosOrdenados = pagosAlumnoSeleccionado
                .OrderByDescending(p => p.FechaPago)
                .ToList();

            int indice = dgvRecibos.SelectedRows[0].Index;

            if (indice < 0 || indice >= pagosOrdenados.Count)
                return;

            Pago pagoSeleccionado = pagosOrdenados[indice];

            if (string.IsNullOrWhiteSpace(pagoSeleccionado.Folio))
            {
                MessageBox.Show(
                    "El recibo seleccionado no tiene folio.",
                    "WhatsApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var pago = obtenerPagoPorFolio.Ejecutar(
                pagoSeleccionado.Folio);

            if (pago == null)
            {
                MessageBox.Show(
                    "No se encontró el recibo con el folio seleccionado.",
                    "WhatsApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var alumnoInfo = AppServices.CrearObtenerAlumno().Ejecutar(pago.AlumnoId);

            if (alumnoInfo == null)
            {
                MessageBox.Show(
                    "No se pudo encontrar la información del alumno.",
                    "WhatsApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var estado = AppServices.CrearObtenerEstadoAlumno().Ejecutar(
                pago.AlumnoId,
                pago.Anio,
                pago.Mes);

            if (estado == null)
            {
                MessageBox.Show(
                    "No se pudo obtener el estado de cuenta del alumno.",
                    "WhatsApp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var pagosCronologicos = pagosAlumnoSeleccionado
                .OrderBy(p => p.FechaPago)
                .ThenBy(p => p.Id)
                .ToList();

            decimal totalAbonado = 0;

            foreach (var pagoAnterior in pagosCronologicos)
            {
                totalAbonado += pagoAnterior.MontoUsd;

                if (pagoAnterior.Id == pago.Id)
                    break;
            }

            decimal restante = Math.Max(
                estado.CuotaTotal - totalAbonado,
                0);

            WhatsAppHelper.EnviarRecibo(
                alumnoInfo,
                pago,
                estado.CuotaTotal,
                totalAbonado,
                restante);
        }

        private void MostrarRecibosAlumno()
        {
            var datos = pagosAlumnoSeleccionado
                .OrderByDescending(p => p.FechaPago)
                .Select(p => new
                {
                    Folio = p.Folio,
                    Fecha = p.FechaPago.ToString("dd/MM/yyyy HH:mm"),
                    Monto = $"${p.MontoUsd:0.00}",
                    Método = p.MetodoPago,
                    Referencia = p.Referencia
                })
                .ToList();

            dgvRecibos.DataSource = datos;

            if (dgvRecibos.Columns.Count > 0)
            {
                dgvRecibos.Columns["Folio"].HeaderText = "Folio";
                dgvRecibos.Columns["Fecha"].HeaderText = "Fecha";
                dgvRecibos.Columns["Monto"].HeaderText = "Monto";
                dgvRecibos.Columns["Método"].HeaderText = "Método";
                dgvRecibos.Columns["Referencia"].HeaderText = "Referencia";
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

        private void CargarDatosHistorial()
        {
            int mesSeleccionado = cbMes.SelectedIndex + 1;
            int anioSeleccionado = (int)nudAnio.Value;

            // Cargar datos del mes seleccionado en memoria
            listaCompletaMes = AppServices.CrearObtenerDashboard().Ejecutar(anioSeleccionado, mesSeleccionado, actividadId);

            // Aplicar el filtro de la caja de búsqueda
            AplicarFiltroCodigo();
        }

        private void AplicarFiltroCodigo()
        {
            string textoBusqueda = txtBuscarCodigo.Text.Trim().ToLower();

            // Filtrar la lista comparando con la cédula/código del alumno
            var listaFiltrada = listaCompletaMes.Where(e =>
                string.IsNullOrEmpty(textoBusqueda) ||
                (!string.IsNullOrEmpty(e.Codigo) && e.Codigo.ToLower().Contains(textoBusqueda))
            ).ToList();

            var datosFormateados = listaFiltrada.Select(e => new
            {
                Código = e.Codigo,
                Alumno = e.Nombre,
                Plan = e.PlanNombre,
                Asistencias = e.ControlaClases ? $"{e.Tomadas} / {e.ClasesMes}" : $"{e.Tomadas} / —",
                Cuota = $"${e.CuotaTotal}",
                Abonado = $"${e.MontoAbonado}",
                Estado = e.EstaPagado ? "💳 Al día" : $"⚠️ Debe ${e.MontoRestante}"
            }).ToList();

            dgvHistorial.DataSource = datosFormateados;

            if (dgvHistorial.Columns.Count > 0)
            {
                dgvHistorial.Columns["Código"].Width = 85;
                dgvHistorial.Columns["Alumno"].Width = 200;
                dgvHistorial.Columns["Plan"].Width = 80;
                dgvHistorial.Columns["Asistencias"].Width = 95;
                dgvHistorial.Columns["Cuota"].Width = 70;
                dgvHistorial.Columns["Abonado"].Width = 80;
                dgvHistorial.Columns["Estado"].Width = 130;
            }
        }
    }
}
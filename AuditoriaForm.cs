using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class AuditoriaForm : Form
    {
        private readonly DateTimePicker dtpDesde = new();
        private readonly DateTimePicker dtpHasta = new();
        private readonly TextBox txtBuscar = new();
        private readonly Button btnFiltrar = new();
        private readonly DataGridView dgvAuditoria = new();

        private readonly int actividadId;
        private List<EventoAuditoria> listaEventos = new();

        public AuditoriaForm(int actividadId)
        {
            this.actividadId = actividadId;

            Text = "📜 Bitácora de Auditoría (Registro de Eventos)";
            Size = new Size(980, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = ColorTranslator.FromHtml("#f8f9fa");

            var panelTop = new FlowLayoutPanel
            {
                Location = new Point(20, 15),
                Size = new Size(924, 40),
                AutoSize = false,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };

            // 1. Fecha Desde
            var lblDesde = new Label
            {
                Text = "Desde:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            dtpDesde.Font = new Font("Segoe UI", 9.5f);
            dtpDesde.Format = DateTimePickerFormat.Short;
            dtpDesde.Width = 110;
            dtpDesde.Value = DateTime.Today.AddDays(-30);
            dtpDesde.Margin = new Padding(0, 2, 15, 0);

            // 2. Fecha Hasta
            var lblHasta = new Label
            {
                Text = "Hasta:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            dtpHasta.Font = new Font("Segoe UI", 9.5f);
            dtpHasta.Format = DateTimePickerFormat.Short;
            dtpHasta.Width = 110;
            dtpHasta.Value = DateTime.Today;
            dtpHasta.Margin = new Padding(0, 2, 15, 0);

            // 3. Buscar Texto
            var lblBuscar = new Label
            {
                Text = "🔍 Buscar:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 0)
            };

            txtBuscar.Font = new Font("Segoe UI", 9.5f);
            txtBuscar.Width = 180;
            txtBuscar.PlaceholderText = "Usuario, acción o detalle...";
            txtBuscar.Margin = new Padding(0, 2, 15, 0);
            txtBuscar.TextChanged += (s, e) => CargarAuditoria();

            // 4. Botón Filtrar
            btnFiltrar.Text = "Filtrar";
            btnFiltrar.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnFiltrar.Size = new Size(90, 28);
            btnFiltrar.BackColor = ColorTranslator.FromHtml("#0b5394");
            btnFiltrar.ForeColor = Color.White;
            btnFiltrar.FlatStyle = FlatStyle.Flat;
            btnFiltrar.FlatAppearance.BorderSize = 0;
            btnFiltrar.Click += (s, e) => CargarAuditoria();

            panelTop.Controls.AddRange(new Control[]
            {
                lblDesde,
                dtpDesde,
                lblHasta,
                dtpHasta,
                lblBuscar,
                txtBuscar,
                btnFiltrar
            });

            // Tabla DataGridView
            dgvAuditoria.Location = new Point(20, 60);
            dgvAuditoria.Size = new Size(924, 435);
            dgvAuditoria.ReadOnly = true;
            dgvAuditoria.AllowUserToAddRows = false;
            dgvAuditoria.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvAuditoria.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAuditoria.RowHeadersVisible = false;
            dgvAuditoria.BackgroundColor = Color.White;
            dgvAuditoria.BorderStyle = BorderStyle.FixedSingle;

            dgvAuditoria.EnableHeadersVisualStyles = false;
            dgvAuditoria.ColumnHeadersDefaultCellStyle.BackColor =
                ColorTranslator.FromHtml("#1e293b");
            dgvAuditoria.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvAuditoria.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvAuditoria.ColumnHeadersHeight = 32;

            Controls.AddRange(new Control[]
            {
                panelTop,
                dgvAuditoria
            });

            Shown += (s, e) => CargarAuditoria();
        }

        private void CargarAuditoria()
        {
            listaEventos = AppServices
                .CrearObtenerAuditoria()
                .Ejecutar(
                    dtpDesde.Value,
                    dtpHasta.Value,
                    txtBuscar.Text,
                    actividadId
                );

            var datosFormateados = listaEventos
                .Select(e => new
                {
                    FechaHora = e.Fecha,
                    Operador = e.Usuario,
                    Acción = e.Accion,
                    Detalle = e.Detalle
                })
                .ToList();

            dgvAuditoria.DataSource = datosFormateados;

            if (dgvAuditoria.Columns.Count > 0)
            {
                dgvAuditoria.Columns["FechaHora"].HeaderText = "Fecha y Hora";
                dgvAuditoria.Columns["FechaHora"].Width = 140;

                dgvAuditoria.Columns["Operador"].HeaderText = "Operador / Usuario";
                dgvAuditoria.Columns["Operador"].Width = 140;

                dgvAuditoria.Columns["Acción"].HeaderText = "Acción";
                dgvAuditoria.Columns["Acción"].Width = 140;

                dgvAuditoria.Columns["Detalle"].HeaderText = "Detalle del Evento";
                dgvAuditoria.Columns["Detalle"].AutoSizeMode =
                    DataGridViewAutoSizeColumnMode.Fill;
            }
        }
    }
}
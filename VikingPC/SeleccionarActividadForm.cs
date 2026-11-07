using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Domain;
using AsistenciaNatacion.Presentation.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    /// <summary>
    /// Pantalla de entrada al sistema. Presenta las actividades como tarjetas
    /// para que el operador identifique rápidamente el entorno de trabajo.
    /// Solo coordina UI; la obtención de actividades continúa en Application.
    /// </summary>
    public class SeleccionarActividadForm : Form
    {
        private readonly FlowLayoutPanel panelActividades = new();
        private readonly Label lblEstado = new();
        private readonly Button btnSalir = new();
        private readonly List<Actividad> actividades = new();

        public int ActividadIdSeleccionada { get; private set; }

        public SeleccionarActividadForm()
        {
            Text = "Control de Gestión · Seleccionar actividad";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = AppTheme.Background;
            KeyPreview = true;
            KeyDown += SeleccionarActividadForm_KeyDown;

            ConstruirInterfaz();
            CargarActividades();
        }

        private void ConstruirInterfaz()
        {
            var raiz = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = AppTheme.Background,
                Padding = new Padding(0)
            };

            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            raiz.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

            // Barra superior de marca
            var barra = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Primary
            };

            var lblMarca = new Label
            {
                Text = "◆  CONTROL DE GESTIÓN",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(34, 24)
            };

            var lblVersion = new Label
            {
                Text = $"GESTIÓN DEPORTIVA  ·  {System.Windows.Forms.Application.ProductVersion.Split('+')[0]}",
                ForeColor = Color.FromArgb(210, 232, 250),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            barra.Controls.Add(lblMarca);
            barra.Controls.Add(lblVersion);
            barra.Resize += (s, e) =>
            {
                lblVersion.Left = barra.ClientSize.Width - lblVersion.Width - 34;
                lblVersion.Top = 29;
            };

            // Encabezado de bienvenida
            var bienvenida = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Background
            };

            var lblTitulo = new Label
            {
                Text = "¿En qué espacio quieres trabajar?",
                Font = new Font("Segoe UI", 27, FontStyle.Bold),
                ForeColor = AppTheme.Text,
                AutoSize = true
            };

            var lblSubtitulo = new Label
            {
                Text = "Selecciona una actividad para abrir su panel de gestión.",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = AppTheme.Muted,
                AutoSize = true
            };

            bienvenida.Controls.Add(lblTitulo);
            bienvenida.Controls.Add(lblSubtitulo);
            bienvenida.Resize += (s, e) =>
            {
                lblTitulo.Left = Math.Max(30, (bienvenida.ClientSize.Width - lblTitulo.Width) / 2);
                lblTitulo.Top = 28;
                lblSubtitulo.Left = Math.Max(30, (bienvenida.ClientSize.Width - lblSubtitulo.Width) / 2);
                lblSubtitulo.Top = lblTitulo.Bottom + 10;
            };

            // Tarjetas de actividades
            panelActividades.Dock = DockStyle.Fill;
            panelActividades.FlowDirection = FlowDirection.LeftToRight;
            panelActividades.WrapContents = true;
            panelActividades.AutoScroll = true;
            panelActividades.Padding = new Padding(0, 12, 0, 20);
            panelActividades.BackColor = AppTheme.Background;
            panelActividades.Resize += (s, e) => CentrarActividades();

            // Pie de pantalla
            var pie = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            lblEstado.Text = "Selecciona una actividad para continuar";
            lblEstado.Font = new Font("Segoe UI", 9.5f);
            lblEstado.ForeColor = AppTheme.Muted;
            lblEstado.AutoSize = true;
            lblEstado.Location = new Point(34, 22);

            btnSalir.Text = "Salir";
            btnSalir.Size = new Size(110, 34);
            btnSalir.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSalir.FlatStyle = FlatStyle.Flat;
            btnSalir.FlatAppearance.BorderColor = AppTheme.Border;
            btnSalir.BackColor = Color.White;
            btnSalir.ForeColor = AppTheme.Text;
            btnSalir.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnSalir.Cursor = Cursors.Hand;
            btnSalir.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            pie.Controls.Add(lblEstado);
            pie.Controls.Add(btnSalir);
            pie.Resize += (s, e) =>
            {
                btnSalir.Left = pie.ClientSize.Width - btnSalir.Width - 34;
                btnSalir.Top = 14;
            };

            raiz.Controls.Add(barra, 0, 0);
            raiz.Controls.Add(bienvenida, 0, 1);
            raiz.Controls.Add(panelActividades, 0, 2);
            raiz.Controls.Add(pie, 0, 3);

            Controls.Add(raiz);
        }

        private void CargarActividades()
        {
            try
            {
                var resultado = AppServices.CrearObtenerActividades().Ejecutar();
                actividades.Clear();
                actividades.AddRange(resultado ?? new List<Actividad>());

                panelActividades.Controls.Clear();

                if (actividades.Count == 0)
                {
                    lblEstado.Text = "No hay actividades activas configuradas.";
                    panelActividades.Controls.Add(CrearMensajeVacio());
                    return;
                }

                foreach (var actividad in actividades)
                    panelActividades.Controls.Add(CrearTarjetaActividad(actividad));

                CentrarActividades();

                lblEstado.Text = actividades.Count == 1
                    ? "1 actividad disponible"
                    : $"{actividades.Count} actividades disponibles";
            }
            catch (Exception ex)
            {
                lblEstado.Text = "No fue posible cargar las actividades.";
                panelActividades.Controls.Add(CrearMensajeVacio(
                    "No se pudieron cargar las actividades.\n\n" + ex.Message));
            }
        }

        private void CentrarActividades()
        {
            if (panelActividades.Controls.Count == 0)
                return;

            int anchoTarjetas = panelActividades.Controls
                .Cast<Control>()
                .Sum(c => c.Width + c.Margin.Horizontal);

            int espacioDisponible = panelActividades.ClientSize.Width;

            int margenIzquierdo = Math.Max(
                0,
                (espacioDisponible - anchoTarjetas) / 2);

            panelActividades.Padding = new Padding(
                margenIzquierdo,
                12,
                margenIzquierdo,
                20);
        }

        private Control CrearTarjetaActividad(Actividad actividad)
        {
            var tarjeta = new TarjetaActividadPanel
            {
                Width = 310,
                Height = 190,
                Margin = new Padding(14),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Tag = actividad,
                BorderColor = AppTheme.Border,
                BorderWidth = 1
            };

            var circulo = new Label
            {
                Text = ObtenerIcono(actividad.Nombre),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 20),
                ForeColor = Color.White,
                BackColor = AppTheme.Primary,
                Size = new Size(54, 54),
                Location = new Point(22, 20)
            };

            var lblNombre = new Label
            {
                Text = actividad.Nombre,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = AppTheme.Text,
                AutoEllipsis = true,
                Location = new Point(92, 23),
                Size = new Size(190, 32)
            };

            var lblTipo = new Label
            {
                Text = "Actividad activa",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.Muted,
                Location = new Point(92, 57),
                AutoSize = true
            };

            var linea = new Panel
            {
                BackColor = AppTheme.Border,
                Location = new Point(22, 88),
                Size = new Size(266, 1)
            };

            var lblDescripcion = new Label
            {
                Text = "Clientes · asistencia · pagos · planes",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = AppTheme.Muted,
                Location = new Point(22, 104),
                AutoSize = true
            };

            var btnEntrar = new Button
            {
                Text = "Entrar  →",
                Size = new Size(110, 34),
                Location = new Point(178, 140),
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Primary,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEntrar.FlatAppearance.BorderSize = 0;
            btnEntrar.Click += (s, e) => SeleccionarActividad(actividad);

            tarjeta.Controls.Add(circulo);
            tarjeta.Controls.Add(lblNombre);
            tarjeta.Controls.Add(lblTipo);
            tarjeta.Controls.Add(linea);
            tarjeta.Controls.Add(lblDescripcion);
            tarjeta.Controls.Add(btnEntrar);

            // Selección mediante clic en cualquier parte de la tarjeta.
            tarjeta.Click += (s, e) => SeleccionarActividad(actividad);
            circulo.Click += (s, e) => SeleccionarActividad(actividad);
            lblNombre.Click += (s, e) => SeleccionarActividad(actividad);
            lblTipo.Click += (s, e) => SeleccionarActividad(actividad);
            lblDescripcion.Click += (s, e) => SeleccionarActividad(actividad);
            linea.Click += (s, e) => SeleccionarActividad(actividad);

            // Estado visual al pasar el mouse por la tarjeta o cualquiera de sus controles.
            foreach (Control control in tarjeta.Controls)
            {
                control.MouseEnter += (s, e) => AplicarHoverTarjeta(tarjeta, true);
                control.MouseLeave += (s, e) => AplicarHoverTarjeta(tarjeta, false);
            }

            tarjeta.MouseEnter += (s, e) => AplicarHoverTarjeta(tarjeta, true);
            tarjeta.MouseLeave += (s, e) => AplicarHoverTarjeta(tarjeta, false);

            return tarjeta;
        }

        private static void AplicarHoverTarjeta(TarjetaActividadPanel tarjeta, bool activo)
        {
            tarjeta.BorderColor = activo
                ? AppTheme.Primary
                : AppTheme.Border;

            tarjeta.BorderWidth = activo ? 2 : 1;
            tarjeta.BackColor = activo
                ? Color.FromArgb(247, 251, 255)
                : Color.White;

            tarjeta.Invalidate();

            foreach (Control control in tarjeta.Controls)
            {
                if (control == tarjeta.Controls[1] && control is Label labelNombre)
                {
                    labelNombre.ForeColor = activo
                        ? AppTheme.Primary
                        : AppTheme.Text;
                }

                if (control is Button boton)
                {
                    boton.BackColor = activo
                        ? ColorTranslator.FromHtml("#0b5394")
                        : AppTheme.Primary;
                }
            }
        }

        private static Control CrearMensajeVacio(string? texto = null)
        {
            return new Label
            {
                Text = texto ?? "No hay actividades disponibles.",
                Font = new Font("Segoe UI", 12, FontStyle.Regular),
                ForeColor = AppTheme.Muted,
                AutoSize = true,
                Padding = new Padding(20),
                Margin = new Padding(40, 20, 40, 20)
            };
        }

        private void SeleccionarActividad(Actividad actividad)
        {
            if (actividad == null || actividad.Id <= 0)
                return;

            ActividadIdSeleccionada = actividad.Id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string ObtenerIcono(string nombre)
        {
            var texto = (nombre ?? "").ToLowerInvariant();

            if (texto.Contains("nat") || texto.Contains("pisc"))
                return "🏊";
            if (texto.Contains("cross"))
                return "🏋";
            if (texto.Contains("func"))
                return "⚡";
            if (texto.Contains("gim"))
                return "💪";
            if (texto.Contains("yoga"))
                return "🧘";

            return "◆";
        }

        private void SeleccionarActividadForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private sealed class TarjetaActividadPanel : Panel
        {
            public Color BorderColor { get; set; } = Color.LightGray;
            public int BorderWidth { get; set; } = 1;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using var pen = new Pen(BorderColor, BorderWidth);

                var rect = new Rectangle(
                    BorderWidth / 2,
                    BorderWidth / 2,
                    Math.Max(1, Width - BorderWidth),
                    Math.Max(1, Height - BorderWidth));

                e.Graphics.DrawRectangle(pen, rect);
            }
        }
    }
}

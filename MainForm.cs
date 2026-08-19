using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Presentation.Forms;
using AsistenciaNatacion.Presentation.Theme;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public enum FiltroTarjeta
    {
        Todos,
        AsistieronHoy,
        PorVencer,
        ConAbonos,
        Pendientes
    }

    public class MainForm : Form
    {
        private int actividadId;
        private string nombreEstablecimiento = "Control de Gestión";
        private string tipoEstablecimiento = "Gestión deportiva";
        private readonly ListView listaAlumnos = new();
        private readonly Label lblMes = new();
        private readonly TextBox txtBuscar = new();
        private readonly Label lblFiltroActivo = new();
        private readonly Button btnLimpiarFiltro = new();

        private List<EstadoAlumnoMes> estadosActuales = new();
        private int anioActual;
        private int mesActual;
        private FiltroTarjeta filtroActual = FiltroTarjeta.Todos;

        // Paneles de Tarjetas para gestionar el resaltado visual
        private Panel cardActivos = new();
        private Panel cardAsistencias = new();
        private Panel cardPorVencer = new();
        private Panel cardRecaudado = new();

        // Labels de valores
        private Label lblValActivos = new();
        private Label lblValAsistencias = new();
        private Label lblValPorVencer = new();
        private Label lblValRecaudado = new();
        private Label lblValPendiente = new();
        private Label lblValCajaHoy = new();

        private Label lblResumenCaja = new();
        private Label lblResumenPendiente = new();
        private Label lblResumenAsistencia = new();
        private Label lblResumenAtencion = new();
        private Label lblResumenActualizado = new();

        private Label lblNombreEstablecimiento = new();
        private Label lblTipoEstablecimiento = new();

        private static readonly string[] MesesEs =
        {
            "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio",
            "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
        };

        public MainForm(int actividadId)
        {
            this.actividadId = actividadId;
            ContextoActividad.ActividadId = actividadId;
            string rutaLogo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "viking_logo.ico");

            if (File.Exists(rutaLogo))
            {
                Icon = new Icon(rutaLogo);
            }
            CargarIdentidadEstablecimiento();
            CargarOperadorDeActividad();
            Text = "Viking · Gestión";

            // Permite que MainForm reciba F2, F3, F4, F5, Ctrl+F y Esc
            // aunque el foco esté dentro de otro control.
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ColorTranslator.FromHtml("#f2f6fa");

            var hoy = DateTime.Today;
            anioActual = hoy.Year;
            mesActual = hoy.Month;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 102F));  // KPI del negocio
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));   // Header principal
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));   // Resumen operativo
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));   // Buscador + filtros
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Tabla de clientes
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));   // Acciones inferiores

            var tarjetas = CrearPanelTarjetas();
            var header = ConstruirHeader();
            var resumen = ConstruirResumenOperativo();
            var busqueda = ConstruirBusqueda();
            var acciones = ConstruirAcciones();
            ConstruirTabla();

            layout.Controls.Add(tarjetas, 0, 0);
            layout.Controls.Add(header, 0, 1);
            layout.Controls.Add(resumen, 0, 2);
            layout.Controls.Add(busqueda, 0, 3);
            layout.Controls.Add(listaAlumnos, 0, 4);
            layout.Controls.Add(acciones, 0, 5);

            Controls.Clear();
            Controls.Add(layout);
            AppTheme.Aplicar(this);

            Refrescar();
        }

        // ---------- TARJETAS SUPERIORES E INTERACTIVIDAD ----------

        private Panel CrearPanelTarjetas()
        {
            var contenedor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(6, 5, 6, 5),
                BackColor = ColorTranslator.FromHtml("#eef3f8")
            };

            for (int i = 0; i < 6; i++)
                contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6667F));

            lblValActivos = CrearValorKpi();
            cardActivos = CrearTarjeta("👥 CLIENTES ACTIVOS", lblValActivos, "Base activa", Color.FromArgb(224, 242, 254), Color.FromArgb(3, 105, 161), () => SeleccionarFiltro(FiltroTarjeta.Todos));

            lblValAsistencias = CrearValorKpi();
            cardAsistencias = CrearTarjeta("🏋 ASISTENCIA HOY", lblValAsistencias, "Presentes hoy", Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61), () => SeleccionarFiltro(FiltroTarjeta.AsistieronHoy));

            lblValPorVencer = CrearValorKpi();
            cardPorVencer = CrearTarjeta("⚠️ ATENCIÓN", lblValPorVencer, "Por vencer / vencidos", Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9), () => SeleccionarFiltro(FiltroTarjeta.PorVencer));

            lblValRecaudado = CrearValorKpi();
            cardRecaudado = CrearTarjeta("💵 COBRADO MES", lblValRecaudado, "Ingresos acumulados", Color.FromArgb(237, 233, 254), Color.FromArgb(109, 40, 217), () => SeleccionarFiltro(FiltroTarjeta.ConAbonos));

            lblValCajaHoy = CrearValorKpi();
            var cardCaja = CrearTarjeta("💰 CAJA HOY", lblValCajaHoy, "Cobros registrados hoy", Color.FromArgb(240, 253, 250), Color.FromArgb(13, 148, 136), () => SeleccionarFiltro(FiltroTarjeta.ConAbonos));

            lblValPendiente = CrearValorKpi();
            var cardPendiente = CrearTarjeta("📌 POR COBRAR", lblValPendiente, "Saldo pendiente del mes", Color.FromArgb(254, 242, 242), Color.FromArgb(185, 28, 28), () => SeleccionarFiltro(FiltroTarjeta.Pendientes));

            contenedor.Controls.Add(cardActivos, 0, 0);
            contenedor.Controls.Add(cardAsistencias, 1, 0);
            contenedor.Controls.Add(cardPorVencer, 2, 0);
            contenedor.Controls.Add(cardRecaudado, 3, 0);
            contenedor.Controls.Add(cardCaja, 4, 0);
            contenedor.Controls.Add(cardPendiente, 5, 0);

            return contenedor;
        }

        private static Label CrearValorKpi()
        {
            return new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true
            };
        }

        private Panel CrearTarjeta(string titulo, Label lblValor, string subtitulo, Color colorFondo, Color colorBorde, Action alHacerClic)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = colorFondo,
                Tag = colorFondo // Guardamos el color base para restaurar
            };

            var lblTitulo = new Label
            {
                Text = titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = colorBorde,
                Location = new Point(10, 6),
                AutoSize = true
            };

            lblValor.Location = new Point(8, 22);

            var lblSub = new Label
            {
                Text = subtitulo,
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 60),
                AutoSize = true
            };

            card.Controls.Add(lblTitulo);
            card.Controls.Add(lblValor);
            card.Controls.Add(lblSub);

            // Asigna el evento Clic y Cursor de Mano a la tarjeta y a TODOS sus elementos hijos
            AsignarClicRecursivo(card, (s, e) => alHacerClic());

            return card;
        }

        private void AsignarClicRecursivo(Control control, EventHandler handler)
        {
            control.Click += handler;
            control.Cursor = Cursors.Hand;
            foreach (Control subControl in control.Controls)
            {
                AsignarClicRecursivo(subControl, handler);
            }
        }

        private void SeleccionarFiltro(FiltroTarjeta filtro)
        {
            // Si hace clic en la misma tarjeta activa, vuelve a "Todos"
            if (filtroActual == filtro && filtro != FiltroTarjeta.Todos)
            {
                filtroActual = FiltroTarjeta.Todos;
            }
            else
            {
                filtroActual = filtro;
            }

            ActualizarEstiloTarjetas();
            AplicarFiltroBusqueda();
        }

        private void ActualizarEstiloTarjetas()
        {
            // Restaura borde normal
            RestaurarTarjeta(cardActivos, filtroActual == FiltroTarjeta.Todos);
            RestaurarTarjeta(cardAsistencias, filtroActual == FiltroTarjeta.AsistieronHoy);
            RestaurarTarjeta(cardPorVencer, filtroActual == FiltroTarjeta.PorVencer);
            RestaurarTarjeta(cardRecaudado, filtroActual == FiltroTarjeta.ConAbonos);

            // Actualiza la etiqueta informativa al lado del buscador
            switch (filtroActual)
            {
                case FiltroTarjeta.AsistieronHoy:
                    lblFiltroActivo.Text = "Filtrando: 🏊 Asistieron Hoy";
                    lblFiltroActivo.Visible = true;
                    btnLimpiarFiltro.Visible = true;
                    break;
                case FiltroTarjeta.PorVencer:
                    lblFiltroActivo.Text = "Filtrando: ⚠️ Planes por Vencer / Agotados";
                    lblFiltroActivo.Visible = true;
                    btnLimpiarFiltro.Visible = true;
                    break;
                case FiltroTarjeta.ConAbonos:
                    lblFiltroActivo.Text = "Filtrando: 💵 Alumnos con Pagos Registrados";
                    lblFiltroActivo.Visible = true;
                    btnLimpiarFiltro.Visible = true;
                    break;
                case FiltroTarjeta.Pendientes:
                    lblFiltroActivo.Text = "Filtrando: 📌 Clientes con saldo pendiente";
                    lblFiltroActivo.Visible = true;
                    btnLimpiarFiltro.Visible = true;
                    break;
                default:
                    lblFiltroActivo.Visible = false;
                    btnLimpiarFiltro.Visible = false;
                    break;
            }
        }

        private void RestaurarTarjeta(Panel card, bool seleccionada)
        {
            if (seleccionada)
            {
                card.BorderStyle = BorderStyle.Fixed3D;
            }
            else
            {
                card.BorderStyle = BorderStyle.None;
            }
        }

        private void ActualizarTarjetasMetricas()
        {
            var casoUso = AppServices.CrearObtenerMetricasRapidas();

            var m = casoUso.Ejecutar(
                anioActual,
                mesActual,
                actividadId);

            lblValActivos.Text = m.AlumnosActivos.ToString();
            lblValAsistencias.Text = m.AsistenciasHoy.ToString();
            lblValPorVencer.Text = m.PlanesPorVencer.ToString();
            lblValRecaudado.Text = $"${m.RecaudadoMes:0.##}";
            lblValCajaHoy.Text = $"${m.RecaudadoHoy:0.##}";

            decimal pendiente = estadosActuales.Sum(e => e.MontoRestante);
            lblValPendiente.Text = $"${pendiente:0.##}";

            int activos = Math.Max(m.AlumnosActivos, 1);
            decimal porcentajeAsistencia = (decimal)m.AsistenciasHoy / activos * 100m;
            int atencion = estadosActuales.Count(e => e.PorVencer || !e.EstaPagado);

            lblResumenCaja.Text = $"Caja hoy: ${m.RecaudadoHoy:0.##}";
            lblResumenPendiente.Text = $"Pendiente: ${pendiente:0.##}";
            lblResumenAsistencia.Text = $"Asistencia: {porcentajeAsistencia:0.#}%";
            lblResumenAtencion.Text = $"Atención requerida: {atencion}";
            lblResumenActualizado.Text = $"Actualizado: {DateTime.Now:HH:mm}";
        }

        private Panel ConstruirResumenOperativo()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(14, 7, 14, 7)
            };

            var titulo = new Label
            {
                Text = "Resumen operativo",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(14, 8)
            };
            panel.Controls.Add(titulo);

            lblResumenCaja = CrearResumenLabel("Caja hoy: $0", new Point(145, 8));
            lblResumenPendiente = CrearResumenLabel("Pendiente: $0", new Point(300, 8));
            lblResumenAsistencia = CrearResumenLabel("Asistencia: 0%", new Point(455, 8));
            lblResumenAtencion = CrearResumenLabel("Atención requerida: 0", new Point(610, 8));
            lblResumenActualizado = CrearResumenLabel("Actualizado: --:--", new Point(820, 8));

            panel.Controls.Add(lblResumenCaja);
            panel.Controls.Add(lblResumenPendiente);
            panel.Controls.Add(lblResumenAsistencia);
            panel.Controls.Add(lblResumenAtencion);
            panel.Controls.Add(lblResumenActualizado);

            var ayuda = new Label
            {
                Text = "Consejo: usa Ctrl+F para localizar rápido un cliente y F2 para registrar asistencia.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(14, 40)
            };
            panel.Controls.Add(ayuda);

            return panel;
        }

        private static Label CrearResumenLabel(string texto, Point ubicacion)
        {
            return new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true,
                Location = ubicacion
            };
        }

        // ---------- HEADER Y BUSCADOR ----------

        private Panel ConstruirHeader()
        {
            var header = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = ColorTranslator.FromHtml("#0b5394")
            };

            lblNombreEstablecimiento = new Label
            {
                Text = nombreEstablecimiento,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 0)
            };
            header.Controls.Add(lblNombreEstablecimiento);

            lblTipoEstablecimiento = new Label
            {
                Text = $"{tipoEstablecimiento} · Centro de operaciones",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(205, 228, 245),
                AutoSize = true,
                Location = new Point(18, 26)
            };
            header.Controls.Add(lblTipoEstablecimiento);
            

            lblMes.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblMes.ForeColor = ColorTranslator.FromHtml("#cfe2f3");
            lblMes.AutoSize = true;
            lblMes.Location = new Point(18, 42);
            header.Controls.Add(lblMes);

            var panelBotones = new FlowLayoutPanel
            {
                Height = 46,
                Dock = DockStyle.Right,
                Padding = new Padding(0, 14, 10, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            var btnCambiarActividad = new Button
            {
                Text = "💪 Actividad",
                Width = 110,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#1565c0")
            };

            btnCambiarActividad.Click += (s, e) => CambiarActividad();
            panelBotones.Controls.Add(btnCambiarActividad);

            var btnNuevo = new Button
            {
                Text = "+ Nuevo",
                Width = 95,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#2e7d32")
            };
            btnNuevo.Click += (s, e) => AbrirNuevoAlumno();
            panelBotones.Controls.Add(btnNuevo);

            var btnAsistenciaRapida = new Button
            {
                Text = "🔳 Asistencia",
                Width = 115,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#e0a800")
            };
            btnAsistenciaRapida.Click += (s, e) => AbrirAsistenciaRapida();
            panelBotones.Controls.Add(btnAsistenciaRapida);

            var btnPrecios = new Button
            {
                Text = "💲 Precios",
                Width = 105,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#1366b0")
            };
            btnPrecios.Click += (s, e) => AbrirPrecios();
            panelBotones.Controls.Add(btnPrecios);

            var btnHistorial = new Button
            {
                Text = "📊 Historial",
                Width = 110,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#0288d1")
            };
            btnHistorial.Click += BtnHistorial_Click;
            panelBotones.Controls.Add(btnHistorial);

            var menuMas = new ContextMenuStrip();

            menuMas.Items.Add(
                "📤 Exportar resumen (CSV / Excel)",
                null,
                (s, e) => ExportarResumen());

            menuMas.Items.Add(
                "🖨️ Imprimir todos los QR",
                null,
                (s, e) => ImprimirTodosLosQr());

            menuMas.Items.Add(
                "🔔 Ver avisos del mes",
                null,
                (s, e) => VerAvisos());

            menuMas.Items.Add(
                "📜 Registro de auditoría (quién hizo qué)",
                null,
                (s, e) => AbrirAuditoria());

            menuMas.Items.Add(
                "⚙️ Configuración del establecimiento",
                null,
                (s, e) =>
                {
                    using var form =
                        new ConfiguracionEstablecimientoForm(actividadId);

                    form.ShowDialog(this);
                    CargarIdentidadEstablecimiento();
                });

            menuMas.Items.Add(
                "📋 Administrar planes",
                null,
                (s, e) =>
                {
                    using var form =
                        new AdministrarPlanesForm(actividadId);

                    form.ShowDialog(this);
                });

            menuMas.Items.Add(
                "⚙️ Configurar opciones de planes",
                null,
                (s, e) =>
                {
                    using var form =
                        new AdministrarOpcionesPlanForm(actividadId);

                    form.ShowDialog(this);
                });

            menuMas.Items.Add(
                "👤 Configuración del operador",
                null,
                (s, e) =>
                {
                    using var form =
                        new ConfiguracionOperadorForm();

                    form.ShowDialog(this);
                });

            menuMas.Items.Add(
                "🏊 Administrar actividades",
                null,
                (s, e) =>
                {
                    using var form =
                        new AdministrarActividadesForm();

                    form.ShowDialog(this);
                });

            menuMas.Items.Add(
                new ToolStripSeparator());

            menuMas.Items.Add(
                "ℹ️ Acerca de y derechos de autor",
                null,
                (s, e) =>
                {
                    using var form = new AcercaDeForm(nombreEstablecimiento);
                    form.ShowDialog(this);
                });

            var btnMas = new Button
            {
                Text = "⋮ Más",
                Width = 70,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#1366b0")
            };
            btnMas.Click += (s, e) => menuMas.Show(btnMas, new Point(0, btnMas.Height));
            panelBotones.Controls.Add(btnMas);

            var btnActualizar = new Button
            {
                Text = "⟲ Actualizar",
                Width = 115,
                Height = 34,
                Margin = new Padding(3, 0, 3, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = ColorTranslator.FromHtml("#1366b0")
            };
            btnActualizar.Click += (s, e) => Refrescar();
            panelBotones.Controls.Add(btnActualizar);

            header.Controls.Add(panelBotones);

            return header;
        }

        private Panel ConstruirBusqueda()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.White };

            var lbl = new Label
            {
                Text = "🔍 Buscar:",
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(16, 11)
            };
            panel.Controls.Add(lbl);

            txtBuscar.Location = new Point(105, 8);
            txtBuscar.Width = 260;
            txtBuscar.PlaceholderText = "Nombre, código o cédula...";
            txtBuscar.TextChanged += (s, e) => AplicarFiltroBusqueda();
            panel.Controls.Add(txtBuscar);

            // Indicador de Filtro Activo por Tarjeta
            lblFiltroActivo.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblFiltroActivo.ForeColor = ColorTranslator.FromHtml("#0b5394");
            lblFiltroActivo.Location = new Point(380, 11);
            lblFiltroActivo.AutoSize = true;
            lblFiltroActivo.Visible = false;
            panel.Controls.Add(lblFiltroActivo);

            btnLimpiarFiltro.Text = "✖ Mostrar todos";
            btnLimpiarFiltro.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            btnLimpiarFiltro.Location = new Point(720, 7);
            btnLimpiarFiltro.Size = new Size(110, 26);
            btnLimpiarFiltro.FlatStyle = FlatStyle.Flat;
            btnLimpiarFiltro.Visible = false;
            btnLimpiarFiltro.Click += (s, e) => SeleccionarFiltro(FiltroTarjeta.Todos);
            panel.Controls.Add(btnLimpiarFiltro);

            // Selector de rol y operador activo para Auditoría
            var lblOp = new Label
            {
                Text = "👤 Operador:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#334155"),
                AutoSize = true,
                Location = new Point(865, 11)
            };
            panel.Controls.Add(lblOp);

            var cbRolOperador = new ComboBox
            {
                Location = new Point(990, 8),
                Width = 115,
                Font = new Font("Segoe UI", 9f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbRolOperador.Items.AddRange(new object[]
            {
                "Recepción",
                "Administración",
                "Entrenador"
            });

            var txtNombreOperador = new TextBox
            {
                Location = new Point(1110, 8),
                Width = 115,
                Font = new Font("Segoe UI", 9f)
            };

            int indiceRol = cbRolOperador.Items.IndexOf(
                string.IsNullOrWhiteSpace(SesionUsuario.RolActual)
                    ? "Recepción"
                    : SesionUsuario.RolActual);

            cbRolOperador.SelectedIndex = indiceRol >= 0 ? indiceRol : 0;
            txtNombreOperador.Text = SesionUsuario.OperadorActual;

            cbRolOperador.SelectedIndexChanged += (s, e) =>
            {
                if (cbRolOperador.SelectedItem == null)
                    return;

                string rol = cbRolOperador.SelectedItem.ToString() ?? "Recepción";
                SesionUsuario.RolActual = rol;
                CargarOperadorDeRol(rol, txtNombreOperador);
            };

            txtNombreOperador.TextChanged += (s, e) =>
            {
                SesionUsuario.OperadorActual = txtNombreOperador.Text.Trim();
            };

            txtNombreOperador.Leave += (s, e) =>
            {
                GuardarOperadorActual(
                    SesionUsuario.RolActual,
                    txtNombreOperador.Text);
            };

            panel.Controls.Add(cbRolOperador);
            panel.Controls.Add(txtNombreOperador);

            return panel;
        }

        private void ConstruirTabla()
        {
            listaAlumnos.View = View.Details;
            listaAlumnos.FullRowSelect = true;
            listaAlumnos.GridLines = true;
            listaAlumnos.MultiSelect = false;
            listaAlumnos.Dock = DockStyle.Fill;
            listaAlumnos.Margin = new Padding(0);
            listaAlumnos.Font = new Font("Segoe UI", 10);
            listaAlumnos.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listaAlumnos.BackColor = Color.White;
            listaAlumnos.GridLines = false;
            listaAlumnos.Scrollable = true;

            listaAlumnos.Columns.Clear();
            listaAlumnos.Columns.Add("Código", 75, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Cédula", 90, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Cliente", 185);
            listaAlumnos.Columns.Add("Plan / especificación", 300);
            listaAlumnos.Columns.Add("Cuota ($)", 80, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Abonado ($)", 90, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Falta ($)", 80, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Estado / Cobro", 125, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Clases", 70, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Restantes", 75, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Próximo pago", 105, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Días", 70, HorizontalAlignment.Center);
            listaAlumnos.Columns.Add("Asistencia", 110, HorizontalAlignment.Center);

            listaAlumnos.DoubleClick += (s, e) => MarcarAsistenciaSeleccionado();
        }

        private Panel ConstruirAcciones()
        {
            var panel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), BackColor = Color.White };

            var btnAsistencia = new Button
            {
                Text = "Marcar asistencia",
                BackColor = ColorTranslator.FromHtml("#0b5394"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(16, 12),
                Width = 140,
                Height = 30
            };
            btnAsistencia.Click += (s, e) => MarcarAsistenciaSeleccionado();
            panel.Controls.Add(btnAsistencia);

            var btnEditar = new Button
            {
                Text = "Editar alumno",
                Location = new Point(164, 12),
                Width = 110,
                Height = 30
            };
            btnEditar.Click += (s, e) => EditarSeleccionado();
            panel.Controls.Add(btnEditar);

            var btnPago = new Button
            {
                Text = "💵 Registrar Abono / Pago",
                BackColor = ColorTranslator.FromHtml("#2e7d32"),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(282, 12),
                Width = 180,
                Height = 30
            };
            btnPago.Click += (s, e) => AbrirRegistroAbono();
            panel.Controls.Add(btnPago);

            var lblAyuda = new Label
            {
                Text = "Doble clic: asistencia · F3: cobro · F4: nuevo · Ctrl+F: buscar",
                ForeColor = AppTheme.Muted,
                AutoSize = true,
                Location = new Point(472, 18)
            };
            panel.Controls.Add(lblAyuda);

            var lblCopyright = new Label
            {
                Text = "© 2026 · Todos los derechos reservados · v2.2",
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(900, 18)
            };
            panel.Controls.Add(lblCopyright);

            return panel;
        }

        // ---------- FILTRADO INTELIGENTE Y LOGICA ----------

        private void AplicarFiltroBusqueda()
        {
            string texto = txtBuscar.Text.Trim().ToLower();

            // Carga los IDs de asistencia solo si el filtro activo es 'AsistieronHoy'
            HashSet<int>? idsAsistieronHoy = filtroActual == FiltroTarjeta.AsistieronHoy
                ? AppServices.CrearObtenerIdsAsistieronHoy().Ejecutar(actividadId)
                : null;

            var filtrados = estadosActuales.Where(e =>
            {
                // 1. Validar Filtro de Tarjeta Activa
                bool cumpleTarjeta = filtroActual switch
                {
                    FiltroTarjeta.AsistieronHoy => idsAsistieronHoy != null && idsAsistieronHoy.Contains(e.AlumnoId),
                    FiltroTarjeta.PorVencer => e.PorVencer,
                    FiltroTarjeta.ConAbonos => e.MontoAbonado > 0,
                    FiltroTarjeta.Pendientes => !e.EstaPagado,
                    _ => true
                };

                if (!cumpleTarjeta) return false;

                // 2. Validar Filtro por Texto de Búsqueda
                return string.IsNullOrEmpty(texto) ||
                       e.Nombre.ToLower().Contains(texto) ||
                       (!string.IsNullOrEmpty(e.Codigo) && e.Codigo.ToLower().Contains(texto)) ||
                       (!string.IsNullOrEmpty(e.Cedula) && e.Cedula.ToLower().Contains(texto));
            });

            MostrarEstados(filtrados);
        }

        private void MostrarEstados(IEnumerable<EstadoAlumnoMes> lista)
        {
            listaAlumnos.Items.Clear();

            foreach (var est in lista)
            {
                var item = new ListViewItem(string.IsNullOrWhiteSpace(est.Codigo) ? "-" : est.Codigo);
                item.SubItems.Add(string.IsNullOrWhiteSpace(est.Cedula) ? "-" : est.Cedula);
                item.SubItems.Add(est.Nombre);
                item.SubItems.Add(est.PlanNombre);
                item.SubItems.Add($"${est.CuotaTotal:0.##}");
                item.SubItems.Add($"${est.MontoAbonado:0.##}");
                item.SubItems.Add($"${est.MontoRestante:0.##}");
                item.SubItems.Add(est.EstadoPagoTexto);
                item.SubItems.Add(est.ControlaClases ? est.Tomadas.ToString() : "—");
                item.SubItems.Add(est.ControlaClases ? est.Restantes.ToString() : "—");
                item.SubItems.Add(est.VencimientoTexto);
                item.SubItems.Add(est.DiasRestantesTexto);
                item.SubItems.Add(est.EstadoTexto);
                item.ToolTipText = est.PlanNombre + $" | Próximo pago: {est.VencimientoTexto} | {est.DiasRestantesTexto}" + (est.ControlaClases ? $" | Clases: {est.Tomadas}/{est.ClasesMes}" : "");
                item.Tag = est.AlumnoId;

                // Coloreado inteligente
                if (est.EsMembresia && est.MembresiaVencida)
                {
                    item.BackColor = ColorTranslator.FromHtml("#ffebee");
                    item.ForeColor = ColorTranslator.FromHtml("#c62828");
                }
                else if (est.PorVencer || !est.EstaPagado)
                {
                    item.BackColor = ColorTranslator.FromHtml("#fff3e0");
                }
                else if (est.Estado == EstadoMes.Completo)
                {
                    item.BackColor = ColorTranslator.FromHtml("#e8f5e9");
                }
                else
                {
                    item.BackColor = Color.White;
                }

                if (est.EsMembresia)
                    item.ToolTipText += " | Membresía por 30 días";

                listaAlumnos.Items.Add(item);
            }
        }

        // ---------- ATAJOS DE TECLADO ----------

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F2:
                    AbrirAsistenciaRapida();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case Keys.F3:
                    AbrirRegistroAbono();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case Keys.F4:
                    AbrirNuevoAlumno();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case Keys.F5:
                    Refrescar();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;

                case Keys.F:
                    if (e.Control)
                    {
                        txtBuscar.Focus();
                        txtBuscar.SelectAll();

                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                    break;

                case Keys.Escape:
                    ManejarEscape();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void ManejarEscape()
        {
            // Si hay texto en búsqueda, primero lo limpiamos.
            if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
            {
                txtBuscar.Clear();
                return;
            }

            // Si hay un filtro activo, volvemos a mostrar todos.
            if (filtroActual != FiltroTarjeta.Todos)
            {
                SeleccionarFiltro(FiltroTarjeta.Todos);
                return;
            }

            // Si no hay nada que limpiar, cerramos el formulario secundario activo.
            // Los formularios secundarios normalmente se manejan con ShowDialog(),
            // por lo que Esc dentro de ellos será manejado por su propio formulario.
        }

        // ---------- EVENTOS GENERALES ----------

        private void BtnHistorial_Click(object? sender, EventArgs e)
        {
            var formHistorial = new HistorialMesesForm(actividadId);
            formHistorial.ShowDialog(this);
        }

        private void AbrirPrecios()
        {
            var form = new PreciosForm(actividadId);
            form.PreciosActualizados += Refrescar;
            form.ShowDialog(this);
        }

        private void AbrirRegistroAbono()
        {
            if (listaAlumnos.SelectedItems.Count == 0)
            {
                MessageBox.Show("Primero selecciona un alumno de la lista.", "Selecciona un alumno", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int alumnoId = (int)listaAlumnos.SelectedItems[0].Tag!;
            var estado = AppServices.CrearObtenerEstadoAlumno().Ejecutar(alumnoId, anioActual, mesActual);
            if (estado == null) return;

            var form = new RegistrarAbonoForm(estado, anioActual, mesActual);
            form.AbonoRegistrado += Refrescar;
            form.ShowDialog(this);
        }

        private void AbrirNuevoAlumno()
        {
            var form = new NuevoAlumnoForm(actividadId);
            form.Guardado += Refrescar;
            form.ShowDialog(this);
        }

        private void AbrirAsistenciaRapida()
        {
            var form = new AsistenciaRapidaForm();
            form.AsistenciaRegistrada += Refrescar;
            form.ShowDialog(this);
        }

        private Alumno? ObtenerAlumnoSeleccionado()
        {
            if (listaAlumnos.SelectedItems.Count == 0)
            {
                MessageBox.Show("Primero selecciona un alumno de la lista.", "Selecciona un alumno", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            int alumnoId = (int)listaAlumnos.SelectedItems[0].Tag!;
            return AppServices.CrearObtenerAlumno().Ejecutar(alumnoId);
        }

        private void MarcarAsistenciaSeleccionado()
        {
            var alumno = ObtenerAlumnoSeleccionado();
            if (alumno == null) return;

            var form = new AsistenciaHoyForm(alumno);
            form.Marcado += Refrescar;
            form.ShowDialog(this);
        }

        private void EditarSeleccionado()
        {
            var alumno = ObtenerAlumnoSeleccionado();
            if (alumno == null) return;

            var form = new EditarAlumnoForm(alumno);
            form.Guardado += Refrescar;
            form.ShowDialog(this);
        }

        private void ExportarResumen()
        {
            using var dialogo = new SaveFileDialog
            {
                Filter = "Archivo CSV, se abre con Excel (*.csv)|*.csv",
                FileName = $"Resumen_{MesesEs[mesActual - 1]}_{anioActual}.csv"
            };

            if (dialogo.ShowDialog(this) == DialogResult.OK)
            {
                Exportador.ExportarResumenCsv(estadosActuales, dialogo.FileName);
                MessageBox.Show("Resumen exportado. Ábrelo con Excel.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ImprimirTodosLosQr()
        {
            var alumnos = AppServices.CrearObtenerAlumnosPorActividad().Ejecutar(actividadId);
            if (alumnos.Count == 0)
            {
                MessageBox.Show("No hay alumnos activos para generar QR.", "Sin alumnos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string ruta = ImpresionQr.GenerarHojaQrTodos(alumnos);
            ImpresionQr.AbrirEnNavegador(ruta);
        }

        private void VerAvisos()
        {
            var form = new AvisosForm(estadosActuales);
            form.AbonoRegistrado += Refrescar;
            form.ShowDialog(this);
        }

        private void AbrirAuditoria()
        {
            var form = new AuditoriaForm(actividadId);
            form.ShowDialog(this);
        }

        private void CargarIdentidadEstablecimiento()
        {
            try
            {
                var configuracion =
                    AppServices.CrearObtenerConfiguracion().Ejecutar(actividadId);

                nombreEstablecimiento = string.IsNullOrWhiteSpace(configuracion.Nombre)
                    ? "Control de Gestión"
                    : configuracion.Nombre.Trim();

                tipoEstablecimiento = string.IsNullOrWhiteSpace(configuracion.TipoEstablecimiento)
                    ? "Gestión deportiva"
                    : configuracion.TipoEstablecimiento.Trim();

                if (lblNombreEstablecimiento != null)
                    lblNombreEstablecimiento.Text = nombreEstablecimiento;

                if (lblTipoEstablecimiento != null)
                    lblTipoEstablecimiento.Text =
                        $"{tipoEstablecimiento} · Centro de operaciones";
            }
            catch
            {
                nombreEstablecimiento = "Control de Gestión";
                tipoEstablecimiento = "Gestión deportiva";

                if (lblNombreEstablecimiento != null)
                    lblNombreEstablecimiento.Text = nombreEstablecimiento;

                if (lblTipoEstablecimiento != null)
                    lblTipoEstablecimiento.Text =
                        $"{tipoEstablecimiento} · Centro de operaciones";
            }
        }

        private void CargarOperadorDeActividad()
        {
            string rol = string.IsNullOrWhiteSpace(SesionUsuario.RolActual)
                ? "Recepción"
                : SesionUsuario.RolActual;

            CargarOperadorDeRol(rol);
        }

        private void CargarOperadorDeRol(
            string rol,
            TextBox? destino = null)
        {
            try
            {
                var configuracion =
                    AppServices.CrearObtenerConfiguracionOperador().Ejecutar(rol);

                SesionUsuario.RolActual = rol;
                SesionUsuario.OperadorActual =
                    configuracion.Nombre.Trim();

                if (destino != null)
                {
                    destino.Text = SesionUsuario.OperadorActual;
                    destino.SelectAll();
                }
            }
            catch
            {
                SesionUsuario.RolActual = rol;
                SesionUsuario.OperadorActual = "";

                if (destino != null)
                    destino.Clear();
            }
        }

        private void GuardarOperadorActual(string rol, string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return;

            try
            {
                var configuracion = new ConfiguracionOperador
                {
                    Rol = rol,
                    Nombre = nombre.Trim()
                };

                AppServices.CrearGuardarConfiguracionOperador()
                    .Ejecutar(configuracion);

                SesionUsuario.RolActual = configuracion.Rol;
                SesionUsuario.OperadorActual = configuracion.Nombre;
            }
            catch
            {
                // La configuración del operador no debe impedir el uso de la aplicación.
            }
        }

        private void CambiarActividad()
        {
            using var selector = new SeleccionarActividadForm();

            if (selector.ShowDialog(this) != DialogResult.OK)
                return;

            if (selector.ActividadIdSeleccionada == actividadId)
                return;

            actividadId = selector.ActividadIdSeleccionada;
            ContextoActividad.ActividadId = actividadId;
            CargarIdentidadEstablecimiento();
            Text = "Viking · Gestión";
            CargarOperadorDeActividad();

            filtroActual = FiltroTarjeta.Todos;
            txtBuscar.Clear();

            Refrescar();
        }

        private void Refrescar()
        {
            estadosActuales = AppServices.CrearObtenerDashboard()
                .Ejecutar(anioActual, mesActual, actividadId);

            decimal totalCuotas = estadosActuales.Sum(e => e.CuotaTotal);
            decimal totalCobrado = estadosActuales.Sum(e => e.MontoAbonado);
            // No restar los totales agregados: un sobrepago de un alumno no
            // puede compensar lo que deben otros. Sumamos el saldo pendiente
            // ya calculado (y capado en 0) de cada alumno individualmente.
            decimal totalPendiente = estadosActuales.Sum(e => e.MontoRestante);

            int avisosClases = estadosActuales.Count(e => e.PorVencer);

            int avisosPago = estadosActuales.Count(e => !e.EstaPagado);

            string textoMes = $"{MesesEs[mesActual - 1]} {anioActual}";

            lblMes.Text =
                $"{textoMes}   |   Cobrar: ${totalCuotas}   |   Cobrado: ${totalCobrado}   |   Falta: ${totalPendiente}   |   ⚠️ {avisosClases} por vencer clases   |   💵 {avisosPago} con pago pendiente";

            AplicarFiltroBusqueda();
            ActualizarTarjetasMetricas();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            AppServices.CrearCrearRespaldoAutomatico().Ejecutar();
        }
    }
}
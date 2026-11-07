using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Presentation.Forms
{
    /// <summary>
    /// Tarjeta emergente de carné en pantalla para el momento del escaneo del QR.
    /// Diseñada con estética oscura premium (OLED kiosk display), foto circular con anillo
    /// semáforo, datos detallados, barra de progreso regresiva y sonidos diferenciados.
    /// </summary>
    public sealed class CarnetPantallaForm : Form
    {
        // Added fields for fade‑in/out animation and tooltip handling
        private readonly System.Windows.Forms.Timer timerFadeIn = new();
        private readonly System.Windows.Forms.Timer timerFadeOut = new();
        private const double OpacityStep = 0.05; // opacity change per tick

                private Rectangle rectBadge; // banner rectangle for tooltip positioning
        private readonly Panel badgePanel = new();
        private readonly ToolTip toolTipStatus = new();

        private readonly System.Windows.Forms.Timer timerCierre = new();
        private readonly System.Windows.Forms.Timer timerProgreso = new();

        private readonly CarnetAlumno carnet;
        private Image? fotoCargada;
        private readonly DateTime tiempoInicio;
        private readonly int duracionMs = 3800;
        private float progresoRestante = 1.0f;

        public CarnetPantallaForm(CarnetAlumno carnet)
        {
            this.carnet = carnet ?? throw new ArgumentNullException(nameof(carnet));

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(440, 580);
            TopMost = true;
            ShowInTaskbar = false;
            PosicionarSinTapar();
            DoubleBuffered = true; Opacity = 0;
            BackColor = ColorTranslator.FromHtml("#0B1120"); // Deep Dark Navy/Slate

            tiempoInicio = DateTime.UtcNow;

            // Aparición gradual: el formulario nace con Opacity = 0, así que
            // este temporizador es el que lo hace visible.
            timerFadeIn.Interval = 15;
            timerFadeIn.Tick += (s, e) =>
            {
                Opacity = Math.Min(1.0, Opacity + OpacityStep * 2);
                if (Opacity >= 1.0)
                    timerFadeIn.Stop();
            };

            // Desaparición gradual y cierre al terminar el tiempo del carnet.
            timerFadeOut.Interval = 15;
            timerFadeOut.Tick += (s, e) =>
            {
                if (Opacity <= OpacityStep)
                {
                    timerFadeOut.Stop();
                    Close();
                    return;
                }

                Opacity -= OpacityStep;
            };

            // Timer de cierre
            timerCierre.Interval = duracionMs;
            timerCierre.Tick += (s, e) =>
            {
                timerCierre.Stop();
                timerProgreso.Stop();
                // Start fade‑out animation instead of immediate close
                timerFadeOut.Start();
            };

            // Timer de animación de la barra regresiva (cada 35ms)
            timerProgreso.Interval = 35;
            timerProgreso.Tick += (s, e) =>
            {
                double transcurrido = (DateTime.UtcNow - tiempoInicio).TotalMilliseconds;
                progresoRestante = Math.Max(0f, 1.0f - (float)(transcurrido / duracionMs));
                Invalidate(new Rectangle(0, Height - 8, Width, 8));
                if (progresoRestante <= 0f)
                {
                    timerProgreso.Stop();
                }
            };

            CargarFoto();
            ReproducirSonidoSegunEstado();

            KeyDown += (s, e) =>
            {
                if (e.KeyCode is Keys.Escape or Keys.Enter or Keys.Space)
                    Close();
            };
            Click += (s, e) => Close();

            Shown += (s, e) =>
            {
                timerFadeIn.Start();
                timerCierre.Start();
                timerProgreso.Start();
            };
        }

        /// <summary>
        /// Coloca el carnet al lado del lector de entrada rápida (si está abierto)
        /// para que no se tapen entre sí. Sin lector, queda centrado en pantalla.
        /// </summary>
        private void PosicionarSinTapar()
        {
            try
            {
                var lector = System.Windows.Forms.Application.OpenForms
                    .OfType<AsistenciaRapidaForm>()
                    .FirstOrDefault(f => f.Visible && !f.IsDisposed);

                var pantalla = lector != null
                    ? Screen.FromControl(lector)
                    : Screen.FromPoint(Cursor.Position);
                var area = pantalla.WorkingArea;
                const int margen = 12;

                int y = area.Top + Math.Max(0, (area.Height - Height) / 2);
                int x = area.Left + Math.Max(0, (area.Width - Width) / 2);

                if (lector != null)
                {
                    x = lector.Right + margen;

                    if (x + Width > area.Right)
                    {
                        x = lector.Left - Width - margen;

                        if (x < area.Left)
                        {
                            // No cabe a ningún lado: el lector se pega a la izquierda
                            // de la pantalla y el carnet va a su derecha.
                            lector.Left = area.Left + margen;
                            x = Math.Max(area.Left, Math.Min(area.Right - Width - margen, lector.Right + margen));
                        }
                    }
                }

                Location = new Point(x, y);
            }
            catch (Exception ex)
            {
                AsistenciaNatacion.Infrastructure.Logging.Logger.Warn("No se pudo posicionar el carnet: " + ex.Message);
                StartPosition = FormStartPosition.CenterScreen;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using var path = CrearRectanguloRedondeado(new Rectangle(0, 0, Width, Height), 24);
            Region = new Region(path);
        }

        private void CargarFoto()
        {
            try
            {
                string? ruta = carnet.FotoRuta;
                if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                {
                    // Fallback directo a fotos/{codigo}.jpg
                    string rutaAlterna = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fotos", $"{carnet.Codigo}.jpg");
                    if (File.Exists(rutaAlterna))
                        ruta = rutaAlterna;
                }

                if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta))
                {
                    using var stream = new MemoryStream(File.ReadAllBytes(ruta));
                    fotoCargada = Image.FromStream(stream);
                }
            }
            catch
            {
                fotoCargada = null;
            }
        }

        private void ReproducirSonidoSegunEstado()
        {
            try
            {
                switch (carnet.EstadoSemaforo)
                {
                    case SemaforoEstado.Rojo:
                        SystemSounds.Hand.Play();
                        break;
                    case SemaforoEstado.Amarillo:
                        SystemSounds.Exclamation.Play();
                        break;
                    case SemaforoEstado.Verde:
                    default:
                        SystemSounds.Asterisk.Play();
                        break;
                }
            }
            catch
            {
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Paleta de colores según el semáforo
            Color colorAcento;
            Color colorFondoBadge;
            string iconoBadge;
            string textoBadgePrincipal;

            switch (carnet.EstadoSemaforo)
            {
                case SemaforoEstado.Rojo:
                    colorAcento = ColorTranslator.FromHtml("#EF4444"); // Rojo moderno
                    colorFondoBadge = ColorTranslator.FromHtml("#7F1D1D"); // Vino oscuro
                    iconoBadge = "";
                    textoBadgePrincipal = "ACCESO DENEGADO";
                    break;
                case SemaforoEstado.Amarillo:
                    colorAcento = ColorTranslator.FromHtml("#F59E0B"); // Ámbar moderno
                    colorFondoBadge = ColorTranslator.FromHtml("#78350F"); // Ámbar oscuro
                    iconoBadge = "";
                    textoBadgePrincipal = "ATENCIÓN REQUERIDA";
                    break;
                case SemaforoEstado.Verde:
                default:
                    colorAcento = ColorTranslator.FromHtml("#22C55E"); // Verde esmeralda
                    colorFondoBadge = ColorTranslator.FromHtml("#14532D"); // Verde oscuro
                    iconoBadge = "";
                    textoBadgePrincipal = "ACCESO AUTORIZADO";
                    break;
            }

            // Borde exterior del carné con esquinas redondeadas
            using (var pathBorde = CrearRectanguloRedondeado(new Rectangle(1, 1, Width - 2, Height - 2), 24))
            using (var penBorde = new Pen(colorAcento, 2.5f))
            {
                g.DrawPath(penBorde, pathBorde);
            }

            // ── 1. BANNER SUPERIOR DE ESTADO ─────────────────────────
            var rectBadge = new Rectangle(20, 16, Width - 40, 48);
            using (var pathBadge = CrearRectanguloRedondeado(rectBadge, 14))
            using (var brushBadge = new SolidBrush(colorFondoBadge))
            using (var penBadge = new Pen(colorAcento, 1.5f))
            {
                g.FillPath(brushBadge, pathBadge);
                g.DrawPath(penBadge, pathBadge);
            }

            using (var fontBadge = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (var brushTextoBadge = new SolidBrush(Color.White))
            {
                string textoEstado = $"{textoBadgePrincipal} · {carnet.TituloEstado.ToUpperInvariant()}";
                var sfCenter = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(textoEstado, fontBadge, brushTextoBadge, rectBadge, sfCenter);
            }

            // ── 2. FOTO CIRCULAR O AVATAR DE INICIALES ───────────────
            int fotoSize = 130;
            int fotoX = (Width - fotoSize) / 2;
            int fotoY = 76;
            var fotoRect = new Rectangle(fotoX, fotoY, fotoSize, fotoSize);

            // Anillo exterior decorativo
            using (var penAnilloExterior = new Pen(Color.FromArgb(50, colorAcento), 8))
            {
                g.DrawEllipse(penAnilloExterior, fotoRect);
            }

            using (var pathCirculo = new GraphicsPath())
            {
                pathCirculo.AddEllipse(fotoRect);

                if (fotoCargada != null)
                {
                    g.SetClip(pathCirculo);
                    g.DrawImage(fotoCargada, fotoRect);
                    g.ResetClip();
                }
                else
                {
                    // Fondo del avatar si no hay foto
                    using var brushAvatar = new SolidBrush(ColorTranslator.FromHtml("#1E293B"));
                    g.FillPath(brushAvatar, pathCirculo);

                    // Iniciales del alumno
                    string iniciales = ObtenerIniciales(carnet.Nombre);
                    using var fontIniciales = new Font("Segoe UI", 36, FontStyle.Bold);
                    using var brushIniciales = new SolidBrush(ColorTranslator.FromHtml("#94A3B8"));
                    var sfAvatar = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(iniciales, fontIniciales, brushIniciales, fotoRect, sfAvatar);
                }

                // Anillo nítido de color sobre el borde
                using var penAnillo = new Pen(colorAcento, 3);
                g.DrawEllipse(penAnillo, fotoRect);
            }

            // ── 3. NOMBRE DEL ALUMNO ──────────────────────────────────
            int actualY = fotoY + fotoSize + 14;
            using (var fontNombre = new Font("Segoe UI", 17, FontStyle.Bold))
            using (var brushNombre = new SolidBrush(Color.White))
            {
                var sfNombre = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter
                };
                g.DrawString(carnet.Nombre, fontNombre, brushNombre, new RectangleF(16, actualY, Width - 32, 34), sfNombre);
            }

            // ── 4. BADGE DE CÓDIGO ID ─────────────────────────────────
            actualY += 36;
            int badgeIdAncho = 130;
            var rectId = new Rectangle((Width - badgeIdAncho) / 2, actualY, badgeIdAncho, 26);
            using (var pathId = CrearRectanguloRedondeado(rectId, 12))
            using (var brushId = new SolidBrush(ColorTranslator.FromHtml("#1E293B")))
            using (var penId = new Pen(ColorTranslator.FromHtml("#334155"), 1))
            {
                g.FillPath(brushId, pathId);
                g.DrawPath(penId, pathId);
            }

            using (var fontId = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            using (var brushIdTexto = new SolidBrush(ColorTranslator.FromHtml("#FACC15"))) // Amarillo dorado
            {
                var sfId = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString($"ID: {carnet.Codigo}", fontId, brushIdTexto, rectId, sfId);
            }

            // ── 5. TARJETA DE DATOS PRINCIPALES (GRID ELEGANTE) ───────
            actualY += 34;
            var rectFicha = new Rectangle(20, actualY, Width - 40, 150);
            using (var pathFicha = CrearRectanguloRedondeado(rectFicha, 16))
            using (var brushFicha = new SolidBrush(ColorTranslator.FromHtml("#131D31")))
            using (var penFicha = new Pen(ColorTranslator.FromHtml("#1E293B"), 1))
            {
                g.FillPath(brushFicha, pathFicha);
                g.DrawPath(penFicha, pathFicha);
            }

            // Filas de información dentro de la ficha
            int filaY = actualY + 12;
            using (var fontLabel = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var fontValor = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var brushLabel = new SolidBrush(ColorTranslator.FromHtml("#64748B"))) // Slate suave
            using (var brushValor = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0"))) // Blanco tenue
            using (var brushAlerta = new SolidBrush(colorAcento))
            {
                // Cédula & Teléfono en 2 columnas
                string cedula = string.IsNullOrWhiteSpace(carnet.Cedula) ? "Sin C.I." : $"V-{carnet.Cedula}";
                string telefono = string.IsNullOrWhiteSpace(carnet.Telefono) ? "Sin Teléfono" : carnet.Telefono;

                g.DrawString("CÉDULA", fontLabel, brushLabel, 34, filaY);
                g.DrawString(cedula, fontValor, brushValor, 34, filaY + 16);

                g.DrawString("TELÉFONO", fontLabel, brushLabel, 230, filaY);
                g.DrawString(telefono, fontValor, brushValor, 230, filaY + 16);

                filaY += 42;
                // Separador sutil
                using (var penSep = new Pen(ColorTranslator.FromHtml("#1E293B"), 1))
                {
                    g.DrawLine(penSep, 34, filaY - 5, Width - 34, filaY - 5);
                }

                // Plan deportivo
                g.DrawString("PLAN", fontLabel, brushLabel, 34, filaY);
                string plan = string.IsNullOrWhiteSpace(carnet.PlanNombre) ? "Sin plan asignado" : carnet.PlanNombre;
                g.DrawString(plan, fontValor, brushValor, 34, filaY + 16);

                filaY += 42;
                using (var penSep = new Pen(ColorTranslator.FromHtml("#1E293B"), 1))
                {
                    g.DrawLine(penSep, 34, filaY - 5, Width - 34, filaY - 5);
                }

                // Vencimiento y saldo
                string vence = carnet.FechaVencimiento.HasValue
                    ? carnet.FechaVencimiento.Value.ToString("dd/MM/yyyy")
                    : "—";

                g.DrawString("VENCIMIENTO", fontLabel, brushLabel, 34, filaY);
                g.DrawString(vence, fontValor, brushValor, 34, filaY + 16);

                g.DrawString("SALDO PENDIENTE", fontLabel, brushLabel, 230, filaY);
                string saldo = carnet.SaldoPendiente > 0
                    ? $"Debe ${carnet.SaldoPendiente:0.##}"
                    : "Al día ($0)";
                g.DrawString(saldo, fontValor, carnet.SaldoPendiente > 0 ? brushAlerta : brushValor, 230, filaY + 16);
            }

            // ── 6. PIE / MENSAJE MOTIVO ───────────────────────────────
            actualY += 158;
            string detalle = !string.IsNullOrWhiteSpace(carnet.MotivoDetalle)
                ? carnet.MotivoDetalle
                : (carnet.EstadoSemaforo == SemaforoEstado.Verde
                    ? "Asistencia verificada · ¡Buen entrenamiento!"
                    : carnet.SubtituloEstado);

            using (var fontDetalle = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var brushDetalle = new SolidBrush(colorAcento))
            {
                var sfDetalle = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(detalle, fontDetalle, brushDetalle, new RectangleF(16, actualY, Width - 32, 28), sfDetalle);
            }

            // ── 7. BARRA REGRESIVA DE TIEMPO (COUNTDOWN) ──────────────
            int barY = Height - 6;
            int barW = (int)(Width * progresoRestante);
            using (var brushBarra = new SolidBrush(colorAcento))
            {
                g.FillRectangle(brushBarra, 0, barY, barW, 6);
            }
        }

        private static string ObtenerIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "?";
            var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, Math.Min(2, partes[0].Length)).ToUpperInvariant();
            return $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant();
        }

        private static GraphicsPath CrearRectanguloRedondeado(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            // Superior izquierda
            path.AddArc(arc, 180, 90);

            // Superior derecha
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // Inferior derecha
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // Inferior izquierda
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timerFadeIn?.Dispose();
                timerFadeOut?.Dispose();
                timerCierre?.Dispose();
                timerProgreso?.Dispose();
                fotoCargada?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

using System.Drawing;
using System.Windows.Forms;
using AsistenciaNatacion.Presentation.Theme;

namespace AsistenciaNatacion;

public class AcercaDeForm : Form
{
    public AcercaDeForm(string nombreProducto = "VIKING")
    {
        Text = "Acerca de la aplicación";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 390); // Ampliado ligeramente para evitar apretar los elementos inferiores
        BackColor = AppTheme.Background;

        // Header Superior
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 104,
            BackColor = AppTheme.Primary
        };
        Controls.Add(header);

        var titulo = new Label
        {
            Text = "VIKING",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(28, 18)
        };
        header.Controls.Add(titulo);

        var subtitulo = new Label
        {
            Text = "Plataforma de gestión para instituciones deportivas",
            ForeColor = Color.FromArgb(220, 235, 248),
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = false,
            Width = 460,
            Height = 36,
            Location = new Point(30, 58)
        };
        header.Controls.Add(subtitulo);

        // Cuerpo / Información Legal
        var cuerpo = new Label
        {
            AutoSize = false,
            Width = 464,
            Height = 165,
            Location = new Point(28, 122),
            ForeColor = AppTheme.Text,
            Font = new Font("Segoe UI", 9.75f),
            Text = "© 2026 Josmar Alejandro Avendaño León. Todos los derechos reservados.\n\n" +
                   "El código fuente, diseño, estructura, documentación y recursos propios de este software forman parte de la propiedad intelectual de su titular.\n\n" +
                   "Uso, distribución, modificación o comercialización: sujeto a la autorización previa y por escrito del titular."
        };
        Controls.Add(cuerpo);

        // Pie de Página (Versión y Botón de Cierre)
        var version = new Label
        {
            Text = "Versión comercial 2.2 · Arquitectura hexagonal",
            ForeColor = AppTheme.Muted,
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Location = new Point(28, 338)
        };
        Controls.Add(version);

        var btnCerrar = new Button
        {
            Text = "Cerrar",
            Width = 100,
            Height = 34,
            Location = new Point(392, 330),
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        Controls.Add(btnCerrar);

        AcceptButton = btnCerrar;

        // Aplicación del Tema
        AppTheme.Aplicar(this);

        // Garantizar estilos del botón primario tras la aplicación del tema
        btnCerrar.BackColor = AppTheme.Primary;
        btnCerrar.ForeColor = Color.White;
        btnCerrar.FlatAppearance.BorderSize = 0;
    }
}
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Infrastructure.Logging;
using AsistenciaNatacion.Presentation.Theme;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Presentation.Forms;

/// <summary>
/// Asistente de primera ejecución: datos del negocio, quién usa el sistema y
/// cómo conectar VikingMobile. Se muestra una sola vez en una instalación
/// nueva (sin alumnos registrados) y deja una marca para no repetirse.
/// </summary>
public sealed class BienvenidaForm : Form
{
    private const string ArchivoMarca = "bienvenida.ok";

    private readonly int actividadId;

    private readonly Panel[] pasos = new Panel[4];
    private int pasoActual;

    private readonly TextBox txtNegocio = new();
    private readonly TextBox txtIdentificacion = new();
    private readonly ComboBox cbTipo = new();
    private readonly ComboBox cbRol = new();
    private readonly TextBox txtOperador = new();
    private readonly Label lblError = new();
    private readonly Label lblResumen = new();

    private readonly Button btnAtras = new();
    private readonly Button btnSiguiente = new();
    private readonly Button btnOmitir = new();

    public BienvenidaForm(int actividadId)
    {
        this.actividadId = actividadId;

        Text = "Bienvenido a VIKING";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(620, 460);
        BackColor = AppTheme.Background;
        Font = new Font("Segoe UI", 10f);

        try { Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath); }
        catch { /* sin ícono propio: se usa el predeterminado */ }

        ConstruirCabecera();
        ConstruirPasos();
        ConstruirPie();
        MostrarPaso(0);
    }

    // ------------------------------------------------------------------
    // ¿Corresponde mostrarlo?
    // ------------------------------------------------------------------

    private static string RutaMarca =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ArchivoMarca);

    /// <summary>
    /// True solo en una instalación nueva: sin marca de bienvenida y sin
    /// alumnos registrados. Los equipos que ya trabajan nunca lo ven.
    /// </summary>
    public static bool Corresponde()
    {
        try
        {
            if (File.Exists(RutaMarca))
                return false;

            using var conn = new SqliteConnection(Database.ObtenerConnectionString());
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM alumnos;";
            return Convert.ToInt32(cmd.ExecuteScalar()) == 0;
        }
        catch (Exception ex)
        {
            Logger.Warn("No se pudo comprobar si corresponde la bienvenida.", ex);
            return false;
        }
    }

    private static void GuardarMarca()
    {
        try
        {
            File.WriteAllText(RutaMarca, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            Logger.Warn("No se pudo guardar la marca de bienvenida.", ex);
        }
    }

    // ------------------------------------------------------------------
    // Construcción de la interfaz
    // ------------------------------------------------------------------

    private void ConstruirCabecera()
    {
        var cabecera = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = AppTheme.PrimaryDark
        };

        cabecera.Controls.Add(new Label
        {
            Text = "VIKING",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(28, 10)
        });

        cabecera.Controls.Add(new Label
        {
            Text = "Configuración inicial",
            Font = new Font("Segoe UI", 10f),
            ForeColor = AppTheme.Accent,
            AutoSize = true,
            Location = new Point(31, 50)
        });

        Controls.Add(cabecera);
    }

    private Label Titulo(string texto) => new()
    {
        Text = texto,
        Font = new Font("Segoe UI", 15f, FontStyle.Bold),
        ForeColor = AppTheme.Primary,
        AutoSize = true,
        Location = new Point(0, 0)
    };

    private Label Parrafo(string texto, int y, int alto = 60) => new()
    {
        Text = texto,
        ForeColor = AppTheme.Text,
        Location = new Point(0, y),
        Size = new Size(560, alto)
    };

    private Label Etiqueta(string texto, int y) => new()
    {
        Text = texto,
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
        ForeColor = AppTheme.Text,
        AutoSize = true,
        Location = new Point(0, y)
    };

    private static void Campo(Control c, int y)
    {
        c.Location = new Point(0, y);
        c.Size = new Size(560, 28);
        c.Font = new Font("Segoe UI", 10.5f);
    }

    private void ConstruirPasos()
    {
        for (int i = 0; i < pasos.Length; i++)
        {
            pasos[i] = new Panel
            {
                Location = new Point(30, 96),
                Size = new Size(560, 290),
                BackColor = Color.Transparent,
                Visible = false
            };
            Controls.Add(pasos[i]);
        }

        // Paso 1: bienvenida
        pasos[0].Controls.Add(Titulo("Te damos la bienvenida"));
        pasos[0].Controls.Add(Parrafo(
            "VIKING te ayuda a controlar alumnos, planes, pagos y asistencia por código QR.\n\n" +
            "En los próximos pasos configuraremos los datos de tu negocio y quién usará el " +
            "sistema. Toma menos de un minuto y puedes cambiarlo después desde el menú de " +
            "configuración.", 44, 150));

        // Paso 2: negocio
        pasos[1].Controls.Add(Titulo("Datos de tu negocio"));
        pasos[1].Controls.Add(Etiqueta("Nombre del negocio *", 50));
        Campo(txtNegocio, 76);
        pasos[1].Controls.Add(txtNegocio);
        pasos[1].Controls.Add(Etiqueta("Identificación / RIF", 116));
        Campo(txtIdentificacion, 142);
        pasos[1].Controls.Add(txtIdentificacion);
        pasos[1].Controls.Add(Etiqueta("Tipo de establecimiento", 182));
        Campo(cbTipo, 208);
        cbTipo.DropDownStyle = ComboBoxStyle.DropDownList;
        cbTipo.Items.AddRange(new object[]
        {
            "Natación",
            "CrossFit",
            "Entrenamiento Funcional",
            "Gimnasio",
            "Academia",
            "Otro"
        });
        cbTipo.SelectedItem = "Academia";
        pasos[1].Controls.Add(cbTipo);

        // Paso 3: operador
        pasos[2].Controls.Add(Titulo("¿Quién usará el sistema?"));
        pasos[2].Controls.Add(Etiqueta("Rol", 50));
        Campo(cbRol, 76);
        cbRol.DropDownStyle = ComboBoxStyle.DropDownList;
        cbRol.Items.AddRange(new object[] { "Recepción", "Administración", "Entrenador" });
        cbRol.SelectedIndex = 0;
        pasos[2].Controls.Add(cbRol);
        pasos[2].Controls.Add(Etiqueta("Nombre del operador *", 116));
        Campo(txtOperador, 142);
        pasos[2].Controls.Add(txtOperador);
        pasos[2].Controls.Add(Parrafo(
            "El nombre aparece en recibos y en el registro de auditoría.", 184, 40));

        // Paso 4: listo
        pasos[3].Controls.Add(Titulo("Todo listo"));
        lblResumen.ForeColor = AppTheme.Text;
        lblResumen.Location = new Point(0, 44);
        lblResumen.Size = new Size(560, 240);
        pasos[3].Controls.Add(lblResumen);

        lblError.ForeColor = Color.Firebrick;
        lblError.AutoSize = false;
        lblError.Location = new Point(30, 392);
        lblError.Size = new Size(560, 24);
        Controls.Add(lblError);
    }

    private void ConstruirPie()
    {
        var pie = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            BackColor = Color.White
        };

        btnOmitir.Text = "Omitir";
        btnOmitir.FlatStyle = FlatStyle.Flat;
        btnOmitir.FlatAppearance.BorderSize = 0;
        btnOmitir.ForeColor = AppTheme.Muted;
        btnOmitir.Size = new Size(90, 34);
        btnOmitir.Location = new Point(20, 11);
        btnOmitir.Cursor = Cursors.Hand;
        btnOmitir.Click += (s, e) =>
        {
            GuardarMarca();
            DialogResult = DialogResult.Cancel;
            Close();
        };

        btnAtras.Text = "Atrás";
        btnAtras.Size = new Size(100, 34);
        btnAtras.Location = new Point(400, 11);
        btnAtras.Click += (s, e) => MostrarPaso(pasoActual - 1);

        btnSiguiente.Text = "Siguiente";
        btnSiguiente.Size = new Size(110, 34);
        btnSiguiente.Location = new Point(506, 11);
        btnSiguiente.BackColor = AppTheme.Primary;
        btnSiguiente.ForeColor = Color.White;
        btnSiguiente.FlatStyle = FlatStyle.Flat;
        btnSiguiente.FlatAppearance.BorderSize = 0;
        btnSiguiente.Cursor = Cursors.Hand;
        btnSiguiente.Click += (s, e) => Avanzar();

        pie.Controls.AddRange(new Control[] { btnOmitir, btnAtras, btnSiguiente });
        Controls.Add(pie);
        AcceptButton = btnSiguiente;
    }

    // ------------------------------------------------------------------
    // Navegación
    // ------------------------------------------------------------------

    private void MostrarPaso(int indice)
    {
        pasoActual = Math.Clamp(indice, 0, pasos.Length - 1);
        lblError.Text = "";

        for (int i = 0; i < pasos.Length; i++)
            pasos[i].Visible = i == pasoActual;

        btnAtras.Visible = pasoActual > 0;
        btnOmitir.Visible = pasoActual < pasos.Length - 1;
        btnSiguiente.Text = pasoActual == pasos.Length - 1 ? "Finalizar" : "Siguiente";

        if (pasoActual == pasos.Length - 1)
            lblResumen.Text = ConstruirResumen();
    }

    private void Avanzar()
    {
        if (pasoActual == 1 && string.IsNullOrWhiteSpace(txtNegocio.Text))
        {
            lblError.Text = "Escribe el nombre de tu negocio para continuar.";
            txtNegocio.Focus();
            return;
        }

        if (pasoActual == 2 && string.IsNullOrWhiteSpace(txtOperador.Text))
        {
            lblError.Text = "Escribe el nombre del operador para continuar.";
            txtOperador.Focus();
            return;
        }

        if (pasoActual < pasos.Length - 1)
        {
            MostrarPaso(pasoActual + 1);
            return;
        }

        Finalizar();
    }

    private void Finalizar()
    {
        try
        {
            var negocio = new ConfiguracionEstablecimiento
            {
                Nombre = txtNegocio.Text.Trim(),
                Identificacion = txtIdentificacion.Text.Trim(),
                TipoEstablecimiento = cbTipo.SelectedItem?.ToString() ?? "Otro"
            };

            AppServices.CrearGuardarConfiguracion().Ejecutar(negocio, actividadId);

            string rol = cbRol.SelectedItem?.ToString() ?? "Recepción";
            string operador = txtOperador.Text.Trim();

            AppServices.CrearGuardarConfiguracionOperador().Ejecutar(
                new ConfiguracionOperador { Rol = rol, Nombre = operador });

            SesionUsuario.RolActual = rol;
            SesionUsuario.OperadorActual = operador;

            GuardarMarca();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Logger.Error("No se pudo guardar la configuración inicial.", ex);
            lblError.Text = "No se pudo guardar: " + ex.Message;
        }
    }

    // ------------------------------------------------------------------
    // Resumen final y dirección para VikingMobile
    // ------------------------------------------------------------------

    private string ConstruirResumen()
    {
        var direcciones = ObtenerDireccionesLocales();

        string conexion = direcciones.Count == 0
            ? "No se detectó una red. Conecta la laptop al Wi-Fi y usa `ipconfig` para ver la dirección."
            : string.Join("\n", direcciones.Select(d => $"    http://{d}:5050"));

        return
            $"Negocio:  {txtNegocio.Text.Trim()}\n" +
            $"Operador:  {txtOperador.Text.Trim()} ({cbRol.SelectedItem})\n\n" +
            "Para conectar VikingMobile, el teléfono debe estar en la misma red Wi-Fi " +
            "y usar esta dirección en Configuración → SERVIDOR VIKING (IP):\n\n" +
            conexion +
            "\n\nSiguiente paso: registra tu primer alumno con el botón «Nuevo».";
    }

    private static List<string> ObtenerDireccionesLocales()
    {
        var resultado = new List<string>();

        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip))
                    continue;

                byte[] b = ip.GetAddressBytes();
                bool privada =
                    b[0] == 10 ||
                    (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                    (b[0] == 192 && b[1] == 168);

                // Se descartan direcciones de enlace local (169.254.x.x) y públicas.
                if (privada)
                    resultado.Add(ip.ToString());
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("No se pudo obtener la dirección IP local.", ex);
        }

        return resultado.Distinct().Take(3).ToList();
    }
}

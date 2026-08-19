using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion;

public class ConfiguracionEstablecimientoForm : Form
{
    private readonly ObtenerConfiguracion obtenerConfiguracion;
    private readonly GuardarConfiguracion guardarConfiguracion;

    private readonly TextBox txtNombre = new();
    private readonly TextBox txtIdentificacion = new();
    private readonly ComboBox cbTipo = new();
    private readonly Button btnGuardar = new();
    private readonly int actividadId;

    public ConfiguracionEstablecimientoForm(int actividadId)
    {
        this.actividadId = actividadId;

        Text = "⚙️ Configuración del establecimiento";
        Size = new Size(520, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorTranslator.FromHtml("#f8f9fa");

        obtenerConfiguracion =
            AppServices.CrearObtenerConfiguracion();

        guardarConfiguracion =
            AppServices.CrearGuardarConfiguracion();

        CrearInterfaz();
        CargarConfiguracion();
    }

    private void CrearInterfaz()
    {
        var titulo = new Label
        {
            Text = "Configuración del establecimiento",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(25, 20)
        };

        var lblNombre = new Label
        {
            Text = "Nombre:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(25, 75)
        };

        txtNombre.Location = new Point(25, 100);
        txtNombre.Size = new Size(450, 28);
        txtNombre.Font = new Font("Segoe UI", 10);

        var lblIdentificacion = new Label
        {
            Text = "Identificación / RIF:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(25, 140)
        };

        txtIdentificacion.Location = new Point(25, 165);
        txtIdentificacion.Size = new Size(450, 28);
        txtIdentificacion.Font = new Font("Segoe UI", 10);

        var lblTipo = new Label
        {
            Text = "Tipo de establecimiento:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(25, 205)
        };

        cbTipo.Location = new Point(25, 230);
        cbTipo.Size = new Size(450, 28);
        cbTipo.Font = new Font("Segoe UI", 10);
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

        btnGuardar.Text = "💾 Guardar configuración";
        btnGuardar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnGuardar.Size = new Size(220, 38);
        btnGuardar.Location = new Point(25, 280);
        btnGuardar.BackColor = ColorTranslator.FromHtml("#0b5394");
        btnGuardar.ForeColor = Color.White;
        btnGuardar.FlatStyle = FlatStyle.Flat;
        btnGuardar.FlatAppearance.BorderSize = 0;
        btnGuardar.Click += BtnGuardar_Click;

        Controls.AddRange(new Control[]
        {
            titulo,
            lblNombre,
            txtNombre,
            lblIdentificacion,
            txtIdentificacion,
            lblTipo,
            cbTipo,
            btnGuardar
        });
    }

    private void CargarConfiguracion()
    {
        var configuracion = obtenerConfiguracion.Ejecutar(actividadId);

        txtNombre.Text = configuracion.Nombre;
        txtIdentificacion.Text = configuracion.Identificacion;

        int indice = cbTipo.Items.IndexOf(
            configuracion.TipoEstablecimiento);

        if (indice >= 0)
            cbTipo.SelectedIndex = indice;
        else
            cbTipo.SelectedIndex = 0;
    }

    private void BtnGuardar_Click(object? sender, EventArgs e)
    {
        string nombre = txtNombre.Text.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            MessageBox.Show(
                "Debes indicar el nombre del establecimiento.",
                "Configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            txtNombre.Focus();
            return;
        }

        string tipo =
            cbTipo.SelectedItem?.ToString()
            ?? "Otro";

        var configuracion = new ConfiguracionEstablecimiento
        {
            Nombre = nombre,
            Identificacion = txtIdentificacion.Text.Trim(),
            TipoEstablecimiento = tipo
        };

        guardarConfiguracion.Ejecutar(configuracion, actividadId);

        MessageBox.Show(
            "La configuración se guardó correctamente.",
            "Configuración",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        Close();
    }
}
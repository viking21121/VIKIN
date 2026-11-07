using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion;

public class ConfiguracionOperadorForm : Form
{
    private readonly ObtenerConfiguracionOperador obtenerConfiguracion;
    private readonly GuardarConfiguracionOperador guardarConfiguracion;

    private readonly ComboBox cbRol = new();
    private readonly TextBox txtNombre = new();
    private readonly Button btnGuardar = new();

    public ConfiguracionOperadorForm()
    {
        obtenerConfiguracion =
            AppServices.CrearObtenerConfiguracionOperador();

        guardarConfiguracion =
            AppServices.CrearGuardarConfiguracionOperador();

        ConfigurarFormulario();
        CargarConfiguracion();
    }

    private void ConfigurarFormulario()
    {
        Text = "Configuración del operador";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(500, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = ColorTranslator.FromHtml("#f8f9fa");

        var lblTitulo = new Label
        {
            Text = "Configuración del operador",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = ColorTranslator.FromHtml("#0b5394"),
            AutoSize = true,
            Location = new Point(30, 25)
        };

        var lblRol = new Label
        {
            Text = "Rol:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(30, 75)
        };

        cbRol.Location = new Point(30, 100);
        cbRol.Size = new Size(420, 30);
        cbRol.Font = new Font("Segoe UI", 10);
        cbRol.DropDownStyle = ComboBoxStyle.DropDownList;
        cbRol.Items.AddRange(new object[]
        {
            "Recepción",
            "Administración",
            "Entrenador"
        });
        cbRol.SelectedIndexChanged += (s, e) => CargarNombreDelRol();

        var lblNombre = new Label
        {
            Text = "Nombre del operador:",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(30, 145)
        };

        txtNombre.Font = new Font("Segoe UI", 11);
        txtNombre.Location = new Point(30, 170);
        txtNombre.Size = new Size(420, 30);

        btnGuardar.Text = "💾 Guardar";
        btnGuardar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnGuardar.Size = new Size(140, 38);
        btnGuardar.Location = new Point(30, 220);
        btnGuardar.BackColor = ColorTranslator.FromHtml("#0b5394");
        btnGuardar.ForeColor = Color.White;
        btnGuardar.FlatStyle = FlatStyle.Flat;
        btnGuardar.FlatAppearance.BorderSize = 0;
        btnGuardar.Click += BtnGuardar_Click;

        Controls.Add(lblTitulo);
        Controls.Add(lblRol);
        Controls.Add(cbRol);
        Controls.Add(lblNombre);
        Controls.Add(txtNombre);
        Controls.Add(btnGuardar);

        AcceptButton = btnGuardar;
    }

    private void CargarConfiguracion()
    {
        string rol = string.IsNullOrWhiteSpace(SesionUsuario.RolActual)
            ? "Recepción"
            : SesionUsuario.RolActual;

        int indice = cbRol.Items.IndexOf(rol);
        cbRol.SelectedIndex = indice >= 0 ? indice : 0;
    }

    private void CargarNombreDelRol()
    {
        if (cbRol.SelectedItem == null)
            return;

        string rol = cbRol.SelectedItem.ToString() ?? "Recepción";

        try
        {
            var configuracion = obtenerConfiguracion.Ejecutar(rol);
            txtNombre.Text = configuracion.Nombre;
        }
        catch
        {
            txtNombre.Clear();
        }
    }

    private void BtnGuardar_Click(object? sender, EventArgs e)
    {
        try
        {
            string rol = cbRol.SelectedItem?.ToString() ?? "Recepción";

            var configuracion = new ConfiguracionOperador
            {
                Rol = rol,
                Nombre = txtNombre.Text
            };

            guardarConfiguracion.Ejecutar(configuracion);

            SesionUsuario.RolActual = configuracion.Rol;
            SesionUsuario.OperadorActual = configuracion.Nombre;

            MessageBox.Show(
                "La configuración del operador se guardó correctamente.",
                "Configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(
                ex.Message,
                "Configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo guardar la configuración.\n\n{ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

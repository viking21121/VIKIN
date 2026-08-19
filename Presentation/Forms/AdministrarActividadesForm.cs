using AsistenciaNatacion.Domain;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion
{
    public class AdministrarActividadesForm : Form
    {
        private readonly ListBox listaActividades = new();
        private readonly TextBox txtNombre = new();
        private readonly Button btnAgregar = new();
        private readonly Button btnDesactivar = new();
        private readonly Button btnCerrar = new();

        public AdministrarActividadesForm()
        {
            Text = "Administrar actividades";
            Size = new Size(520, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitulo = new Label
            {
                Text = "Administrar actividades",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(25, 20)
            };

            var lblLista = new Label
            {
                Text = "Actividades activas",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(25, 65)
            };

            listaActividades.Location = new Point(25, 90);
            listaActividades.Size = new Size(440, 150);
            listaActividades.DisplayMember = "Nombre";

            var lblNueva = new Label
            {
                Text = "Nueva actividad",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(25, 260)
            };

            txtNombre.Location = new Point(25, 285);
            txtNombre.Size = new Size(280, 27);

            btnAgregar.Text = "Agregar";
            btnAgregar.Location = new Point(320, 283);
            btnAgregar.Size = new Size(145, 30);
            btnAgregar.Click += BtnAgregar_Click;

            btnDesactivar.Text = "Desactivar seleccionada";
            btnDesactivar.Location = new Point(25, 335);
            btnDesactivar.Size = new Size(190, 30);
            btnDesactivar.Click += BtnDesactivar_Click;

            btnCerrar.Text = "Cerrar";
            btnCerrar.Location = new Point(365, 335);
            btnCerrar.Size = new Size(100, 30);
            btnCerrar.Click += (s, e) => Close();

            Controls.Add(lblTitulo);
            Controls.Add(lblLista);
            Controls.Add(listaActividades);
            Controls.Add(lblNueva);
            Controls.Add(txtNombre);
            Controls.Add(btnAgregar);
            Controls.Add(btnDesactivar);
            Controls.Add(btnCerrar);

            CargarActividades();
        }

        private void CargarActividades()
        {
            var casoUso = AppServices.CrearObtenerActividades();
            var actividades = casoUso.Ejecutar();

            listaActividades.DataSource = null;
            listaActividades.DataSource = actividades;
            listaActividades.DisplayMember = "Nombre";
        }

        private void BtnAgregar_Click(object? sender, EventArgs e)
        {
            string nombre = txtNombre.Text.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show(
                    "Escribe el nombre de la actividad.",
                    "Actividad",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtNombre.Focus();
                return;
            }

            try
            {
                var casoUso = AppServices.CrearCrearActividad();
                casoUso.Ejecutar(nombre);

                txtNombre.Clear();
                CargarActividades();

                MessageBox.Show(
                    $"La actividad '{nombre}' fue agregada correctamente.",
                    "Actividad creada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo crear la actividad.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void BtnDesactivar_Click(object? sender, EventArgs e)
        {
            if (listaActividades.SelectedItem is not Actividad actividad)
            {
                MessageBox.Show(
                    "Selecciona una actividad.",
                    "Actividad",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var confirmar = MessageBox.Show(
                $"¿Seguro que deseas desactivar '{actividad.Nombre}'?\n\n" +
                "La actividad dejará de aparecer en el selector, pero sus datos históricos se conservarán.",
                "Desactivar actividad",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmar != DialogResult.Yes)
                return;

            try
            {
                var casoUso = AppServices.CrearDesactivarActividad();
                casoUso.Ejecutar(actividad.Id);

                CargarActividades();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo desactivar la actividad.\n\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
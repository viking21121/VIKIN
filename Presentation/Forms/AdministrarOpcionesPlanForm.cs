using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Domain;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AsistenciaNatacion.Presentation.Forms
{
    public class AdministrarOpcionesPlanForm : Form
    {
        private readonly int actividadId;
        private readonly ObtenerOpcionesPlanActividad obtener;
        private readonly CrearOpcionPlanActividad crear;
        private readonly ActualizarOpcionPlanActividad actualizar;
        private readonly DesactivarOpcionPlanActividad desactivar;
        private readonly ComboBox cbTipo = new();
        private readonly ListBox lst = new();
        private readonly TextBox txtNombre = new();
        private readonly NumericUpDown nudValor = new();
        private readonly CheckBox chkValor = new();
        private readonly Label lblAyuda = new();
        private int? opcionId;

        public AdministrarOpcionesPlanForm(int actividadId)
        {
            this.actividadId = actividadId;
            obtener = AppServices.CrearObtenerOpcionesPlanActividad();
            crear = AppServices.CrearCrearOpcionPlanActividad();
            actualizar = AppServices.CrearActualizarOpcionPlanActividad();
            desactivar = AppServices.CrearDesactivarOpcionPlanActividad();

            Text = "Configurar catálogos de planes";
            ClientSize = new Size(760, 535);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ColorTranslator.FromHtml("#F5F7FA");
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            CrearInterfaz();
            Cargar();
            Nuevo();
        }

        private void CrearInterfaz()
        {
            var header = new Panel { Location = new Point(0, 0), Size = new Size(ClientSize.Width, 82), BackColor = ColorTranslator.FromHtml("#0B5394") };
            header.Controls.Add(new Label
            {
                Text = "Catálogos de planes",
                Font = new Font("Segoe UI", 19, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(22, 12)
            });
            header.Controls.Add(new Label
            {
                Text = "Define las opciones que aparecerán al crear un plan.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 49)
            });
            Controls.Add(header);

            var cardLista = new Panel { Location = new Point(20, 100), Size = new Size(340, 410), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            cardLista.Controls.Add(new Label { Text = "Opciones actuales", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#17365D"), AutoSize = true, Location = new Point(18, 16) });
            cbTipo.SetBounds(18, 48, 300, 31);
            cbTipo.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTipo.Font = new Font("Segoe UI", 9.5f);
            cbTipo.Items.AddRange(new object[] { "Nivel", "Frecuencia", "Clases" });
            cbTipo.SelectedIndex = 0;
            cbTipo.SelectedIndexChanged += (s, e) => Cargar();
            cardLista.Controls.Add(cbTipo);
            lst.SetBounds(18, 93, 300, 270);
            lst.Font = new Font("Segoe UI", 9.5f);
            lst.SelectedIndexChanged += (s, e) => Seleccionar();
            cardLista.Controls.Add(lst);
            cardLista.Controls.Add(new Label
            {
                Text = "Las opciones desactivadas dejan de aparecer en nuevos planes, pero no alteran a los alumnos existentes.",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.Gray,
                AutoSize = false,
                Size = new Size(300, 42),
                Location = new Point(18, 354)
            });
            Controls.Add(cardLista);

            var cardEdit = new Panel { Location = new Point(380, 100), Size = new Size(360, 410), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            cardEdit.Controls.Add(new Label { Text = "Editar opción", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#17365D"), AutoSize = true, Location = new Point(18, 16) });
            cardEdit.Controls.Add(new Label { Text = "Nombre que verá el encargado", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = ColorTranslator.FromHtml("#425466"), AutoSize = true, Location = new Point(18, 55) });
            txtNombre.SetBounds(18, 78, 320, 31);
            txtNombre.Font = new Font("Segoe UI", 9.5f);
            cardEdit.Controls.Add(txtNombre);

            chkValor.Text = "Usar valor numérico para cálculos";
            chkValor.AutoSize = true;
            chkValor.Location = new Point(18, 125);
            chkValor.Font = new Font("Segoe UI", 8.5f);
            chkValor.CheckedChanged += (s, e) => nudValor.Enabled = chkValor.Checked;
            cardEdit.Controls.Add(chkValor);

            nudValor.SetBounds(18, 155, 120, 31);
            nudValor.Minimum = 1;
            nudValor.Maximum = 1000;
            nudValor.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            nudValor.Enabled = false;
            cardEdit.Controls.Add(nudValor);

            lblAyuda.Text = "";
            lblAyuda.Location = new Point(18, 198);
            lblAyuda.Size = new Size(320, 55);
            lblAyuda.Font = new Font("Segoe UI", 8.5f);
            lblAyuda.ForeColor = Color.Gray;
            cardEdit.Controls.Add(lblAyuda);

            var btnNuevo = CrearBoton("Nuevo", 18, 278, 95, false); btnNuevo.Click += (s, e) => Nuevo(); cardEdit.Controls.Add(btnNuevo);
            var btnGuardar = CrearBoton("Guardar", 123, 278, 100, true); btnGuardar.Click += (s, e) => Guardar(); cardEdit.Controls.Add(btnGuardar);
            var btnDesactivar = CrearBoton("Desactivar", 234, 278, 105, false); btnDesactivar.Click += (s, e) => Desactivar(); cardEdit.Controls.Add(btnDesactivar);
            Controls.Add(cardEdit);
        }

        private static Button CrearBoton(string texto, int x, int y, int w, bool principal)
        {
            var b = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(w, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = principal ? ColorTranslator.FromHtml("#0B5394") : ColorTranslator.FromHtml("#EEF3F8"),
                ForeColor = principal ? Color.White : ColorTranslator.FromHtml("#17365D")
            };
            b.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#C8D7E6");
            return b;
        }

        private string Tipo => cbTipo.SelectedItem?.ToString() ?? "Nivel";

        private void Cargar()
        {
            lst.DataSource = null;
            lst.DataSource = obtener.Ejecutar(actividadId, Tipo);
            lblAyuda.Text = Tipo switch
            {
                "Frecuencia" => "Ejemplo: 2x por semana. El valor numérico se utiliza para determinar la frecuencia semanal.",
                "Clases" => "Ejemplo: 12 clases. El valor numérico define cuántas asistencias incluye el plan.",
                _ => "Ejemplo: Inicial, Intermedio, Avanzado. Es una categoría descriptiva del servicio."
            };
        }

        private void Seleccionar()
        {
            if (lst.SelectedItem is not OpcionPlanActividad o)
                return;

            opcionId = o.Id;
            txtNombre.Text = o.Nombre;
            chkValor.Checked = o.ValorEntero.HasValue;
            if (o.ValorEntero.HasValue)
                nudValor.Value = o.ValorEntero.Value;
        }

        private void Nuevo()
        {
            opcionId = null;
            txtNombre.Clear();
            chkValor.Checked = Tipo != "Nivel";
            nudValor.Value = 1;
            if (lst.Items.Count > 0)
                lst.ClearSelected();
            txtNombre.Focus();
        }

        private void Guardar()
        {
            try
            {
                string nombre = txtNombre.Text.Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    MessageBox.Show("Escribe el nombre de la opción.", "Catálogo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int? valor = chkValor.Checked ? (int)nudValor.Value : null;
                if (opcionId.HasValue)
                    actualizar.Ejecutar(opcionId.Value, nombre, valor);
                else
                    crear.Ejecutar(actividadId, Tipo, nombre, valor);

                Cargar();
                Nuevo();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Catálogo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Desactivar()
        {
            if (!opcionId.HasValue)
            {
                MessageBox.Show("Selecciona una opción.", "Catálogo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                "¿Desactivar la opción seleccionada?\n\nLos planes y alumnos existentes conservarán sus datos.",
                "Confirmar",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            desactivar.Ejecutar(opcionId.Value);
            Cargar();
            Nuevo();
        }
    }
}

using AsistenciaNatacion.Application.UseCases;
using AsistenciaNatacion.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AsistenciaNatacion.Presentation.Forms
{
    public class AdministrarPlanesForm : Form
    {
        private readonly int actividadId;
        private readonly ObtenerPlanesPorActividad obtenerPlanes;
        private readonly CrearPlanActividad crearPlan;
        private readonly ActualizarPlanActividad actualizarPlan;
        private readonly DesactivarPlanActividad desactivarPlan;
        private readonly ObtenerOpcionesPlanActividad obtenerOpciones;
        private readonly PrepararCatalogoPlanActividad prepararCatalogo;

        private readonly TextBox txtNombre = new();
        private readonly ComboBox cbNivel = new();
        private readonly ComboBox cbFrecuencia = new();
        private readonly ComboBox cbClases = new();
        private readonly RadioButton rbPorClases = new();
        private readonly RadioButton rbMembresia = new();
        private readonly NumericUpDown nudPrecio = new();
        private readonly DataGridView dgvPlanes = new();
        private readonly Label lblResumen = new();
        private readonly Label lblAyuda = new();
        private readonly Label lblTituloModo = new();

        private int? planSeleccionadoId;
        private List<PlanActividad> planes = new();

        public AdministrarPlanesForm(int actividadId)
        {
            this.actividadId = actividadId;
            obtenerPlanes = AppServices.CrearObtenerPlanesPorActividad();
            crearPlan = AppServices.CrearCrearPlanActividad();
            actualizarPlan = AppServices.CrearActualizarPlanActividad();
            desactivarPlan = AppServices.CrearDesactivarPlanActividad();
            obtenerOpciones = AppServices.CrearObtenerOpcionesPlanActividad();
            prepararCatalogo = AppServices.CrearPrepararCatalogoPlanActividad();

            Text = "Administrar planes";
            ClientSize = new Size(1080, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ColorTranslator.FromHtml("#F5F7FA");
            KeyPreview = true;
            KeyDown += Form_KeyDown;

            CrearInterfaz();
            prepararCatalogo.Ejecutar(actividadId);
            CargarOpciones();
            CargarPlanes();
            NuevoPlan();
        }

        private void CrearInterfaz()
        {
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 86),
                BackColor = ColorTranslator.FromHtml("#0B5394")
            };

            header.Controls.Add(new Label
            {
                Text = "Administrar planes",
                Font = new Font("Segoe UI", 21, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(24, 14)
            });

            header.Controls.Add(new Label
            {
                Text = "Crea ofertas claras para tus clientes y controla exactamente qué incluye cada plan.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(27, 57)
            });
            Controls.Add(header);

            var editor = CrearTarjetaEditor();
            Controls.Add(editor);

            var lista = CrearTarjetaLista();
            Controls.Add(lista);
        }

        private Panel CrearTarjetaEditor()
        {
            var card = new Panel
            {
                Location = new Point(20, 104),
                Size = new Size(440, 660),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            card.Controls.Add(new Label
            {
                Text = "Información del plan",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#17365D"),
                AutoSize = true,
                Location = new Point(20, 16)
            });

            card.Controls.Add(new Label
            {
                Text = "El cliente verá este nombre al registrarse.",
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(21, 42)
            });

            AgregarLabel(card, "Nombre comercial", 20, 77);
            txtNombre.SetBounds(20, 101, 390, 32);
            txtNombre.Font = new Font("Segoe UI", 10);
            card.Controls.Add(txtNombre);

            AgregarLabel(card, "Modalidad", 20, 145);
            lblTituloModo.Text = "¿Cómo se consume este plan?";
            lblTituloModo.Font = new Font("Segoe UI", 8.5f);
            lblTituloModo.ForeColor = Color.Gray;
            lblTituloModo.AutoSize = true;
            lblTituloModo.Location = new Point(20, 167);
            card.Controls.Add(lblTituloModo);

            rbPorClases.Text = "Por clases";
            rbPorClases.SetBounds(20, 190, 150, 30);
            rbPorClases.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            rbPorClases.CheckedChanged += (s, e) => ActualizarModo();
            card.Controls.Add(rbPorClases);

            rbMembresia.Text = "Membresía 30 días";
            rbMembresia.SetBounds(190, 190, 185, 30);
            rbMembresia.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            rbMembresia.CheckedChanged += (s, e) => ActualizarModo();
            card.Controls.Add(rbMembresia);

            AgregarLabel(card, "Nivel / categoría", 20, 231);
            ConfigurarCombo(cbNivel, 20, 255, 390);
            card.Controls.Add(cbNivel);

            AgregarLabel(card, "Frecuencia", 20, 302);
            ConfigurarCombo(cbFrecuencia, 20, 326, 185);
            card.Controls.Add(cbFrecuencia);

            AgregarLabel(card, "Clases incluidas", 220, 302);
            ConfigurarCombo(cbClases, 220, 326, 190);
            card.Controls.Add(cbClases);

            AgregarLabel(card, "Precio del plan", 20, 373);
            nudPrecio.SetBounds(20, 397, 150, 34);
            nudPrecio.Minimum = 0;
            nudPrecio.Maximum = 10000000;
            nudPrecio.DecimalPlaces = 2;
            nudPrecio.ThousandsSeparator = true;
            nudPrecio.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            card.Controls.Add(nudPrecio);

            lblAyuda.Text = "";
            lblAyuda.Font = new Font("Segoe UI", 8.5f);
            lblAyuda.ForeColor = ColorTranslator.FromHtml("#5B6770");
            lblAyuda.AutoSize = false;
            lblAyuda.SetBounds(190, 389, 220, 52);
            card.Controls.Add(lblAyuda);

            var btnNuevo = Boton("Nuevo", 20, 462, 110, false);
            btnNuevo.Click += (s, e) => NuevoPlan();
            card.Controls.Add(btnNuevo);

            var btnGuardar = Boton("Guardar plan", 140, 462, 130, true);
            btnGuardar.Click += (s, e) => GuardarPlan();
            card.Controls.Add(btnGuardar);

            var btnDesactivar = Boton("Desactivar", 280, 462, 130, false);
            btnDesactivar.Click += (s, e) => DesactivarPlan();
            card.Controls.Add(btnDesactivar);

            var btnDuplicar = new Button
            {
                Text = "Duplicar seleccionado",
                Location = new Point(20, 525),
                Size = new Size(190, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = ColorTranslator.FromHtml("#0B5394")
            };
            btnDuplicar.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#C8D7E6");
            btnDuplicar.Click += (s, e) => DuplicarPlan();
            card.Controls.Add(btnDuplicar);

            var btnOpciones = new Button
            {
                Text = "⚙ Configurar catálogos",
                Location = new Point(220, 525),
                Size = new Size(190, 35),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#EEF3F8"),
                ForeColor = ColorTranslator.FromHtml("#0B5394")
            };
            btnOpciones.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#C8D7E6");
            btnOpciones.Click += (s, e) =>
            {
                using var f = new AdministrarOpcionesPlanForm(actividadId);
                f.ShowDialog(this);
                prepararCatalogo.Ejecutar(actividadId);
                CargarOpciones();
                CargarPlanes();
            };
            card.Controls.Add(btnOpciones);

            return card;
        }

        private Panel CrearTarjetaLista()
        {
            var card = new Panel
            {
                Location = new Point(480, 104),
                Size = new Size(580, 560),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            card.Controls.Add(new Label
            {
                Text = "Planes disponibles",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#17365D"),
                AutoSize = true,
                Location = new Point(20, 16)
            });

            lblResumen.Location = new Point(22, 48);
            lblResumen.Size = new Size(530, 25);
            lblResumen.Font = new Font("Segoe UI", 8.5f);
            lblResumen.ForeColor = Color.Gray;
            card.Controls.Add(lblResumen);

            dgvPlanes.SetBounds(20, 82, 538, 408);
            dgvPlanes.AllowUserToAddRows = false;
            dgvPlanes.AllowUserToDeleteRows = false;
            dgvPlanes.ReadOnly = true;
            dgvPlanes.MultiSelect = false;
            dgvPlanes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPlanes.RowHeadersVisible = false;
            dgvPlanes.AutoGenerateColumns = false;
            dgvPlanes.BackgroundColor = Color.White;
            dgvPlanes.BorderStyle = BorderStyle.None;
            dgvPlanes.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvPlanes.GridColor = ColorTranslator.FromHtml("#E5EAF0");
            dgvPlanes.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#EAF2F8");
            dgvPlanes.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#17365D");
            dgvPlanes.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvPlanes.EnableHeadersVisualStyles = false;
            dgvPlanes.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f);
            dgvPlanes.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#DCEAF7");
            dgvPlanes.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#17365D");
            dgvPlanes.RowTemplate.Height = 34;
            dgvPlanes.DoubleBuffered(true);
            dgvPlanes.CellFormatting += DgvPlanes_CellFormatting;
            dgvPlanes.CellDoubleClick += (s, e) => SeleccionarPlan();
            dgvPlanes.SelectionChanged += (s, e) => SeleccionarPlan();

            dgvPlanes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Plan", DataPropertyName = "Nombre", Width = 145 });
            dgvPlanes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nivel", DataPropertyName = "NivelTexto", Width = 90 });
            dgvPlanes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Frecuencia", DataPropertyName = "FrecuenciaTexto", Width = 90 });
            dgvPlanes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Modalidad", DataPropertyName = "ModalidadTexto", Width = 110 });
            dgvPlanes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Clases", DataPropertyName = "ClasesTexto", Width = 85 });
            dgvPlanes.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Precio", DataPropertyName = "PrecioTexto", Width = 75 });
            card.Controls.Add(dgvPlanes);

            var ayuda = new Label
            {
                Text = "Tip: selecciona un plan para editarlo. Los cambios aquí no modifican ningún alumno existente.",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = ColorTranslator.FromHtml("#66727D"),
                AutoSize = true,
                Location = new Point(20, 510)
            };
            card.Controls.Add(ayuda);

            return card;
        }

        private static void AgregarLabel(Control parent, string texto, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = texto,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#425466"),
                AutoSize = true,
                Location = new Point(x, y)
            });
        }

        private static void ConfigurarCombo(ComboBox combo, int x, int y, int width)
        {
            combo.SetBounds(x, y, width, 30);
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Font = new Font("Segoe UI", 9.5f);
            combo.BackColor = Color.White;
        }

        private static Button Boton(string texto, int x, int y, int width, bool principal)
        {
            var boton = new Button
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(width, 34),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = principal ? ColorTranslator.FromHtml("#0B5394") : ColorTranslator.FromHtml("#EEF3F8"),
                ForeColor = principal ? Color.White : ColorTranslator.FromHtml("#17365D")
            };
            boton.FlatAppearance.BorderColor = principal
                ? ColorTranslator.FromHtml("#0B5394")
                : ColorTranslator.FromHtml("#C8D7E6");
            return boton;
        }

        private void CargarOpciones()
        {
            CargarCombo(cbNivel, obtenerOpciones.Ejecutar(actividadId, "Nivel"), "— Sin nivel —");
            CargarCombo(cbFrecuencia, obtenerOpciones.Ejecutar(actividadId, "Frecuencia"), "— Sin frecuencia —");
            CargarCombo(cbClases, obtenerOpciones.Ejecutar(actividadId, "Clases"), "— Sin clases —");
        }

        private static void CargarCombo(ComboBox combo, List<OpcionPlanActividad> opciones, string textoNinguno)
        {
            combo.DataSource = null;
            combo.Items.Clear();
            combo.Items.Add(new OpcionCombo(null, textoNinguno));
            foreach (var opcion in opciones)
                combo.Items.Add(opcion);
            combo.SelectedIndex = 0;
        }

        private void CargarPlanes()
        {
            planes = obtenerPlanes.Ejecutar(actividadId);
            dgvPlanes.DataSource = null;
            dgvPlanes.DataSource = planes;
            lblResumen.Text = planes.Count == 0
                ? "Todavía no tienes planes. Crea el primero en el panel de la izquierda."
                : $"{planes.Count} plan{(planes.Count == 1 ? " activo" : "es activos")} · Haz doble clic para editar.";
        }

        private int? GetComboId(ComboBox cb)
        {
            return cb.SelectedItem switch
            {
                OpcionCombo o => o.Id,
                OpcionPlanActividad o => o.Id,
                _ => null
            };
        }

        private void SeleccionarPlan()
        {
            if (dgvPlanes.SelectedRows.Count == 0)
                return;

            if (dgvPlanes.SelectedRows[0].DataBoundItem is not PlanActividad p)
                return;

            planSeleccionadoId = p.Id;
            txtNombre.Text = p.Nombre;
            SeleccionarOpcion(cbNivel, p.NivelOpcionId, p.Nivel);
            SeleccionarOpcion(cbFrecuencia, p.FrecuenciaOpcionId, p.FrecuenciaSemanal);
            SeleccionarOpcion(cbClases, p.ClasesOpcionId, p.ClasesMes);

            rbPorClases.Checked = p.EsPorClases;
            rbMembresia.Checked = p.EsPorTiempo;
            nudPrecio.Value = Math.Min(nudPrecio.Maximum, Math.Max(nudPrecio.Minimum, p.Precio));
        }

        private static void SeleccionarOpcion(ComboBox combo, int? id, string? legacyNombre)
        {
            if (id.HasValue)
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i] is OpcionPlanActividad opcion && opcion.Id == id.Value)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(legacyNombre))
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i] is OpcionPlanActividad opcion &&
                        string.Equals(opcion.Nombre, legacyNombre, StringComparison.OrdinalIgnoreCase))
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private static void SeleccionarOpcion(ComboBox combo, int? id, int? valor)
        {
            if (id.HasValue)
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i] is OpcionPlanActividad opcion && opcion.Id == id.Value)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (valor.HasValue)
            {
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    if (combo.Items[i] is OpcionPlanActividad opcion && opcion.ValorEntero == valor.Value)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
            }

            if (combo.Items.Count > 0)
                combo.SelectedIndex = 0;
        }

        private void ActualizarModo()
        {
            bool porClases = rbPorClases.Checked;
            cbClases.Enabled = porClases;

            if (!porClases && cbClases.Items.Count > 0)
                cbClases.SelectedIndex = 0;

            lblAyuda.Text = porClases
                ? "Se descuentan las asistencias de la cantidad de clases configurada para el período."
                : "La vigencia es de 30 días. No depende del número de asistencias realizadas.";
        }

        private void NuevoPlan()
        {
            planSeleccionadoId = null;
            txtNombre.Clear();
            cbNivel.SelectedIndex = 0;
            cbFrecuencia.SelectedIndex = 0;
            cbClases.SelectedIndex = cbClases.Items.Count > 3 ? Math.Min(1, cbClases.Items.Count - 1) : 0;
            rbPorClases.Checked = true;
            rbMembresia.Checked = false;
            nudPrecio.Value = 0;
            dgvPlanes.ClearSelection();
            ActualizarModo();
            txtNombre.Focus();
        }

        private void DuplicarPlan()
        {
            if (dgvPlanes.SelectedRows.Count == 0 || dgvPlanes.SelectedRows[0].DataBoundItem is not PlanActividad p)
            {
                MessageBox.Show("Selecciona un plan para duplicarlo.", "Planes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            planSeleccionadoId = null;
            txtNombre.Text = p.Nombre + " (copia)";
            SeleccionarOpcion(cbNivel, p.NivelOpcionId, p.Nivel);
            SeleccionarOpcion(cbFrecuencia, p.FrecuenciaOpcionId, p.FrecuenciaSemanal);
            SeleccionarOpcion(cbClases, p.ClasesOpcionId, p.ClasesMes);
            rbPorClases.Checked = p.EsPorClases;
            rbMembresia.Checked = p.EsPorTiempo;
            nudPrecio.Value = Math.Min(nudPrecio.Maximum, Math.Max(nudPrecio.Minimum, p.Precio));
            ActualizarModo();
            txtNombre.Focus();
            txtNombre.SelectAll();
        }

        private void GuardarPlan()
        {
            string nombre = txtNombre.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("Escribe un nombre comercial para el plan.", "Plan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNombre.Focus();
                return;
            }

            int? nivelId = GetComboId(cbNivel);
            int? frecuenciaId = GetComboId(cbFrecuencia);
            int? clasesId = rbPorClases.Checked ? GetComboId(cbClases) : null;

            if (rbPorClases.Checked && !clasesId.HasValue)
            {
                MessageBox.Show("Para un plan por clases debes seleccionar cuántas clases incluye.", "Plan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbClases.Focus();
                return;
            }

            if (nudPrecio.Value <= 0)
            {
                MessageBox.Show("Indica un precio mayor que cero.", "Plan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nudPrecio.Focus();
                return;
            }

            try
            {
                if (planSeleccionadoId.HasValue)
                {
                    actualizarPlan.Ejecutar(planSeleccionadoId.Value, nombre, nivelId, frecuenciaId, clasesId, nudPrecio.Value);
                    MessageBox.Show("El plan fue actualizado correctamente. Esto no cambia el plan de ningún alumno existente.", "Plan actualizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    crearPlan.Ejecutar(actividadId, nombre, nivelId, frecuenciaId, clasesId, nudPrecio.Value);
                    MessageBox.Show("El plan fue creado correctamente y ya está disponible para nuevos alumnos.", "Plan creado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                CargarPlanes();
                NuevoPlan();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Planes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void DesactivarPlan()
        {
            if (!planSeleccionadoId.HasValue)
            {
                MessageBox.Show("Selecciona un plan de la lista.", "Planes", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var plan = planes.FirstOrDefault(p => p.Id == planSeleccionadoId.Value);
            string nombre = plan?.Nombre ?? "seleccionado";

            if (MessageBox.Show(
                $"¿Desactivar '{nombre}'?\n\nLos alumnos que ya lo tengan no serán modificados. El plan solo dejará de aparecer para nuevos registros.",
                "Desactivar plan",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            desactivarPlan.Ejecutar(planSeleccionadoId.Value);
            CargarPlanes();
            NuevoPlan();
        }

        private void DgvPlanes_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvPlanes.Rows.Count)
                return;

            if (dgvPlanes.Rows[e.RowIndex].DataBoundItem is not PlanActividad p)
                return;

            if (e.ColumnIndex == 3)
            {
                e.CellStyle.ForeColor = p.EsPorClases
                    ? ColorTranslator.FromHtml("#176B87")
                    : ColorTranslator.FromHtml("#2E7D32");
                e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Close();
            }
        }

        private sealed class OpcionCombo
        {
            public int? Id { get; }
            public string Nombre { get; }

            public OpcionCombo(int? id, string nombre)
            {
                Id = id;
                Nombre = nombre;
            }

            public override string ToString() => Nombre;
        }
    }

    internal static class DataGridViewExtensions
    {
        public static void DoubleBuffered(this DataGridView grid, bool value)
        {
            var property = typeof(DataGridView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            property?.SetValue(grid, value, null);
        }
    }
}

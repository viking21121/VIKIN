using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AsistenciaNatacion.Infrastructure.Carnet;
using AsistenciaNatacion.Infrastructure.Logging;
using AsistenciaNatacion.Infrastructure.Pdf;

namespace AsistenciaNatacion.Presentation.Forms;

/// <summary>Datos del carnet impreso: contacto de emergencia, datos médicos básicos y nivel.</summary>
public sealed class DatosCarnetForm : Form
{
    private readonly Alumno alumno;
    private readonly TextBox txtNivel = new();
    private readonly TextBox txtContacto = new();
    private readonly TextBox txtTelefono = new();
    private readonly ComboBox cbSangre = new();
    private readonly TextBox txtAlertas = new();

    public DatosCarnetForm(Alumno alumno)
    {
        this.alumno = alumno;

        Text = $"Carnet · {alumno.Nombre}";
        ClientSize = new Size(460, 445);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5f);

        int y = 16;
        AgregarCampo("Nivel / categoría (ej. Infantil B, Máster):", txtNivel, ref y);
        AgregarCampo("Contacto de emergencia (representante):", txtContacto, ref y);
        AgregarCampo("Teléfono del contacto:", txtTelefono, ref y);

        Controls.Add(new Label { Text = "Tipo de sangre:", AutoSize = true, Location = new Point(16, y) });
        cbSangre.DropDownStyle = ComboBoxStyle.DropDownList;
        cbSangre.Items.AddRange(new object[] { "", "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" });
        cbSangre.Location = new Point(16, y + 22);
        cbSangre.Width = 120;
        Controls.Add(cbSangre);
        y += 56;

        Controls.Add(new Label { Text = "Alertas médicas (alergias graves, condiciones clave):", AutoSize = true, Location = new Point(16, y) });
        txtAlertas.Multiline = true;
        txtAlertas.Location = new Point(16, y + 22);
        txtAlertas.Size = new Size(428, 52);
        Controls.Add(txtAlertas);
        y += 84;

        Controls.Add(new Label
        {
            Text = "Lo médico se imprime en el reverso del carnet solo si lo llenas. Déjalo vacío si prefieres no mostrarlo.",
            ForeColor = Color.DimGray,
            Location = new Point(16, y),
            Size = new Size(428, 34)
        });

        var btnGuardar = new Button { Text = "Guardar", Width = 100, Height = 32, Location = new Point(16, 352) };
        var btnImprimir = new Button { Text = "Guardar e imprimir carnet", Width = 190, Height = 32, Location = new Point(124, 352) };
        var btnCerrar = new Button { Text = "Cerrar", Width = 90, Height = 32, Location = new Point(354, 352) };
        var btnWhatsApp = new Button { Text = "Enviar PDF por WhatsApp", Width = 428, Height = 32, Location = new Point(16, 396) };
        btnWhatsApp.Click += (s, e) => { if (Guardar()) EnviarPorWhatsApp(); };
        Controls.Add(btnWhatsApp);

        btnGuardar.Click += (s, e) => { if (Guardar()) MessageBox.Show(this, "Datos guardados.", "Carnet"); };
        btnImprimir.Click += (s, e) => { if (Guardar()) Imprimir(); };
        btnCerrar.Click += (s, e) => Close();
        Controls.AddRange(new Control[] { btnGuardar, btnImprimir, btnCerrar });

        Cargar();
    }

    private void AgregarCampo(string etiqueta, TextBox caja, ref int y)
    {
        Controls.Add(new Label { Text = etiqueta, AutoSize = true, Location = new Point(16, y) });
        caja.Location = new Point(16, y + 22);
        caja.Width = 428;
        Controls.Add(caja);
        y += 56;
    }

    private void Cargar()
    {
        try
        {
            var d = DatosCarnetRepositorio.Obtener(alumno.Id);
            txtNivel.Text = d.NivelCategoria;
            txtContacto.Text = d.ContactoNombre;
            txtTelefono.Text = d.ContactoTelefono;
            txtAlertas.Text = d.AlertasMedicas;
            int i = cbSangre.Items.IndexOf(d.TipoSangre);
            cbSangre.SelectedIndex = i >= 0 ? i : 0;
        }
        catch (Exception ex)
        {
            Logger.Error("Cargar datos del carnet", ex);
            cbSangre.SelectedIndex = 0;
        }
    }

    private bool Guardar()
    {
        try
        {
            DatosCarnetRepositorio.Guardar(alumno.Id, new DatosCarnet
            {
                NivelCategoria = txtNivel.Text,
                ContactoNombre = txtContacto.Text,
                ContactoTelefono = txtTelefono.Text,
                TipoSangre = cbSangre.SelectedItem?.ToString() ?? "",
                AlertasMedicas = txtAlertas.Text
            });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Guardar datos del carnet", ex);
            MessageBox.Show(this, "No se pudieron guardar los datos: " + ex.Message, "Carnet",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void EnviarPorWhatsApp()
    {
        try
        {
            string? contacto = WhatsAppCarnet.NormalizarTelefono(txtTelefono.Text);
            string? propio = WhatsAppCarnet.NormalizarTelefono(alumno.Telefono);
            string? destino;

            if (contacto != null && propio != null && contacto != propio)
            {
                var r = MessageBox.Show(this,
                    "¿A quién se lo envías?\n\nSí = representante (" + txtTelefono.Text.Trim() + ")\nNo = alumno (" + alumno.Telefono + ")",
                    "WhatsApp", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r == DialogResult.Cancel) return;
                destino = r == DialogResult.Yes ? contacto : propio;
            }
            else
            {
                destino = contacto ?? propio;
            }

            if (destino == null)
            {
                MessageBox.Show(this,
                    "No hay un teléfono válido. Escribe el teléfono del contacto (o el del alumno) y vuelve a intentar.",
                    "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string html = GeneradorCarnet.Generar(alumno);
            string? pdf = HtmlAPdf.Convertir(html);
            if (pdf == null)
            {
                MessageBox.Show(this,
                    "No se pudo crear el PDF (hace falta Microsoft Edge o Chrome). Prueba con «Guardar e imprimir carnet».",
                    "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var archivos = new System.Collections.Specialized.StringCollection();
            archivos.Add(pdf);
            Clipboard.SetFileDropList(archivos);

            WhatsAppCarnet.AbrirChat(destino,
                $"Hola, te envío el carnet de {alumno.Nombre} ({alumno.Codigo}).");

            MessageBox.Show(this,
                "El PDF ya está copiado.\n\nEn el chat de WhatsApp que se abrió, pulsa Ctrl+V para pegarlo y envía.",
                "WhatsApp", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("Enviar carnet por WhatsApp", ex);
            MessageBox.Show(this, "No se pudo preparar el envío: " + ex.Message, "WhatsApp",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Imprimir()
    {
        try
        {
            string html = GeneradorCarnet.Generar(alumno);
            string abrir = HtmlAPdf.Convertir(html) ?? html;
            Process.Start(new ProcessStartInfo(abrir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error("Imprimir carnet", ex);
            MessageBox.Show(this, "No se pudo generar el carnet: " + ex.Message, "Carnet",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

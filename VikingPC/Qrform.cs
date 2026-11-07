using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using QRCoder;

namespace AsistenciaNatacion
{
    /// <summary>
    /// Genera un código QR que contiene el "codigo" del alumno (ej. "A007").
    /// Cualquier app lectora de QR del celular, al escanearlo, mostrará ese texto:
    /// eso es lo que luego se escribe/pega en la ventana de Asistencia rápida.
    /// </summary>
    public class QrForm : Form
    {
        private readonly Bitmap qrBitmap;

        public QrForm(string nombreAlumno, string codigo)
        {
            Text = $"Código QR - {nombreAlumno}";
            Size = new Size(340, 460);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            // --- Generar el QR ---
            // ECCLevel.Q = nivel de corrección de errores (permite que el QR se lea
            // aunque esté un poco sucio o doblado). "20" es el tamaño en píxeles por módulo.
            using var generador = new QRCodeGenerator();
            var datosQr = generador.CreateQrCode(codigo, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(datosQr);
            qrBitmap = qrCode.GetGraphic(12);

            var lblNombre = new Label
            {
                Text = nombreAlumno,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = ColorTranslator.FromHtml("#0b5394"),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 15),
                Width = 280
            };
            Controls.Add(lblNombre);

            var picQr = new PictureBox
            {
                Image = qrBitmap,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(50, 50),
                Size = new Size(220, 220),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(picQr);

            var lblCodigo = new Label
            {
                Text = $"Código: {codigo}",
                Font = new Font("Segoe UI", 11),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(20, 280),
                Width = 280
            };
            Controls.Add(lblCodigo);

            var btnGuardar = new Button
            {
                Text = "Guardar como imagen (PNG)",
                BackColor = ColorTranslator.FromHtml("#0b5394"),
                ForeColor = Color.White,
                Location = new Point(50, 330),
                Width = 220,
                Height = 34
            };
            btnGuardar.Click += (s, e) => GuardarImagen(nombreAlumno, codigo);
            Controls.Add(btnGuardar);

            var btnCerrar = new Button
            {
                Text = "Cerrar",
                Location = new Point(50, 375),
                Width = 220,
                Height = 30
            };
            btnCerrar.Click += (s, e) => Close();
            Controls.Add(btnCerrar);
        }

        private void GuardarImagen(string nombreAlumno, string codigo)
        {
            using var dialogo = new SaveFileDialog
            {
                Filter = "Imagen PNG (*.png)|*.png",
                FileName = $"QR_{codigo}_{nombreAlumno}.png"
            };

            if (dialogo.ShowDialog(this) == DialogResult.OK)
            {
                qrBitmap.Save(dialogo.FileName, ImageFormat.Png);
                MessageBox.Show("Imagen guardada. Ya la puedes imprimir.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                qrBitmap?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
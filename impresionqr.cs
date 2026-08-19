using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using QRCoder;

namespace AsistenciaNatacion
{
    /// <summary>
    /// Genera una página HTML con el QR de cada alumno en una cuadrícula, y la abre
    /// en el navegador. Elegimos HTML en vez de imprimir directo con GDI+ porque el
    /// navegador ya sabe acomodar automáticamente cuántos QR caben por hoja al
    /// imprimir (Ctrl+P) — es mucho más simple que calcular la paginación a mano.
    /// </summary>
    public static class ImpresionQr
    {
        public static string GenerarHojaQrTodos(List<Alumno> alumnos)
        {
            var sb = new StringBuilder();
            sb.Append(@"
                <html><head><meta charset='utf-8'><title>Códigos QR - Alumnos</title>
                <style>
                    body { font-family: Segoe UI, Arial, sans-serif; }
                    .grid { display: flex; flex-wrap: wrap; gap: 16px; }
                    .tarjeta {
                        border: 1px solid #ccc; border-radius: 8px; padding: 10px;
                        width: 160px; text-align: center; page-break-inside: avoid;
                    }
                    .tarjeta img { width: 130px; height: 130px; }
                    .nombre { font-weight: bold; margin-top: 6px; }
                    .codigo { color: #555; font-size: 13px; }
                </style></head><body>
                <h2>Códigos QR de alumnos</h2>
                <div class='grid'>
            ");

            using var generador = new QRCodeGenerator();
            foreach (var alumno in alumnos)
            {
                if (string.IsNullOrWhiteSpace(alumno.Codigo)) continue;

                var datosQr = generador.CreateQrCode(alumno.Codigo, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new QRCode(datosQr);
                using var bitmap = qrCode.GetGraphic(10);

                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                string base64 = Convert.ToBase64String(ms.ToArray());

                sb.Append($@"
                    <div class='tarjeta'>
                        <img src='data:image/png;base64,{base64}' />
                        <div class='nombre'>{System.Net.WebUtility.HtmlEncode(alumno.Nombre)}</div>
                        <div class='codigo'>{System.Net.WebUtility.HtmlEncode(alumno.Codigo)}</div>
                    </div>
                ");
            }

            sb.Append("</div></body></html>");

            string rutaArchivo = Path.Combine(Path.GetTempPath(), "qr_alumnos.html");
            File.WriteAllText(rutaArchivo, sb.ToString(), Encoding.UTF8);
            return rutaArchivo;
        }

        public static void AbrirEnNavegador(string rutaArchivo)
        {
            // UseShellExecute = true: le pide a Windows que abra el archivo con el
            // programa por defecto (el navegador), en vez de tratar de ejecutarlo.
            Process.Start(new ProcessStartInfo(rutaArchivo) { UseShellExecute = true });
        }
    }
}
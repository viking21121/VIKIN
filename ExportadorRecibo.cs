using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;

namespace AsistenciaNatacion;

/// <summary>
/// Generador de comprobantes HTML imprimibles.
/// Mantiene el folio en un archivo local por año para evitar duplicados
/// incluso cuando la aplicación se reinicia.
/// </summary>
public static class ExportadorRecibo
{
    private static readonly object FolioLock = new();

    public sealed class DatosRecibo
    {
        public string Academia { get; init; } = "ACADEMIA DE NATACIÓN";
        public string IdentificacionAcademia { get; init; } = "";
        public DateTime FechaHora { get; init; } = DateTime.Now;
        public string Alumno { get; init; } = "";
        public string CodigoAlumno { get; init; } = "";
        public string Cedula { get; init; } = "";
        public decimal MontoUsd { get; init; }
        public decimal TasaBcv { get; init; }
        public decimal MontoBs { get; init; }
        public string MetodoPago { get; init; } = "";
        public string Referencia { get; init; } = "";
        public decimal SaldoPendiente { get; init; }
        public string Periodo { get; init; } = "";
    }

    public static string GenerarFolio(DateTime fechaHora)
    {
        lock (FolioLock)
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AsistenciaHoyForm");
            Directory.CreateDirectory(dir);

            string archivo = Path.Combine(dir, $"folios-{fechaHora:yyyy}.txt");
            int ultimo = 0;

            if (File.Exists(archivo))
            {
                var txt = File.ReadAllText(archivo).Trim();
                int.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out ultimo);
            }

            ultimo++;
            File.WriteAllText(archivo, ultimo.ToString(CultureInfo.InvariantCulture));

            return $"REC-{fechaHora:yyyy}-{ultimo:0000}";
        }
    }

    
public static string GenerarHtml(DatosRecibo datos, string folio)
    {
        string E(string? value)
        {
            return HtmlEncoder.Default.Encode(value ?? "");
        }

        string Money(decimal value)
        {
            return value.ToString("N2", CultureInfo.InvariantCulture);
        }

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"es\">");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("<title>Recibo " + E(folio) + "</title>");

        html.AppendLine("<style>");
        html.AppendLine("@page { size: A4; margin: 14mm; }");
        html.AppendLine("body { font-family: Arial, Helvetica, sans-serif; color: #222; margin: 0; background: #fff; }");
        html.AppendLine(".receipt { max-width: 760px; margin: auto; border: 1px solid #bbb; padding: 28px; }");
        html.AppendLine(".header { text-align: center; border-bottom: 2px solid #222; padding-bottom: 16px; }");
        html.AppendLine(".header h1 { margin: 0 0 5px; font-size: 24px; }");
        html.AppendLine(".header p { margin: 3px 0; color: #555; }");
        html.AppendLine(".folio { text-align: right; margin: 18px 0; font-weight: bold; }");
        html.AppendLine(".section { margin-top: 18px; }");
        html.AppendLine(".section-title { font-weight: bold; background: #f1f1f1; padding: 8px; }");
        html.AppendLine(".row { display: flex; justify-content: space-between; padding: 7px 4px; border-bottom: 1px solid #eee; gap: 20px; }");
        html.AppendLine(".label { color: #555; }");
        html.AppendLine(".value { font-weight: 600; text-align: right; }");
        html.AppendLine(".total { font-size: 18px; font-weight: bold; }");
        html.AppendLine(".note { margin-top: 28px; font-size: 12px; color: #666; text-align: center; }");
        html.AppendLine(".actions { text-align: center; margin: 20px 0; }");
        html.AppendLine("button { padding: 10px 18px; font-size: 15px; cursor: pointer; }");
        html.AppendLine("@media print { .actions { display: none; } .receipt { border: 0; } }");
        html.AppendLine("</style>");

        html.AppendLine("</head>");
        html.AppendLine("<body>");

        html.AppendLine("<div class=\"receipt\">");

        // Encabezado
        html.AppendLine("<div class=\"header\">");
        html.AppendLine("<h1>" + E(datos.Academia) + "</h1>");

        if (!string.IsNullOrWhiteSpace(datos.IdentificacionAcademia))
        {
            html.AppendLine("<p>" + E(datos.IdentificacionAcademia) + "</p>");
        }

        html.AppendLine("<p>COMPROBANTE DE PAGO</p>");
        html.AppendLine("</div>");

        // Folio y fecha
        html.AppendLine("<div class=\"folio\">");
        html.AppendLine("Folio: #" + E(folio) + "<br>");
        html.AppendLine("Fecha: " + datos.FechaHora.ToString("dd/MM/yyyy HH:mm"));
        html.AppendLine("</div>");

        // Datos del alumno
        html.AppendLine("<div class=\"section\">");
        html.AppendLine("<div class=\"section-title\">DATOS DEL ALUMNO</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Nombre</span>" +
            "<span class=\"value\">" + E(datos.Alumno) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Código</span>" +
            "<span class=\"value\">" + E(datos.CodigoAlumno) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Cédula</span>" +
            "<span class=\"value\">" + E(datos.Cedula) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Período</span>" +
            "<span class=\"value\">" + E(datos.Periodo) + "</span>" +
            "</div>");

        html.AppendLine("</div>");

        // Desglose del pago
        html.AppendLine("<div class=\"section\">");
        html.AppendLine("<div class=\"section-title\">DESGLOSE DEL PAGO</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Monto abonado</span>" +
            "<span class=\"value\">$ " + Money(datos.MontoUsd) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Tasa BCV aplicada</span>" +
            "<span class=\"value\">Bs " + Money(datos.TasaBcv) + " / $</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Equivalente en bolívares</span>" +
            "<span class=\"value\">Bs " + Money(datos.MontoBs) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Método de pago</span>" +
            "<span class=\"value\">" + E(datos.MetodoPago) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row\">" +
            "<span class=\"label\">Referencia</span>" +
            "<span class=\"value\">" + E(datos.Referencia) + "</span>" +
            "</div>");

        html.AppendLine(
            "<div class=\"row total\">" +
            "<span>Saldo pendiente</span>" +
            "<span>Bs " + Money(datos.SaldoPendiente) + "</span>" +
            "</div>");

        html.AppendLine("</div>");

        // Nota
        html.AppendLine("<div class=\"note\">");
        html.AppendLine(
            "Este comprobante registra el pago realizado " +
            "en la fecha y hora indicadas.");
        html.AppendLine("</div>");

        html.AppendLine("</div>");

        // Botón de impresión
        html.AppendLine("<div class=\"actions\">");
        html.AppendLine(
            "<button onclick=\"window.print()\">" +
            "&#128424; Imprimir / Guardar como PDF" +
            "</button>");
        html.AppendLine("</div>");

        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }



    public static string GuardarHtml(DatosRecibo datos, string? folio = null)
    {
        folio ??= GenerarFolio(datos.FechaHora);

        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Recibos Academia");
        Directory.CreateDirectory(dir);

        string file = Path.Combine(dir, $"{folio}.html");
        File.WriteAllText(file, GenerarHtml(datos, folio), new UTF8Encoding(false));
        return file;
    }

    public static void AbrirEnNavegador(DatosRecibo datos, string? folio = null)
    {
        string file = GuardarHtml(datos, folio);
        Process.Start(new ProcessStartInfo
        {
            FileName = file,
            UseShellExecute = true
        });
    }
}

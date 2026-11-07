using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AsistenciaNatacion
{
    public static class ImpresionRecibo
    {
        private static readonly ObtenerConfiguracion obtenerConfiguracion =
    AppServices.CrearObtenerConfiguracion();
        public static string GenerarReciboHtml(
            string nombreAlumno,
            string codigoAlumno,
            string? cedulaAlumno,
            int anio,
            int mes,
            decimal montoDolares,
            decimal tasaBcv,
            decimal montoBs,
            string metodoPago,
            string referencia,
            decimal cuotaTotal,
            decimal nuevoAbonado,
            decimal nuevoRestante,
            string? folio = null,
            DateTime? fechaHora = null)
        {
            DateTime fechaRecibo = fechaHora ?? DateTime.Now;
            int actividadId = Application.App.ContextoActividad.ActividadId;
            var configuracion = obtenerConfiguracion.Ejecutar(actividadId);

            string numRecibo = folio
                ?? $"REC-{fechaRecibo:yyyyMMdd}-{fechaRecibo:HHmmss}";

            string html = ConstruirHtml(
                configuracion.Nombre,
                configuracion.Identificacion,
                configuracion.TipoEstablecimiento,
                actividadId,
                nombreAlumno,
                codigoAlumno,
                cedulaAlumno,
                anio,
                mes,
                montoDolares,
                tasaBcv,
                montoBs,
                metodoPago,
                referencia,
                cuotaTotal,
                nuevoAbonado,
                nuevoRestante,
                numRecibo,
                fechaRecibo);

            string rutaArchivo = Path.Combine(
                Path.GetTempPath(),
                $"recibo_{numRecibo}.html");

            File.WriteAllText(
                rutaArchivo,
                html,
                Encoding.UTF8);

            return rutaArchivo;
        }

        /// <summary>
        /// Única plantilla de recibo de VIKING: la usan la ventana de PC y la API
        /// que consulta VikingMobile, para que se vean exactamente igual.
        /// Devuelve el HTML; no escribe archivos.
        /// </summary>
        public static string ConstruirHtml(
            string establecimiento,
            string identificacion,
            string tipo,
            int actividadId,
            string nombreAlumno,
            string codigoAlumno,
            string? cedulaAlumno,
            int anio,
            int mes,
            decimal montoDolares,
            decimal tasaBcv,
            decimal montoBs,
            string metodoPago,
            string referencia,
            decimal cuotaTotal,
            decimal nuevoAbonado,
            decimal nuevoRestante,
            string numRecibo,
            DateTime fechaRecibo)
        {
            string nombreEstablecimiento = System.Net.WebUtility.HtmlEncode(establecimiento ?? "");
            string tipoEstablecimiento = System.Net.WebUtility.HtmlEncode(tipo ?? "");
            string identificacionEstablecimiento = System.Net.WebUtility.HtmlEncode(identificacion ?? "");
            string fechaStr = fechaRecibo.ToString("dd/MM/yyyy hh:mm tt");

            static string H(string? texto) => System.Net.WebUtility.HtmlEncode(texto ?? "");

            string estadoSello = nuevoRestante <= 0 ? "PAGADO" : "ABONO";
            string claseSello = nuevoRestante <= 0 ? "" : " abono";
            string colorResta = nuevoRestante > 0 ? "#C62828" : "#0B7A3B";
            string cedulaHtml = string.IsNullOrWhiteSpace(cedulaAlumno) ? "—" : H(cedulaAlumno);
            string filaReferencia = string.IsNullOrWhiteSpace(referencia)
                ? ""
                : "<tr><td>N° de referencia</td><td>#" + H(referencia) + "</td></tr>";

            string logoHtml = "";
            try
            {
                var (logoBytes, logoTipo) = Infrastructure.Carnet.DatosCarnetRepositorio
                    .Logo(actividadId);
                if (logoBytes != null)
                    logoHtml = "<img class='logo' src='data:" + H(logoTipo) + ";base64,"
                        + Convert.ToBase64String(logoBytes) + "'/>";
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Warn("Logo del recibo: " + ex.Message);
            }

            string subtitulo = string.IsNullOrWhiteSpace(identificacionEstablecimiento)
                ? tipoEstablecimiento
                : identificacionEstablecimiento + " · " + tipoEstablecimiento;

            var sb = new StringBuilder();

            sb.Append($@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Recibo {H(numRecibo)}</title>
<style>");

            // Tamaño de papel A5. box-sizing: border-box evita que el contenido se
            // salga por la derecha al convertir a PDF.
            sb.Append(@"
@page { size: A5; margin: 0; }
* { box-sizing: border-box; -webkit-print-color-adjust: exact; print-color-adjust: exact; }
:root { --navy: #04152F; --azul: #1E6FD9; --oro: #D4A935; --texto: #1d2733; --suave: #6b7686; --linea: #e6eaf0; }
body { margin: 0; background: #e9edf2; font-family: 'Segoe UI', Arial, sans-serif; color: var(--texto); }
.hoja { width: 148mm; height: 210mm; margin: 16px auto; background: #fff; position: relative; overflow: hidden;
        box-shadow: 0 4px 18px rgba(0,0,0,.18); }
@media print { body { background: #fff; } .hoja { margin: 0; box-shadow: none; } .no-print { display: none !important; } }
.cab { background: linear-gradient(135deg, var(--navy), #0B2F63); color: #fff; padding: 8mm 10mm 7mm;
       display: flex; align-items: center; gap: 5mm; position: relative; }
.cab::after { content: ''; position: absolute; left: 0; right: 0; bottom: 0; height: 1.2mm; background: var(--oro); }
.logo { height: 16mm; width: 16mm; object-fit: contain; background: #fff; border-radius: 50%; padding: 1mm; flex: none; }
.cab h1 { margin: 0; font-size: 16pt; letter-spacing: .8px; text-transform: uppercase; line-height: 1.1; }
.cab .sub { margin-top: 1.2mm; font-size: 8.5pt; color: #b9d2f5; letter-spacing: .5px; }
.titulo { display: flex; justify-content: space-between; align-items: flex-end; padding: 7mm 10mm 0; }
.titulo .t { font-size: 8pt; letter-spacing: 1.8px; color: var(--suave); font-weight: 700; text-transform: uppercase; }
.titulo .n { font-size: 14pt; font-weight: 800; color: var(--navy); margin-top: .5mm; }
.sello { border: .5mm solid #0B7A3B; color: #0B7A3B; padding: 1mm 4mm; border-radius: 1.5mm; font-weight: 800;
         letter-spacing: 1.5px; font-size: 10pt; transform: rotate(-4deg); }
.sello.abono { border-color: #C77700; color: #C77700; }
.grid { display: grid; grid-template-columns: 1fr 1fr; gap: 4mm 8mm; padding: 6mm 10mm 0; }
.dato .l { font-size: 7pt; letter-spacing: 1px; text-transform: uppercase; color: var(--suave); font-weight: 700; }
.dato .v { font-size: 10.5pt; font-weight: 600; margin-top: .5mm; word-break: break-word; }
.monto { margin: 7mm 10mm 0; padding: 5mm; border: .3mm solid #cfe3ff; background: #f3f8ff; border-radius: 2.5mm; text-align: center; }
.monto .et { font-size: 7.5pt; letter-spacing: 1.4px; text-transform: uppercase; color: var(--suave); font-weight: 700; }
.monto .usd { font-size: 26pt; font-weight: 800; color: var(--navy); line-height: 1.15; }
.monto .bs { font-size: 9pt; color: var(--suave); margin-top: 1mm; }
.tabla { margin: 6mm 10mm 0; width: calc(100% - 20mm); border-collapse: collapse; font-size: 10pt; }
.tabla td { padding: 2.2mm 0; border-bottom: .25mm solid var(--linea); }
.tabla td:first-child { color: var(--suave); }
.tabla td:last-child { text-align: right; font-weight: 700; }
.tabla tr.tot td { border-bottom: none; border-top: .5mm solid var(--navy); padding-top: 3mm; font-size: 11pt; color: var(--texto); }
.firma { position: absolute; left: 10mm; right: 10mm; bottom: 27mm; display: flex; justify-content: flex-end; }
.firma div { width: 50mm; border-top: .3mm solid #9aa5b4; text-align: center; font-size: 7.5pt; color: var(--suave); padding-top: 1.2mm; }
.pie { position: absolute; left: 0; right: 0; bottom: 0; padding: 5mm 10mm 6mm; text-align: center; font-size: 8pt;
       color: var(--suave); border-top: .25mm solid var(--linea); }
.pie b { color: var(--navy); }
.btn-imprimir { display: block; margin: 0 auto 16px; background: #1E6FD9; color: #fff; border: none; padding: 10px 22px;
                border-radius: 6px; font-size: 14px; font-weight: 700; cursor: pointer; }

/* Pantallas angostas (teléfono): la hoja ocupa todo el ancho y fluye hacia abajo. */
@media screen and (max-width: 600px) {
  body { background: #fff; }
  .hoja { width: 100%; height: auto; margin: 0; box-shadow: none; overflow: visible; }
  .firma, .pie { position: static; }
  .firma { margin: 12mm 10mm 0; }
  .pie { margin-top: 8mm; }
  .btn-imprimir { width: calc(100% - 20px); margin: 12px 10px 16px; }
}
");

            sb.Append($@"
</style>
</head>
<body>

<div class='hoja'>
  <div class='cab'>
    {logoHtml}
    <div>
      <h1>{nombreEstablecimiento}</h1>
      <div class='sub'>{subtitulo}</div>
    </div>
  </div>

  <div class='titulo'>
    <div>
      <div class='t'>Comprobante de pago</div>
      <div class='n'>N° {H(numRecibo)}</div>
    </div>
    <div class='sello{claseSello}'>{estadoSello}</div>
  </div>

  <div class='grid'>
    <div class='dato'><div class='l'>Alumno</div><div class='v'>{H(nombreAlumno)}</div></div>
    <div class='dato'><div class='l'>Fecha y hora</div><div class='v'>{fechaStr}</div></div>
    <div class='dato'><div class='l'>Código</div><div class='v'>{H(codigoAlumno)}</div></div>
    <div class='dato'><div class='l'>Cédula / ID</div><div class='v'>{cedulaHtml}</div></div>
    <div class='dato'><div class='l'>Período</div><div class='v'>{mes:D2}/{anio}</div></div>
    <div class='dato'><div class='l'>Método de pago</div><div class='v'>{H(metodoPago)}</div></div>
  </div>

  <div class='monto'>
    <div class='et'>Monto recibido</div>
    <div class='usd'>${montoDolares:N2}</div>
    <div class='bs'>{montoBs:N2} Bs. · Tasa BCV {tasaBcv:N2} Bs/$</div>
  </div>

  <table class='tabla'>
    {filaReferencia}
    <tr><td>Cuota total del mes</td><td>${cuotaTotal:N2}</td></tr>
    <tr><td>Total abonado</td><td>${nuevoAbonado:N2}</td></tr>
    <tr class='tot'><td>Resta por pagar</td><td style='color: {colorResta}'>${nuevoRestante:N2}</td></tr>
  </table>

  <div class='firma'><div>Recibido por</div></div>

  <div class='pie'>
    <b>¡Gracias por su pago!</b><br/>
    Conserve este recibo como comprobante oficial.
  </div>
</div>

<button class='btn-imprimir no-print' onclick='window.print()'>Imprimir / Guardar en PDF</button>

</body>
</html>
");

            return sb.ToString();
        }

        public static void MostrarRecibo(
            string nombreAlumno,
            string codigoAlumno,
            string? cedulaAlumno,
            int anio,
            int mes,
            decimal montoDolares,
            decimal tasaBcv,
            decimal montoBs,
            string metodoPago,
            string referencia,
            decimal cuotaTotal,
            decimal nuevoAbonado,
            decimal nuevoRestante,
            string? folio = null,
            DateTime? fechaHora = null)
        {
            string ruta = GenerarReciboHtml(
                nombreAlumno,
                codigoAlumno,
                cedulaAlumno,
                anio,
                mes,
                montoDolares,
                tasaBcv,
                montoBs,
                metodoPago,
                referencia,
                cuotaTotal,
                nuevoAbonado,
                nuevoRestante,
                folio,
                fechaHora);

            // Se genera también el PDF (queda junto al HTML) y se abre ese;
            // si no se puede, se abre el HTML como antes.
            string archivoAbrir =
                AsistenciaNatacion.Infrastructure.Pdf.HtmlAPdf.Convertir(ruta) ?? ruta;

            Process.Start(new ProcessStartInfo(archivoAbrir)
            {
                UseShellExecute = true
            });
        }
    }
}
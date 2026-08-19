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
            var configuracion = obtenerConfiguracion.Ejecutar(Application.App.ContextoActividad.ActividadId);

            string nombreEstablecimiento =
                System.Net.WebUtility.HtmlEncode(configuracion.Nombre);

            string tipoEstablecimiento =
                System.Net.WebUtility.HtmlEncode(configuracion.TipoEstablecimiento);

            string identificacionEstablecimiento =
                System.Net.WebUtility.HtmlEncode(configuracion.Identificacion);

            string numRecibo = folio
                ?? $"REC-{fechaRecibo:yyyyMMdd}-{fechaRecibo:HHmmss}";

            string fechaStr = fechaRecibo.ToString(
                "dd/MM/yyyy hh:mm tt");

            var sb = new StringBuilder();

            sb.Append($@"
<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Recibo {System.Net.WebUtility.HtmlEncode(numRecibo)}</title>

<style>
    body {{
        font-family: Arial, sans-serif;
        background: #f4f6f8;
        padding: 20px;
        margin: 0;
    }}

    .recibo-card {{
        max-width: 560px;
        margin: auto;
        background: white;
        border-radius: 12px;
        padding: 30px;
        box-shadow: 0 4px 15px rgba(0,0,0,0.08);
    }}

    .encabezado {{
        text-align: center;
        margin-bottom: 20px;
    }}

    .encabezado h1 {{
        margin: 0;
        color: #0b5394;
    }}

    .folio {{
        text-align: center;
        background: #e3f2fd;
        color: #0b5394;
        padding: 8px;
        border-radius: 6px;
        font-weight: bold;
        margin-top: 10px;
    }}

    .filas {{
        margin-top: 15px;
    }}

    .fila {{
        display: flex;
        justify-content: space-between;
        gap: 15px;
        padding: 8px 0;
        border-bottom: 1px solid #eee;
    }}

    .label {{
        color: #555;
    }}

    .val {{
        font-weight: bold;
        text-align: right;
    }}

    .monto-box {{
        margin: 20px 0;
        padding: 18px;
        border: 1px solid #b7ebc6;
        border-radius: 10px;
        background: #f0fff4;
        text-align: center;
    }}

    .monto-dl {{
        font-size: 28px;
        font-weight: bold;
        color: #087f23;
    }}

    .monto-bs {{
        margin-top: 5px;
        color: #087f23;
    }}

    .footer {{
        text-align: center;
        margin-top: 20px;
        color: #777;
    }}

    .btn-imprimir {{
        display: block;
        width: 100%;
        background: #0b5394;
        color: white;
        border: none;
        padding: 10px;
        border-radius: 6px;
        font-size: 14px;
        font-weight: bold;
        cursor: pointer;
        margin-top: 15px;
    }}

    @media print {{
        body {{
            background: white !important;
            padding: 0 !important;
        }}

        .recibo-card {{
            box-shadow: none !important;
            border: none !important;
            width: 100% !important;
            max-width: 100% !important;
            margin: 0 !important;
        }}

        .btn-imprimir {{
            display: none !important;
            visibility: hidden !important;
        }}
    }}
</style>
</head>

<body>

<div class='recibo-card'>

    <div class='encabezado'>
        <h1>{nombreEstablecimiento}</h1>
<div>{tipoEstablecimiento}</div>

        <div class='folio'>
            COMPROBANTE N° {System.Net.WebUtility.HtmlEncode(numRecibo)}
        </div>
    </div>

    <div class='filas'>

        <div class='fila'>
            <span class='label'>Fecha y Hora:</span>
            <span class='val'>{fechaStr}</span>
        </div>

        <div class='fila'>
            <span class='label'>Alumno:</span>
            <span class='val'>
                {System.Net.WebUtility.HtmlEncode(nombreAlumno)}
            </span>
        </div>

        <div class='fila'>
            <span class='label'>Código / Cédula:</span>
            <span class='val'>
                {System.Net.WebUtility.HtmlEncode(codigoAlumno)}
                {(string.IsNullOrWhiteSpace(cedulaAlumno)
                    ? ""
                    : " | " + System.Net.WebUtility.HtmlEncode(cedulaAlumno))}
            </span>
        </div>

        <div class='fila'>
            <span class='label'>Período Correspondiente:</span>
            <span class='val'>{mes:D2}/{anio}</span>
        </div>

    </div>

    <div class='monto-box'>

        <div class='monto-dl'>
            ${montoDolares:N2}
        </div>

        <div class='monto-bs'>
            ({montoBs:N2} Bs. a Tasa BCV {tasaBcv:N2} Bs/$)
        </div>

    </div>

    <div class='filas'>

        <div class='fila'>
            <span class='label'>Método de Pago:</span>
            <span class='val'>
                {System.Net.WebUtility.HtmlEncode(metodoPago)}
            </span>
        </div>

        {(string.IsNullOrWhiteSpace(referencia)
            ? ""
            : $@"
        <div class='fila'>
            <span class='label'>N° Referencia:</span>
            <span class='val'>
                #{System.Net.WebUtility.HtmlEncode(referencia)}
            </span>
        </div>")}

        <div class='fila'>
            <span class='label'>Cuota Total Mes:</span>
            <span class='val'>
                ${cuotaTotal:N2}
            </span>
        </div>

        <div class='fila'>
            <span class='label'>Total Abonado:</span>
            <span class='val'>
                ${nuevoAbonado:N2}
            </span>
        </div>

        <div class='fila'>
            <span class='label'>Resta por Pagar:</span>
            <span class='val'
                  style='color: {(nuevoRestante > 0
                      ? "#c62828"
                      : "#2e7d32")}'>
                ${nuevoRestante:N2}
            </span>
        </div>

    </div>

    <div class='footer'>
        ¡Muchas gracias por su pago!<br>
        <i>Conserve este recibo como comprobante oficial.</i>
    </div>

    <button
        class='btn-imprimir no-print'
        onclick='window.print()'>
        🖨️ Imprimir / Guardar en PDF
    </button>

</div>

</body>
</html>
");

            string rutaArchivo = Path.Combine(
                Path.GetTempPath(),
                $"recibo_{numRecibo}.html");

            File.WriteAllText(
                rutaArchivo,
                sb.ToString(),
                Encoding.UTF8);

            return rutaArchivo;
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

            Process.Start(new ProcessStartInfo(ruta)
            {
                UseShellExecute = true
            });
        }
    }
}
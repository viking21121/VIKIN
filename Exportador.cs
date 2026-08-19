using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AsistenciaNatacion
{
    /// <summary>
    /// Genera un archivo CSV compatible directamente con Excel en español,
    /// abriendo la información formateada en columnas ordenadas.
    /// </summary>
    public static class Exportador
    {
        public static void ExportarResumenCsv(List<EstadoAlumnoMes> estados, string rutaArchivo)
        {
            var sb = new StringBuilder();

            // Usamos punto y coma (;) como separador para que Excel en español organice en columnas automáticamente
            string separador = ";";

            // Encabezados
            sb.AppendLine(string.Join(separador,
                "Código",
                "Cédula",
                "Alumno",
                "Plan",
                "Cuota ($)",
                "Abonado ($)",
                "Falta ($)",
                "Estado Pago",
                "Clases Tomadas",
                "Clases Restantes",
                "Estado Asistencia"
            ));

            foreach (var est in estados)
            {
                string estadoPago = est.EstaPagado ? "Completo" : (est.MontoAbonado > 0 ? "Parcial" : "Pendiente");

                sb.AppendLine(string.Join(separador,
                    CampoCsv(est.Codigo),
                    CampoCsv(est.Cedula ?? ""),
                    CampoCsv(est.Nombre),
                    CampoCsv(est.PlanNombre),
                    est.CuotaTotal.ToString("F2", CultureInfo.InvariantCulture),
                    est.MontoAbonado.ToString("F2", CultureInfo.InvariantCulture),
                    est.MontoRestante.ToString("F2", CultureInfo.InvariantCulture),
                    CampoCsv(estadoPago),
                    est.Tomadas.ToString(),
                    (est.ControlaClases ? est.Restantes.ToString() : ""),
                    CampoCsv(est.EstadoTexto)
                ));
            }

            // Se incluye el BOM (Byte Order Mark) con new UTF8Encoding(true)
            // para que Excel reconozca correctamente los caracteres con tildes y eñes.
            File.WriteAllText(rutaArchivo, sb.ToString(), new UTF8Encoding(true));
        }

        private static string CampoCsv(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";

            // En un CSV separado por punto y coma, escapamos si contiene punto y coma, comillas o salto de línea
            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }
            return valor;
        }
    }
}
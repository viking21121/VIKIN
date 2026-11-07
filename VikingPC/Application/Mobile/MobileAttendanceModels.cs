using System;

namespace AsistenciaNatacion.Application.Mobile
{
    public sealed class MobileAttendanceRequest
    {
        public string CodigoAlumno { get; set; } = "";
        public string Qr { get; set; } = "";
        public int? ActividadId { get; set; }
        public DateTime? FechaHora { get; set; }

        /// <summary>
        /// Identificador único de la operación, generado por el teléfono al
        /// escanear. Si el mismo identificador llega dos veces (por ejemplo,
        /// porque se perdió la respuesta y la app reenvió la asistencia), el
        /// servidor responde que ya estaba registrada y no repite nada.
        /// Es opcional para no romper versiones antiguas de VikingMobile.
        /// </summary>
        public string? IdOperacion { get; set; }
    }

    public sealed class MobileAttendanceResponse
    {
        public bool Ok { get; set; }
        public string Mensaje { get; set; } = "";
        public string? Alumno { get; set; }
        public int? AlumnoId { get; set; }

        public string? Plan { get; set; }
        public string? FechaVencimiento { get; set; }
        public string? EstadoSemaforo { get; set; }
        public string? TituloEstado { get; set; }
        public string? SubtituloEstado { get; set; }
        public bool TieneFoto { get; set; }

        public static MobileAttendanceResponse Success(
            string mensaje,
            string? alumno = null,
            int? alumnoId = null,
            string? plan = null,
            System.DateTime? fechaVencimiento = null,
            string? estadoSemaforo = null,
            string? tituloEstado = null,
            string? subtituloEstado = null,
            bool tieneFoto = false)
        {
            return new MobileAttendanceResponse
            {
                Ok = true,
                Mensaje = mensaje,
                Alumno = alumno,
                AlumnoId = alumnoId,
                Plan = plan,
                FechaVencimiento = fechaVencimiento?.ToString("dd/MM/yyyy"),
                EstadoSemaforo = estadoSemaforo,
                TituloEstado = tituloEstado,
                SubtituloEstado = subtituloEstado,
                TieneFoto = tieneFoto
            };
        }

        public static MobileAttendanceResponse Fail(string mensaje)
        {
            return new MobileAttendanceResponse
            {
                Ok = false,
                Mensaje = mensaje
            };
        }
    }
}
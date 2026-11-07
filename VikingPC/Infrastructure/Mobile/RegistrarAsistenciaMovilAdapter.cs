using AsistenciaNatacion.Application.Mobile;
using System;

namespace AsistenciaNatacion.Infrastructure.Mobile
{
    /// <summary>
    /// Adaptador puente entre la API móvil y la lógica existente de Viking.
    ///
    /// La función real de asistencia se inyecta desde el arranque de Viking.
    /// Esto evita que el servidor HTTP conozca SQLite, WinForms o SQL.
    /// </summary>
    public sealed class RegistrarAsistenciaMovilAdapter : IRegistrarAsistenciaMovil
    {
        private readonly Func<MobileAttendanceRequest, MobileAttendanceResponse> ejecutar;

        public RegistrarAsistenciaMovilAdapter(
            Func<MobileAttendanceRequest, MobileAttendanceResponse> ejecutar)
        {
            this.ejecutar =
                ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));
        }

        public MobileAttendanceResponse Ejecutar(
            MobileAttendanceRequest request)
        {
            return ejecutar(request);
        }
    }
}

using System;
using System.Collections.Generic;
using AsistenciaNatacion.Application.Mobile;

namespace AsistenciaNatacion.Infrastructure.Mobile
{
    /// <summary>
    /// Punto único para arrancar/detener la API móvil desde Viking.
    /// </summary>
    public static class VikingMobileApiHost
    {
        private static VikingMobileApiServer? server;

        public static bool EstaEjecutando =>
            server?.EstaEjecutando == true;

        public static void Iniciar(
            Func<MobileAttendanceRequest, MobileAttendanceResponse> registrarAsistencia,
            Func<List<AsistenciaNatacion.Domain.Actividad>> obtenerActividades,
            int port = 5050)
        {
            if (server?.EstaEjecutando == true)
                return;

            var adapter =
                new RegistrarAsistenciaMovilAdapter(
                    registrarAsistencia);

            server = new VikingMobileApiServer(
                adapter,
                obtenerActividades,
                port);

            server.Iniciar();
        }

        public static void Detener()
        {
            if (server == null)
                return;

            server.DetenerAsync()
                .GetAwaiter()
                .GetResult();

            server.Dispose();
            server = null;
        }
    }
}
using System;
using System.IO;
using AsistenciaNatacion.Domain;

namespace AsistenciaNatacion.Application.UseCases
{
    public sealed class ObtenerCarnetPorId
    {
        private readonly ObtenerAlumno obtenerAlumno;
        private readonly ObtenerEstadoAlumno obtenerEstadoAlumno;

        public ObtenerCarnetPorId(
            ObtenerAlumno obtenerAlumno,
            ObtenerEstadoAlumno obtenerEstadoAlumno)
        {
            this.obtenerAlumno = obtenerAlumno ?? throw new ArgumentNullException(nameof(obtenerAlumno));
            this.obtenerEstadoAlumno = obtenerEstadoAlumno ?? throw new ArgumentNullException(nameof(obtenerEstadoAlumno));
        }

        public CarnetAlumno? Ejecutar(int alumnoId)
        {
            if (alumnoId <= 0) return null;

            var alumno = obtenerAlumno.Ejecutar(alumnoId);
            if (alumno == null) return null;

            return EvaluarCarnet(alumno);
        }

        public CarnetAlumno? EjecutarPorCodigo(string codigo, ObtenerAlumnoPorCodigo buscadorCodigo, int actividadId = 0)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var alumno = actividadId > 0
                ? buscadorCodigo.Ejecutar(codigo, actividadId)
                : buscadorCodigo.EjecutarGlobal(codigo);

            if (alumno == null) return null;

            return EvaluarCarnet(alumno);
        }

        private CarnetAlumno EvaluarCarnet(Alumno alumno)
        {
            int anio = DateTime.Today.Year;
            int mes = DateTime.Today.Month;

            var estado = obtenerEstadoAlumno.Ejecutar(alumno.Id, anio, mes);

            var carnet = new CarnetAlumno
            {
                AlumnoId = alumno.Id,
                Codigo = alumno.Codigo ?? "",
                Nombre = alumno.Nombre ?? "",
                Cedula = alumno.Cedula,
                Telefono = alumno.Telefono,
                PlanNombre = estado?.PlanNombre ?? (alumno.PlanVecesSemana > 0 ? $"{alumno.PlanVecesSemana}x semana" : "Sin plan"),
                FechaVencimiento = estado?.FechaVencimientoPlan ?? alumno.FechaVencimientoPlan,
                DiasRestantes = estado?.DiasRestantes ?? 0,
                SaldoPendiente = estado?.MontoRestante ?? 0m
            };

            // Resolver ruta absoluta de la foto si existe
            carnet.FotoRuta = ResolverRutaFoto(alumno.Codigo, alumno.FotoRuta);

            // Regla del semáforo centralizada:
            // 1. ROJO: Si la mensualidad está vencida (MembresiaVencida o DiasRestantes < 0 con saldo)
            // 2. AMARILLO: Si faltan datos obligatorios (cédula, teléfono o foto) o está por vencer
            // 3. VERDE: Solvente en cualquier otro caso.

            bool estaVencido = (estado != null && estado.MembresiaVencida) ||
                               (!carnet.SaldoPendiente.Equals(0m) && carnet.DiasRestantes < 0) ||
                               (carnet.FechaVencimiento.HasValue && carnet.FechaVencimiento.Value.Date < DateTime.Today && carnet.SaldoPendiente > 0);

            if (estaVencido)
            {
                carnet.EstadoSemaforo = SemaforoEstado.Rojo;
                carnet.TituloEstado = "VENCIDO";
                carnet.SubtituloEstado = "Mensualidad vencida";
                carnet.MotivoDetalle = carnet.SaldoPendiente > 0
                    ? $"Pendiente: ${carnet.SaldoPendiente:0.##}"
                    : "Fecha de plan caducada";
                return carnet;
            }

            // Validar faltantes para AMARILLO
            bool faltaCedula = string.IsNullOrWhiteSpace(carnet.Cedula);
            bool faltaTelefono = string.IsNullOrWhiteSpace(carnet.Telefono);
            bool faltaFoto = !carnet.TieneFoto;

            if (faltaCedula || faltaTelefono || faltaFoto)
            {
                carnet.EstadoSemaforo = SemaforoEstado.Amarillo;
                carnet.TituloEstado = "INCOMPLETO";

                var faltantes = new System.Collections.Generic.List<string>();
                if (faltaFoto) faltantes.Add("Falta foto");
                if (faltaCedula) faltantes.Add("Falta cédula");
                if (faltaTelefono) faltantes.Add("Falta teléfono");

                carnet.SubtituloEstado = string.Join(" · ", faltantes);
                carnet.MotivoDetalle = "Pase autorizado con aviso";
                return carnet;
            }

            if (estado != null && estado.PorVencer)
            {
                carnet.EstadoSemaforo = SemaforoEstado.Amarillo;
                carnet.TituloEstado = "POR VENCER";
                carnet.SubtituloEstado = $"Vence en {carnet.DiasRestantes} día(s)";
                carnet.MotivoDetalle = "Renovación requerida pronto";
                return carnet;
            }

            // VERDE: Solvente
            carnet.EstadoSemaforo = SemaforoEstado.Verde;
            carnet.TituloEstado = "SOLVENTE";
            carnet.SubtituloEstado = "Membresía activa al día";
            carnet.MotivoDetalle = carnet.DiasRestantes > 0 ? $"{carnet.DiasRestantes} días restantes" : "Acceso concedido";

            return carnet;
        }

        private static string? ResolverRutaFoto(string? codigo, string? fotoRuta)
        {
            if (!string.IsNullOrWhiteSpace(fotoRuta) && File.Exists(fotoRuta))
                return Path.GetFullPath(fotoRuta);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (!string.IsNullOrWhiteSpace(fotoRuta))
            {
                string relativa = Path.Combine(baseDir, fotoRuta);
                if (File.Exists(relativa)) return Path.GetFullPath(relativa);
            }

            if (!string.IsNullOrWhiteSpace(codigo))
            {
                string dirFotos = Path.Combine(baseDir, "fotos");
                string jpg = Path.Combine(dirFotos, $"{codigo.Trim()}.jpg");
                if (File.Exists(jpg)) return jpg;

                string png = Path.Combine(dirFotos, $"{codigo.Trim()}.png");
                if (File.Exists(png)) return png;

                string jpeg = Path.Combine(dirFotos, $"{codigo.Trim()}.jpeg");
                if (File.Exists(jpeg)) return jpeg;
            }

            return null;
        }
    }
}

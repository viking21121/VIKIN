using System;

namespace AsistenciaNatacion
{
    public class Alumno
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public string Nombre { get; set; } = "";
        public string? Telefono { get; set; }
        public string? Cedula { get; set; }
        public string Codigo { get; set; } = "";
        public string FechaInscripcion { get; set; } = "";
        public int PlanVecesSemana { get; set; }
        public int ClasesMes { get; set; }
        public bool Activo { get; set; } = true;
        public int PlanId { get; set; }
        // Ciclo individual de pago. Se mantiene el nombre de columna histórico
        // (fecha_inicio_plan / fecha_vencimiento_plan) para compatibilidad con SQLite.
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaVencimientoPlan { get; set; }
        public DateTime? FechaInicioCiclo => FechaInicioPlan;
        public DateTime? FechaProximoPago => FechaVencimientoPlan;
    }

    public enum EstadoMes
    {
        EnProgreso,
        Completo,
        Excedido
    }

    public class EstadoAlumnoMes
    {
        public int AlumnoId { get; set; }
        public string Nombre { get; set; } = "";
        public string Codigo { get; set; } = "";
        public string? Cedula { get; set; }
        public int PlanVecesSemana { get; set; }
        public int PlanId { get; set; }
        public string PlanNombre { get; set; } = "";
        public bool ControlaClases { get; set; }
        public int ClasesMes { get; set; }
        public int Tomadas { get; set; }
        public int Restantes { get; set; }
        public EstadoMes Estado { get; set; }

        public bool EsMembresia { get; set; }
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaVencimientoPlan { get; set; }
        public DateTime? FechaInicioCiclo => FechaInicioPlan;
        public DateTime? FechaProximoPago => FechaVencimientoPlan;
        public int DiasRestantes { get; set; }
        // Una membresía solo se considera vencida si, además de haberse
        // cumplido la fecha, el alumno todavía tiene saldo pendiente. Si ya
        // pagó (EstaPagado), no debe mostrarse como vencida aunque el ciclo
        // en base de datos todavía no se haya renovado con la nueva fecha.
        public bool MembresiaVencida => EsMembresia && !EstaPagado && DiasRestantes <= 0;
        public bool MembresiaPorVencer =>
            EsMembresia && !EstaPagado && DiasRestantes >= 0 && DiasRestantes <= 7;

        public bool PagoPorVencer =>
            !EstaPagado &&
            FechaProximoPago.HasValue &&
            DiasRestantes <= 7;

        public bool PorVencer =>
            (ControlaClases && Restantes <= 1) ||
            PagoPorVencer;

        // --- PROPIEDADES DE PAGO / ABONOS ---
        public decimal CuotaTotal { get; set; }
        public decimal MontoAbonado { get; set; }
        public decimal MontoRestante =>
            FechaInicioPlan.HasValue &&
            FechaInicioPlan.Value.Date > DateTime.Today.Date
                ? 0
                : Math.Max(CuotaTotal - MontoAbonado, 0);

        public bool EstaPagado => MontoRestante == 0;

        public bool CicloAunNoInicia =>
            FechaInicioPlan.HasValue &&
            FechaInicioPlan.Value.Date > DateTime.Today.Date;

        public string EstadoPagoTexto => EsMembresia
            ? (MembresiaVencida ? "🔴 Vencido" : MembresiaPorVencer ? "🟡 Por vencer" : "✅ Vigente")
            : (CicloAunNoInicia
                ? "🟢 Vigente"
                : EstaPagado
                    ? "✅ Completo"
                    : (MontoAbonado > 0 ? "🟡 Abono" : "❌ Pendiente"));
        public string VencimientoTexto => FechaProximoPago.HasValue ? FechaProximoPago.Value.ToString("dd/MM/yyyy") : "—";
        public string DiasRestantesTexto => FechaProximoPago.HasValue
            ? (DiasRestantes <= 0 ? "VENCIDO" : $"{DiasRestantes} días")
            : "—";

        public string EstadoTexto => Estado switch
        {
            EstadoMes.EnProgreso => "En progreso",
            EstadoMes.Completo => "Completo",
            EstadoMes.Excedido => "Excedido",
            _ => ""
        };
    }

    public class EventoAuditoria
    {
        public int Id { get; set; }
        public string Fecha { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Accion { get; set; } = "";
        public string Detalle { get; set; } = "";
    }

    public static class SesionUsuario
    {
        public static string RolActual { get; set; } = "Recepción";
        public static string OperadorActual { get; set; } = "";
    }
}
using System;

namespace AsistenciaNatacion.Domain
{
    public enum SemaforoEstado
    {
        Verde,      // Solvente
        Amarillo,   // Incompleto (datos faltantes o por vencer)
        Rojo        // Vencido / Moroso
    }

    public sealed class CarnetAlumno
    {
        public int AlumnoId { get; set; }
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public string PlanNombre { get; set; } = "";
        public string? FotoRuta { get; set; }
        public SemaforoEstado EstadoSemaforo { get; set; }
        public string TituloEstado { get; set; } = "";
        public string SubtituloEstado { get; set; } = "";
        public string? MotivoDetalle { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public int DiasRestantes { get; set; }
        public decimal SaldoPendiente { get; set; }
        public bool TieneFoto => !string.IsNullOrWhiteSpace(FotoRuta) && System.IO.File.Exists(FotoRuta);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace AsistenciaNatacion.Domain
{
    public class PlanActividad
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public string Nombre { get; set; } = "";
        public int? NivelOpcionId { get; set; }
        public int? FrecuenciaOpcionId { get; set; }
        public int? ClasesOpcionId { get; set; }
        public string? Nivel { get; set; }
        public int? FrecuenciaSemanal { get; set; }
        public int? ClasesMes { get; set; }
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;

        public bool EsPorClases => ClasesMes.HasValue && ClasesMes.Value > 0;
        public bool EsPorTiempo => !EsPorClases;
        public string ModalidadTexto => EsPorClases ? "Por clases" : "Membresía 30 días";
        public string NivelTexto => string.IsNullOrWhiteSpace(Nivel) ? "Sin nivel" : Nivel!;
        public string FrecuenciaTexto => FrecuenciaSemanal.HasValue && FrecuenciaSemanal.Value > 0
            ? $"{FrecuenciaSemanal.Value}x/sem"
            : "Sin frecuencia";
        public string ClasesTexto => EsPorClases ? $"{ClasesMes!.Value} clases/mes" : "No usa clases";
        public string PrecioTexto => $"${Precio:0.##}";

        public string DescripcionCompleta
        {
            get
            {
                var detalles = new List<string>();

                if (!string.IsNullOrWhiteSpace(Nivel))
                    detalles.Add(Nivel!.Trim());

                if (FrecuenciaSemanal.HasValue && FrecuenciaSemanal.Value > 0)
                    detalles.Add($"{FrecuenciaSemanal.Value}x/sem");

                detalles.Add(EsPorClases ? $"{ClasesMes!.Value} clases/mes" : "30 días");

                return detalles.Count == 0
                    ? $"{Nombre} · {ModalidadTexto} · {PrecioTexto}"
                    : $"{Nombre} · {string.Join(" · ", detalles)} · {PrecioTexto}";
            }
        }

        public override string ToString() => DescripcionCompleta;
    }
}

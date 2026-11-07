namespace AsistenciaNatacion.Domain
{
    public class OpcionPlanActividad
    {
        public int Id { get; set; }
        public int ActividadId { get; set; }
        public string Tipo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public int? ValorEntero { get; set; }
        public bool Activo { get; set; } = true;

        public override string ToString()
        {
            return Nombre;
        }
    }
}

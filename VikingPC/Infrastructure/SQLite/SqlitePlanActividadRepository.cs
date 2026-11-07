using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.SQLite
{
    public class SqlitePlanActividadRepository : IPlanActividadRepository
    {
        private readonly string connectionString;
        public SqlitePlanActividadRepository(string connectionString) => this.connectionString = connectionString;

        private const string SelectBase = @"
            SELECT
                p.id, p.actividad_id, p.nombre,
                p.nivel_opcion_id, p.frecuencia_opcion_id, p.clases_opcion_id,
                COALESCE(NULLIF(TRIM(p.nivel), ''), nivel_op.nombre) AS nivel,
                COALESCE(p.frecuencia_semanal, frecuencia_op.valor_entero) AS frecuencia_semanal,
                COALESCE(p.clases_mes, clases_op.valor_entero) AS clases_mes,
                p.activo, p.precio
            FROM planes_actividad p
            LEFT JOIN opciones_plan_actividad nivel_op
                ON nivel_op.id = p.nivel_opcion_id
            LEFT JOIN opciones_plan_actividad frecuencia_op
                ON frecuencia_op.id = p.frecuencia_opcion_id
            LEFT JOIN opciones_plan_actividad clases_op
                ON clases_op.id = p.clases_opcion_id
            WHERE ";

        public List<PlanActividad> ObtenerPorActividad(int actividadId)
        {
            var planes = new List<PlanActividad>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectBase + "p.actividad_id = $actividadId AND p.activo = 1 ORDER BY p.id;";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) planes.Add(LeerPlan(reader));
            return planes;
        }

        public PlanActividad? ObtenerPorId(int id)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SelectBase + "p.id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? LeerPlan(reader) : null;
        }

        public int Crear(int actividadId, string nombre, string? nivel, int? frecuenciaSemanal, int? clasesMes, decimal precio)
        {
            return CrearInterno(actividadId, nombre, null, null, null, nivel, frecuenciaSemanal, clasesMes, precio);
        }

        public int Crear(int actividadId, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, decimal precio)
        {
            return CrearInterno(actividadId, nombre, nivelOpcionId, frecuenciaOpcionId, clasesOpcionId, null, null, null, precio);
        }

        private int CrearInterno(int actividadId, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, string? nivel, int? frecuenciaSemanal, int? clasesMes, decimal precio)
        {
            if (actividadId <= 0) throw new ArgumentException("La actividad no es válida.");
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre del plan es obligatorio.");
            if (precio < 0) throw new ArgumentException("El precio no puede ser negativo.");

            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO planes_actividad
                (actividad_id, nombre, nivel_opcion_id, frecuencia_opcion_id, clases_opcion_id, nivel, frecuencia_semanal, clases_mes, activo, precio)
                VALUES
                ($actividadId, $nombre, $nivelOpcionId, $frecuenciaOpcionId, $clasesOpcionId, $nivel, $frecuencia, $clases, 1, $precio);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$nombre", nombre.Trim());
            cmd.Parameters.AddWithValue("$nivelOpcionId", (object?)nivelOpcionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$frecuenciaOpcionId", (object?)frecuenciaOpcionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$clasesOpcionId", (object?)clasesOpcionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$nivel", (object?)nivel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$frecuencia", (object?)frecuenciaSemanal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$clases", (object?)clasesMes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$precio", precio);
            try
            {
                int planId = Convert.ToInt32(cmd.ExecuteScalar());
                using var cmdHist = conn.CreateCommand();
                cmdHist.CommandText = @"INSERT INTO planes_actividad_precio_historial(plan_id, anio, mes, precio) VALUES($planId,$anio,$mes,$precio) ON CONFLICT(plan_id, anio, mes) DO UPDATE SET precio=excluded.precio;";
                cmdHist.Parameters.AddWithValue("$planId", planId);
                cmdHist.Parameters.AddWithValue("$anio", DateTime.Today.Year);
                cmdHist.Parameters.AddWithValue("$mes", DateTime.Today.Month);
                cmdHist.Parameters.AddWithValue("$precio", precio);
                cmdHist.ExecuteNonQuery();
                return planId;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new ArgumentException("No fue posible crear el plan. Verifica las opciones seleccionadas.");
            }
        }

        public void Actualizar(int id, string nombre, string? nivel, int? frecuenciaSemanal, int? clasesMes, decimal precio)
        {
            ActualizarInterno(id, nombre, null, null, null, nivel, frecuenciaSemanal, clasesMes, precio);
        }

        public void Actualizar(int id, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, decimal precio)
        {
            ActualizarInterno(id, nombre, nivelOpcionId, frecuenciaOpcionId, clasesOpcionId, null, null, null, precio);
        }

        private void ActualizarInterno(int id, string nombre, int? nivelOpcionId, int? frecuenciaOpcionId, int? clasesOpcionId, string? nivel, int? frecuenciaSemanal, int? clasesMes, decimal precio)
        {
            if (id <= 0) throw new ArgumentException("El plan no es válido.");
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre del plan es obligatorio.");
            if (precio < 0) throw new ArgumentException("El precio no puede ser negativo.");

            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE planes_actividad
                SET nombre = $nombre,
                    nivel_opcion_id = $nivelOpcionId,
                    frecuencia_opcion_id = $frecuenciaOpcionId,
                    clases_opcion_id = $clasesOpcionId,
                    nivel = $nivel,
                    frecuencia_semanal = $frecuencia,
                    clases_mes = $clases,
                    precio = $precio
                WHERE id = $id;
            ";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$nombre", nombre.Trim());
            cmd.Parameters.AddWithValue("$nivelOpcionId", (object?)nivelOpcionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$frecuenciaOpcionId", (object?)frecuenciaOpcionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$clasesOpcionId", (object?)clasesOpcionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$nivel", (object?)nivel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$frecuencia", (object?)frecuenciaSemanal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$clases", (object?)clasesMes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$precio", precio);
            cmd.ExecuteNonQuery();

            using var cmdHist = conn.CreateCommand();
            cmdHist.CommandText = @"INSERT INTO planes_actividad_precio_historial(plan_id, anio, mes, precio) VALUES($planId,$anio,$mes,$precio) ON CONFLICT(plan_id, anio, mes) DO UPDATE SET precio=excluded.precio;";
            cmdHist.Parameters.AddWithValue("$planId", id);
            cmdHist.Parameters.AddWithValue("$anio", DateTime.Today.Year);
            cmdHist.Parameters.AddWithValue("$mes", DateTime.Today.Month);
            cmdHist.Parameters.AddWithValue("$precio", precio);
            cmdHist.ExecuteNonQuery();
        }

        public void Desactivar(int id)
        {
            if (id <= 0) throw new ArgumentException("El plan no es válido.");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE planes_actividad SET activo = 0 WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        private static PlanActividad LeerPlan(SqliteDataReader reader)
        {
            int n = reader.GetOrdinal("nivel");
            int f = reader.GetOrdinal("frecuencia_semanal");
            int c = reader.GetOrdinal("clases_mes");
            int no = reader.GetOrdinal("nivel_opcion_id");
            int fo = reader.GetOrdinal("frecuencia_opcion_id");
            int co = reader.GetOrdinal("clases_opcion_id");
            return new PlanActividad
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ActividadId = reader.GetInt32(reader.GetOrdinal("actividad_id")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                NivelOpcionId = reader.IsDBNull(no) ? null : reader.GetInt32(no),
                FrecuenciaOpcionId = reader.IsDBNull(fo) ? null : reader.GetInt32(fo),
                ClasesOpcionId = reader.IsDBNull(co) ? null : reader.GetInt32(co),
                Nivel = reader.IsDBNull(n) ? null : reader.GetString(n),
                FrecuenciaSemanal = reader.IsDBNull(f) ? null : reader.GetInt32(f),
                ClasesMes = reader.IsDBNull(c) ? null : reader.GetInt32(c),
                Activo = reader.GetInt32(reader.GetOrdinal("activo")) == 1,
                Precio = Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("precio")))
            };
        }
    }
}

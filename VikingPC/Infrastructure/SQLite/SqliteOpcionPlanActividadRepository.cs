using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion.Infrastructure.SQLite
{
    public class SqliteOpcionPlanActividadRepository : IOpcionPlanActividadRepository
    {
        private readonly string connectionString;

        public SqliteOpcionPlanActividadRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<OpcionPlanActividad> ObtenerPorActividad(int actividadId, string tipo)
        {
            var resultado = new List<OpcionPlanActividad>();
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, actividad_id, tipo, nombre, valor_entero, activo
                FROM opciones_plan_actividad
                WHERE actividad_id = $actividadId
                  AND tipo = $tipo
                  AND activo = 1
                ORDER BY COALESCE(valor_entero, 999999), nombre;
            ";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$tipo", tipo);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) resultado.Add(Leer(reader));
            return resultado;
        }

        public OpcionPlanActividad? ObtenerPorId(int id)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT id, actividad_id, tipo, nombre, valor_entero, activo FROM opciones_plan_actividad WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Leer(reader) : null;
        }

        public void AsegurarCatalogoBase(int actividadId)
        {
            if (actividadId <= 0)
                return;

            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            // Estos catálogos son genéricos para cualquier establecimiento.
            // Solo se agregan cuando la actividad todavía no tiene opciones del tipo.
            AsegurarFrecuencias(conn, actividadId);
            AsegurarClases(conn, actividadId);
            AsegurarNiveles(conn, actividadId);
            SincronizarPlanesLegacy(conn, actividadId);
        }

        private static void AsegurarFrecuencias(SqliteConnection conn, int actividadId)
        {
            if (ExisteTipo(conn, actividadId, "Frecuencia"))
                return;

            for (int frecuencia = 1; frecuencia <= 5; frecuencia++)
            {
                InsertarSiNoExiste(conn, actividadId, "Frecuencia", $"{frecuencia}x por semana", frecuencia);
            }
        }

        private static void AsegurarClases(SqliteConnection conn, int actividadId)
        {
            if (ExisteTipo(conn, actividadId, "Clases"))
                return;

            foreach (int clases in new[] { 4, 8, 12, 16, 20, 24 })
            {
                InsertarSiNoExiste(conn, actividadId, "Clases", $"{clases} clases", clases);
            }
        }

        private static void AsegurarNiveles(SqliteConnection conn, int actividadId)
        {
            if (ExisteTipo(conn, actividadId, "Nivel"))
                return;

            foreach (string nivel in new[] { "General", "Inicial", "Intermedio", "Avanzado" })
            {
                InsertarSiNoExiste(conn, actividadId, "Nivel", nivel, null);
            }
        }

        private static bool ExisteTipo(SqliteConnection conn, int actividadId, string tipo)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT 1 FROM opciones_plan_actividad WHERE actividad_id = $actividadId AND tipo = $tipo AND activo = 1 LIMIT 1;";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$tipo", tipo);
            return cmd.ExecuteScalar() != null;
        }

        private static void InsertarSiNoExiste(SqliteConnection conn, int actividadId, string tipo, string nombre, int? valorEntero)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO opciones_plan_actividad
                    (actividad_id, tipo, nombre, valor_entero, activo)
                VALUES
                    ($actividadId, $tipo, $nombre, $valor, 1);
            ";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$tipo", tipo);
            cmd.Parameters.AddWithValue("$nombre", nombre);
            cmd.Parameters.AddWithValue("$valor", (object?)valorEntero ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private static void SincronizarPlanesLegacy(SqliteConnection conn, int actividadId)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR IGNORE INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero, activo)
                SELECT DISTINCT actividad_id, 'Nivel', TRIM(nivel), NULL, 1
                FROM planes_actividad
                WHERE actividad_id = $actividadId
                  AND nivel IS NOT NULL
                  AND TRIM(nivel) <> '';

                INSERT OR IGNORE INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero, activo)
                SELECT DISTINCT actividad_id, 'Frecuencia',
                       CAST(frecuencia_semanal AS TEXT) || 'x por semana', frecuencia_semanal, 1
                FROM planes_actividad
                WHERE actividad_id = $actividadId
                  AND frecuencia_semanal IS NOT NULL
                  AND frecuencia_semanal > 0;

                INSERT OR IGNORE INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero, activo)
                SELECT DISTINCT actividad_id, 'Clases',
                       CAST(clases_mes AS TEXT) || ' clases', clases_mes, 1
                FROM planes_actividad
                WHERE actividad_id = $actividadId
                  AND clases_mes IS NOT NULL
                  AND clases_mes > 0;

                UPDATE planes_actividad
                SET nivel_opcion_id = COALESCE(
                        nivel_opcion_id,
                        (SELECT o.id FROM opciones_plan_actividad o
                         WHERE o.actividad_id = planes_actividad.actividad_id
                           AND o.tipo = 'Nivel'
                           AND o.nombre = TRIM(planes_actividad.nivel)
                         LIMIT 1)),
                    frecuencia_opcion_id = COALESCE(
                        frecuencia_opcion_id,
                        (SELECT o.id FROM opciones_plan_actividad o
                         WHERE o.actividad_id = planes_actividad.actividad_id
                           AND o.tipo = 'Frecuencia'
                           AND o.valor_entero = planes_actividad.frecuencia_semanal
                         LIMIT 1)),
                    clases_opcion_id = COALESCE(
                        clases_opcion_id,
                        (SELECT o.id FROM opciones_plan_actividad o
                         WHERE o.actividad_id = planes_actividad.actividad_id
                           AND o.tipo = 'Clases'
                           AND o.valor_entero = planes_actividad.clases_mes
                         LIMIT 1))
                WHERE actividad_id = $actividadId;

                UPDATE planes_actividad
                SET frecuencia_semanal = COALESCE(
                        frecuencia_semanal,
                        (SELECT o.valor_entero FROM opciones_plan_actividad o WHERE o.id = frecuencia_opcion_id)),
                    clases_mes = COALESCE(
                        clases_mes,
                        (SELECT o.valor_entero FROM opciones_plan_actividad o WHERE o.id = clases_opcion_id)),
                    nivel = COALESCE(
                        NULLIF(TRIM(nivel), ''),
                        (SELECT o.nombre FROM opciones_plan_actividad o WHERE o.id = nivel_opcion_id))
                WHERE actividad_id = $actividadId;
            ";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.ExecuteNonQuery();
        }

        public int Crear(int actividadId, string tipo, string nombre, int? valorEntero)
        {
            if (actividadId <= 0) throw new ArgumentException("La actividad no es válida.");
            if (string.IsNullOrWhiteSpace(tipo)) throw new ArgumentException("El tipo de opción es obligatorio.");
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre de la opción es obligatorio.");
            if (valorEntero.HasValue && valorEntero.Value <= 0) throw new ArgumentException("El valor debe ser mayor que cero.");

            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero, activo)
                VALUES ($actividadId, $tipo, $nombre, $valor, 1);
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$tipo", tipo.Trim());
            cmd.Parameters.AddWithValue("$nombre", nombre.Trim());
            cmd.Parameters.AddWithValue("$valor", (object?)valorEntero ?? DBNull.Value);
            try
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new ArgumentException("Ya existe una opción con ese nombre para esta actividad y tipo.");
            }
        }

        public void Actualizar(int id, string nombre, int? valorEntero)
        {
            if (id <= 0) throw new ArgumentException("La opción no es válida.");
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre de la opción es obligatorio.");
            if (valorEntero.HasValue && valorEntero.Value <= 0) throw new ArgumentException("El valor debe ser mayor que cero.");

            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE opciones_plan_actividad SET nombre = $nombre, valor_entero = $valor WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.Parameters.AddWithValue("$nombre", nombre.Trim());
            cmd.Parameters.AddWithValue("$valor", (object?)valorEntero ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void Desactivar(int id)
        {
            if (id <= 0) throw new ArgumentException("La opción no es válida.");
            using var conn = new SqliteConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE opciones_plan_actividad SET activo = 0 WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        private static OpcionPlanActividad Leer(SqliteDataReader reader)
        {
            int valor = reader.GetOrdinal("valor_entero");
            return new OpcionPlanActividad
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ActividadId = reader.GetInt32(reader.GetOrdinal("actividad_id")),
                Tipo = reader.GetString(reader.GetOrdinal("tipo")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                ValorEntero = reader.IsDBNull(valor) ? null : reader.GetInt32(valor),
                Activo = reader.GetInt32(reader.GetOrdinal("activo")) == 1
            };
        }
    }
}

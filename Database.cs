using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Domain;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace AsistenciaNatacion
{
    // Clase DTO para las métricas
    public class MetricasResumen
    {
        public int AlumnosActivos { get; set; }
        public int AsistenciasHoy { get; set; }
        public int PlanesPorVencer { get; set; }
        public decimal RecaudadoMes { get; set; }
        public decimal RecaudadoHoy { get; set; }
    }

    public static class Database
    {
        public static readonly int[] PlanesDisponibles = { 4, 3, 2, 1 };

        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "asistencia_natacion.db");

        private static string ConnectionString => $"Data Source={DbPath}";

        private static int ActividadActualId =>
            ContextoActividad.ActividadId > 0
                ? ContextoActividad.ActividadId
                : 1;

        public static string ObtenerConnectionString()
        {
            return ConnectionString;
        }

        public static void Inicializar()
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS alumnos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    nombre TEXT NOT NULL,
                    telefono TEXT,
                    cedula TEXT,
                    codigo TEXT UNIQUE,
                    fecha_inscripcion TEXT NOT NULL,
                    plan_veces_semana INTEGER NOT NULL,
                    clases_mes INTEGER NOT NULL,
                    activo INTEGER NOT NULL DEFAULT 1,
                    actividad_id INTEGER NOT NULL DEFAULT 1,
                    plan_id INTEGER NOT NULL DEFAULT 0,
                    fecha_inicio_plan TEXT,
                    fecha_vencimiento_plan TEXT
                );

                CREATE TABLE IF NOT EXISTS asistencias (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    alumno_id INTEGER NOT NULL,
                    fecha TEXT NOT NULL,
                    presente INTEGER NOT NULL DEFAULT 1,
                    UNIQUE(alumno_id, fecha),
                    FOREIGN KEY (alumno_id) REFERENCES alumnos(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS pagos (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    alumno_id INTEGER NOT NULL,
                    anio INTEGER NOT NULL,
                    mes INTEGER NOT NULL,
                    monto DECIMAL NOT NULL DEFAULT 0,
                    fecha_pago TEXT NOT NULL,
                    metodo_pago TEXT DEFAULT 'Efectivo ($)',
                    referencia TEXT DEFAULT '',
                    FOREIGN KEY (alumno_id) REFERENCES alumnos(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS precios_plan (
                    plan_veces_semana INTEGER PRIMARY KEY,
                    precio REAL NOT NULL
                );

                CREATE TABLE IF NOT EXISTS precios_plan_historial (
                    plan_veces_semana INTEGER NOT NULL,
                    anio INTEGER NOT NULL,
                    mes INTEGER NOT NULL,
                    precio REAL NOT NULL,
                    PRIMARY KEY (plan_veces_semana, anio, mes)
                );

                CREATE TABLE IF NOT EXISTS auditoria (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    fecha TEXT NOT NULL,
                    usuario TEXT NOT NULL,
                    accion TEXT NOT NULL,
                    detalle TEXT NOT NULL
                );
                
                CREATE TABLE IF NOT EXISTS configuracion_establecimiento (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    nombre TEXT NOT NULL,
    identificacion TEXT DEFAULT '',
    tipo_establecimiento TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS configuracion_operador (
    id INTEGER PRIMARY KEY,
    nombre TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS actividades (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    nombre TEXT NOT NULL,
    activa INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS planes_actividad (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    actividad_id INTEGER NOT NULL,
    nombre TEXT NOT NULL,
    nivel_opcion_id INTEGER,
    frecuencia_opcion_id INTEGER,
    clases_opcion_id INTEGER,
    nivel TEXT,
    frecuencia_semanal INTEGER,
    clases_mes INTEGER,
    activo INTEGER NOT NULL DEFAULT 1,
    precio REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (actividad_id) REFERENCES actividades(id),
    FOREIGN KEY (nivel_opcion_id) REFERENCES opciones_plan_actividad(id),
    FOREIGN KEY (frecuencia_opcion_id) REFERENCES opciones_plan_actividad(id),
    FOREIGN KEY (clases_opcion_id) REFERENCES opciones_plan_actividad(id)
);

CREATE TABLE IF NOT EXISTS opciones_plan_actividad (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    actividad_id INTEGER NOT NULL,
    tipo TEXT NOT NULL,
    nombre TEXT NOT NULL,
    valor_entero INTEGER,
    activo INTEGER NOT NULL DEFAULT 1,
    UNIQUE(actividad_id, tipo, nombre),
    FOREIGN KEY (actividad_id) REFERENCES actividades(id)
);

CREATE TABLE IF NOT EXISTS planes_actividad_precio_historial (
    plan_id INTEGER NOT NULL,
    anio INTEGER NOT NULL,
    mes INTEGER NOT NULL,
    precio REAL NOT NULL,
    PRIMARY KEY(plan_id, anio, mes),
    FOREIGN KEY(plan_id) REFERENCES planes_actividad(id)
);

CREATE TABLE IF NOT EXISTS precios_plan_actividad (
    actividad_id INTEGER NOT NULL,
    plan_veces_semana INTEGER NOT NULL,
    precio REAL NOT NULL,
    PRIMARY KEY (actividad_id, plan_veces_semana)
);

CREATE TABLE IF NOT EXISTS precios_plan_historial_actividad (
    actividad_id INTEGER NOT NULL,
    plan_veces_semana INTEGER NOT NULL,
    anio INTEGER NOT NULL,
    mes INTEGER NOT NULL,
    precio REAL NOT NULL,
    PRIMARY KEY (actividad_id, plan_veces_semana, anio, mes)
);

CREATE TABLE IF NOT EXISTS configuracion_operador_actividad (
    actividad_id INTEGER PRIMARY KEY,
    nombre TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS configuracion_operador_actividad_rol (
    actividad_id INTEGER NOT NULL,
    rol TEXT NOT NULL,
    nombre TEXT NOT NULL,
    PRIMARY KEY (actividad_id, rol)
);
            ";
            cmd.ExecuteNonQuery();

            var cmdConfiguracion = conn.CreateCommand();

            cmdConfiguracion.CommandText = @"
    INSERT OR IGNORE INTO configuracion_establecimiento
    (
        id,
        nombre,
        identificacion,
        tipo_establecimiento
    )
    VALUES
    (
        1,
        'ACADEMIA DE NATACIÓN',
        '',
        'Natación'
    );
";

            cmdConfiguracion.ExecuteNonQuery();

            // Migración automática: Agrega las columnas si la BD ya existía previamente
            AplicarMigraciones(conn);
            MigrarFechasMembresia(conn);
            var cmdActividad = conn.CreateCommand();
            cmdActividad.CommandText = @"
    INSERT OR IGNORE INTO actividades (id, nombre, activa)
    VALUES (1, 'Natación', 1);
";
            cmdActividad.ExecuteNonQuery();

            var cmdPlanesActividad = conn.CreateCommand();

            cmdPlanesActividad.CommandText = @"
    INSERT OR IGNORE INTO planes_actividad
    (
        id,
        actividad_id,
        nombre,
        frecuencia_semanal,
        clases_mes,
        activo,
        precio
    )
           
    VALUES
        (1, 1, '1x por semana', 1, 4, 1, 15),
        (2, 1, '2x por semana', 2, 8, 1, 20),
        (3, 1, '3x por semana', 3, 12, 1, 30),
        (4, 1, '4x por semana', 4, 16, 1, 35);
";

            cmdPlanesActividad.ExecuteNonQuery();

            // Migración de datos globales existentes hacia la actividad Natación.
            using (var cmdMigracionActividad = conn.CreateCommand())
            {
                cmdMigracionActividad.CommandText = @"
                    INSERT OR IGNORE INTO precios_plan_actividad
                        (actividad_id, plan_veces_semana, precio)
                    SELECT 1, plan_veces_semana, precio FROM precios_plan;

                    INSERT OR IGNORE INTO precios_plan_historial_actividad
                        (actividad_id, plan_veces_semana, anio, mes, precio)
                    SELECT 1, plan_veces_semana, anio, mes, precio
                    FROM precios_plan_historial;

                    INSERT OR IGNORE INTO configuracion_operador_actividad
                        (actividad_id, nombre)
                    SELECT 1, nombre
                    FROM configuracion_operador
                    WHERE id = 1;

                    INSERT OR IGNORE INTO configuracion_operador_actividad_rol
                        (actividad_id, rol, nombre)
                    SELECT 1, 'Recepción', nombre
                    FROM configuracion_operador
                    WHERE id = 1;

                    INSERT OR IGNORE INTO configuracion_operador_actividad_rol
                        (actividad_id, rol, nombre)
                    SELECT actividad_id, 'Recepción', nombre
                    FROM configuracion_operador_actividad;
                ";
                try
                {
                    cmdMigracionActividad.ExecuteNonQuery();
                }
                catch { }
            }

            // Precios iniciales
            var cmdPrecios = conn.CreateCommand();
            cmdPrecios.CommandText = @"
                INSERT OR IGNORE INTO precios_plan (plan_veces_semana, precio) VALUES
                    (4, 35), (3, 30), (2, 20), (1, 15);

                INSERT OR IGNORE INTO precios_plan_historial (plan_veces_semana, anio, mes, precio) VALUES
                    (4, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 35),
                    (3, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 30),
                    (2, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 20),
                    (1, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 15);

                INSERT OR IGNORE INTO precios_plan_actividad (actividad_id, plan_veces_semana, precio) VALUES
                    (1, 4, 35), (1, 3, 30), (1, 2, 20), (1, 1, 15);

                INSERT OR IGNORE INTO precios_plan_historial_actividad
                    (actividad_id, plan_veces_semana, anio, mes, precio) VALUES
                    (1, 4, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 35),
                    (1, 3, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 30),
                    (1, 2, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 20),
                    (1, 1, CAST(strftime('%Y', 'now') AS INT), CAST(strftime('%m', 'now') AS INT), 15);
            ";
            cmdPrecios.ExecuteNonQuery();

            MigrarCatalogosYPlanes(conn);
        }

        private static void AsegurarColumna(SqliteConnection conn, string tabla, string columna, string definicionColumna)
        {
            try
            {
                var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"PRAGMA table_info({tabla});";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        columnas.Add(reader.GetString(1));
                    }
                }

                if (!columnas.Contains(columna))
                {
                    using var cmdAdd = conn.CreateCommand();
                    cmdAdd.CommandText = $"ALTER TABLE {tabla} ADD COLUMN {columna} {definicionColumna};";
                    cmdAdd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Si la tabla no existe aún o el PRAGMA falla, continuar de forma segura
            }
        }

        private static void AplicarMigraciones(SqliteConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS precios_plan_historial (
                        plan_veces_semana INTEGER NOT NULL,
                        anio INTEGER NOT NULL,
                        mes INTEGER NOT NULL,
                        precio REAL NOT NULL,
                        PRIMARY KEY (plan_veces_semana, anio, mes)
                    );

                    CREATE TABLE IF NOT EXISTS auditoria (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        fecha TEXT NOT NULL,
                        usuario TEXT NOT NULL,
                        accion TEXT NOT NULL,
                        detalle TEXT NOT NULL
                    );
                ";
                cmd.ExecuteNonQuery();
            }

            // Asegurar columnas requeridas en alumnos
            AsegurarColumna(conn, "alumnos", "plan_veces_semana", "INTEGER NOT NULL DEFAULT 0");
            AsegurarColumna(conn, "alumnos", "actividad_id", "INTEGER NOT NULL DEFAULT 1");
            AsegurarColumna(conn, "alumnos", "plan_id", "INTEGER NOT NULL DEFAULT 0");
            AsegurarColumna(conn, "alumnos", "clases_mes", "INTEGER NOT NULL DEFAULT 0");
            AsegurarColumna(conn, "alumnos", "fecha_inicio_plan", "TEXT");
            AsegurarColumna(conn, "alumnos", "fecha_vencimiento_plan", "TEXT");

            // Asegurar columnas requeridas en pagos
            AsegurarColumna(conn, "pagos", "metodo_pago", "TEXT DEFAULT 'Efectivo ($)'");
            AsegurarColumna(conn, "pagos", "referencia", "TEXT DEFAULT ''");
            AsegurarColumna(conn, "pagos", "tasa_bcv", "DECIMAL NOT NULL DEFAULT 0");
            AsegurarColumna(conn, "pagos", "monto_bs", "DECIMAL NOT NULL DEFAULT 0");
            AsegurarColumna(conn, "pagos", "folio", "TEXT DEFAULT ''");

            // Asegurar columnas requeridas en planes_actividad
            AsegurarColumna(conn, "planes_actividad", "nivel", "TEXT");
            AsegurarColumna(conn, "planes_actividad", "frecuencia_semanal", "INTEGER");
            AsegurarColumna(conn, "planes_actividad", "nivel_opcion_id", "INTEGER");
            AsegurarColumna(conn, "planes_actividad", "frecuencia_opcion_id", "INTEGER");
            AsegurarColumna(conn, "planes_actividad", "clases_opcion_id", "INTEGER");

            // Asegurar columnas requeridas en auditoria
            AsegurarColumna(conn, "auditoria", "actividad_id", "INTEGER NOT NULL DEFAULT 1");

            try
            {
                using var cmdClases = conn.CreateCommand();
                cmdClases.CommandText = @"
                    UPDATE alumnos
                    SET clases_mes =
                        CASE plan_veces_semana
                            WHEN 1 THEN 4
                            WHEN 2 THEN 8
                            WHEN 3 THEN 12
                            WHEN 4 THEN 16
                            ELSE clases_mes
                        END
                    WHERE clases_mes = 0;
                ";
                cmdClases.ExecuteNonQuery();
            }
            catch
            {
            }
        }

        private static void MigrarFechasMembresia(SqliteConnection conn)
        {
            try
            {
                // Desde VIKING 1.1 todos los alumnos tienen un ciclo individual
                // de pago. Se reutilizan las columnas históricas de fechas del plan
                // para mantener compatibilidad con bases de datos existentes.
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE alumnos
                    SET fecha_inicio_plan =
                            COALESCE(
                                fecha_inicio_plan,
                                fecha_inscripcion
                            ),
                        fecha_vencimiento_plan =
                            COALESCE(
                                fecha_vencimiento_plan,
                                date(
                                    COALESCE(
                                        fecha_inicio_plan,
                                        fecha_inscripcion
                                    ),
                                    '+1 month'
                                )
                            )
                    WHERE fecha_inicio_plan IS NULL
                       OR fecha_vencimiento_plan IS NULL;";
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // La migración debe ser tolerante para no impedir el arranque.
            }
        }

        private static void MigrarCatalogosYPlanes(SqliteConnection conn)
        {
            try
            {
                using var cmdLegacy = conn.CreateCommand();
                cmdLegacy.CommandText = @"
                    UPDATE planes_actividad SET frecuencia_semanal = 1 WHERE id = 1 AND actividad_id = 1 AND frecuencia_semanal IS NULL;
                    UPDATE planes_actividad SET frecuencia_semanal = 2 WHERE id = 2 AND actividad_id = 1 AND frecuencia_semanal IS NULL;
                    UPDATE planes_actividad SET frecuencia_semanal = 3 WHERE id = 3 AND actividad_id = 1 AND frecuencia_semanal IS NULL;
                    UPDATE planes_actividad SET frecuencia_semanal = 4 WHERE id = 4 AND actividad_id = 1 AND frecuencia_semanal IS NULL;
                ";
                cmdLegacy.ExecuteNonQuery();
            }
            catch { }

            try
            {
                using var cmdOpciones = conn.CreateCommand();
                cmdOpciones.CommandText = @"
                    INSERT OR IGNORE INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero)
                    SELECT DISTINCT actividad_id, 'Nivel', nivel, NULL
                    FROM planes_actividad
                    WHERE nivel IS NOT NULL AND TRIM(nivel) <> '';

                    INSERT OR IGNORE INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero)
                    SELECT DISTINCT actividad_id, 'Frecuencia', CAST(frecuencia_semanal AS TEXT) || 'x/semana', frecuencia_semanal
                    FROM planes_actividad
                    WHERE frecuencia_semanal IS NOT NULL AND frecuencia_semanal > 0;

                    INSERT OR IGNORE INTO opciones_plan_actividad (actividad_id, tipo, nombre, valor_entero)
                    SELECT DISTINCT actividad_id, 'Clases', CAST(clases_mes AS TEXT) || ' clases', clases_mes
                    FROM planes_actividad
                    WHERE clases_mes IS NOT NULL AND clases_mes > 0;
                ";
                cmdOpciones.ExecuteNonQuery();
            }
            catch { }

            try
            {
                using var cmdMap = conn.CreateCommand();
                cmdMap.CommandText = @"
                    UPDATE planes_actividad
                    SET frecuencia_semanal = CASE WHEN frecuencia_semanal IS NULL AND frecuencia_opcion_id IS NOT NULL THEN (SELECT valor_entero FROM opciones_plan_actividad o WHERE o.id = frecuencia_opcion_id) ELSE frecuencia_semanal END,
                        clases_mes = CASE WHEN clases_mes IS NULL AND clases_opcion_id IS NOT NULL THEN (SELECT valor_entero FROM opciones_plan_actividad o WHERE o.id = clases_opcion_id) ELSE clases_mes END,
                        nivel = CASE WHEN (nivel IS NULL OR TRIM(nivel) = '') AND nivel_opcion_id IS NOT NULL THEN (SELECT nombre FROM opciones_plan_actividad o WHERE o.id = nivel_opcion_id) ELSE nivel END;

                    UPDATE planes_actividad
                    SET nivel_opcion_id = COALESCE(nivel_opcion_id, (SELECT o.id FROM opciones_plan_actividad o WHERE o.actividad_id = planes_actividad.actividad_id AND o.tipo = 'Nivel' AND o.nombre = planes_actividad.nivel LIMIT 1)),
                        frecuencia_opcion_id = COALESCE(frecuencia_opcion_id, (SELECT o.id FROM opciones_plan_actividad o WHERE o.actividad_id = planes_actividad.actividad_id AND o.tipo = 'Frecuencia' AND o.valor_entero = planes_actividad.frecuencia_semanal LIMIT 1)),
                        clases_opcion_id = COALESCE(clases_opcion_id, (SELECT o.id FROM opciones_plan_actividad o WHERE o.actividad_id = planes_actividad.actividad_id AND o.tipo = 'Clases' AND o.valor_entero = planes_actividad.clases_mes LIMIT 1));

                    UPDATE alumnos
                    SET plan_id = COALESCE((
                        SELECT p.id FROM planes_actividad p
                        WHERE p.actividad_id = alumnos.actividad_id
                          AND p.frecuencia_semanal = alumnos.plan_veces_semana
                          AND p.activo = 1
                        ORDER BY p.id
                        LIMIT 1
                    ), plan_id)
                    WHERE plan_id = 0;

                    INSERT OR IGNORE INTO planes_actividad_precio_historial(plan_id, anio, mes, precio)
                    SELECT p.id, h.anio, h.mes, h.precio
                    FROM planes_actividad p
                    JOIN precios_plan_historial_actividad h
                      ON h.actividad_id = p.actividad_id
                     AND h.plan_veces_semana = p.frecuencia_semanal
                    WHERE p.frecuencia_semanal IS NOT NULL;
                ";
                cmdMap.ExecuteNonQuery();
            }
            catch { }
        }

        // ---------- ALUMNOS Y ASISTENCIAS ----------

        public static int AgregarAlumno(string nombre, string? telefono, string? cedula, string codigo, int planId, int actividadId)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre del alumno es obligatorio");
            if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código es obligatorio");
            if (planId <= 0) throw new ArgumentException("Debes seleccionar un plan válido.");

            var plan = ObtenerPlanActividad(planId);
            if (plan == null || plan.ActividadId != actividadId || !plan.Activo)
                throw new ArgumentException("El plan seleccionado no pertenece a la actividad actual.");

            int planVecesSemana = plan.FrecuenciaSemanal ?? 0;
            int clasesMes = plan.ClasesMes ?? 0;

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
               INSERT INTO alumnos (nombre, telefono, cedula, codigo, fecha_inscripcion, plan_veces_semana, clases_mes, activo, actividad_id, plan_id, fecha_inicio_plan, fecha_vencimiento_plan)
               VALUES ($nombre, $telefono, $cedula, $codigo, $fecha, $plan, $clases, 1, $actividadId, $planId,
                       CASE WHEN $clases > 0 THEN NULL ELSE $fecha END,
                       CASE WHEN $clases > 0 THEN NULL ELSE date($fecha, '+30 day') END);
               SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("$nombre", nombre.Trim());
            cmd.Parameters.AddWithValue("$telefono", (object?)telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cedula", (object?)cedula ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$codigo", codigo.Trim());
            cmd.Parameters.AddWithValue("$fecha", DateTime.Today.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$plan", planVecesSemana);
            cmd.Parameters.AddWithValue("$clases", clasesMes);
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$planId", planId);
            try
            {
                int nuevoId = Convert.ToInt32(cmd.ExecuteScalar());
                RegistrarAuditoria("ALTA_ALUMNO", $"Creó al alumno '{nombre.Trim()}' (Código: {codigo.Trim()}) con plan '{plan.Nombre}'");
                return nuevoId;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new ArgumentException($"Ya existe un alumno con el código \"{codigo}\". Usa otro código.");
            }
        }

        public static int AgregarAlumno(string nombre, string? telefono, string? cedula, string codigo, int planVecesSemana, int clasesMes, int actividadId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id FROM planes_actividad
                WHERE actividad_id = $actividadId AND activo = 1
                  AND frecuencia_semanal = $frecuencia
                  AND (clases_mes = $clases OR clases_mes IS NULL)
                ORDER BY id LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            cmd.Parameters.AddWithValue("$frecuencia", planVecesSemana);
            cmd.Parameters.AddWithValue("$clases", clasesMes);
            var result = cmd.ExecuteScalar();
            if (result == null) throw new ArgumentException("No se encontró un plan compatible con esos datos.");
            return AgregarAlumno(nombre, telefono, cedula, codigo, Convert.ToInt32(result), actividadId);
        }

        public static void ActualizarAlumno(int alumnoId, int planId, string? telefono = null)
        {
            var alumno = ObtenerAlumno(alumnoId) ?? throw new ArgumentException("El alumno no existe.");
            var plan = ObtenerPlanActividad(planId) ?? throw new ArgumentException("El plan seleccionado no existe.");
            if (plan.ActividadId != alumno.ActividadId || !plan.Activo) throw new ArgumentException("El plan no pertenece a la actividad del alumno.");

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE alumnos
                SET plan_veces_semana = $plan, clases_mes = $clases, plan_id = $planId, telefono = $telefono,
                    fecha_inicio_plan = CASE WHEN $clases > 0 THEN NULL WHEN fecha_inicio_plan IS NULL THEN date('now') ELSE fecha_inicio_plan END,
                    fecha_vencimiento_plan = CASE WHEN $clases > 0 THEN NULL WHEN fecha_vencimiento_plan IS NULL THEN date('now', '+30 day') ELSE fecha_vencimiento_plan END
                WHERE id = $id;";
            cmd.Parameters.AddWithValue("$plan", plan.FrecuenciaSemanal ?? 0);
            cmd.Parameters.AddWithValue("$clases", plan.ClasesMes ?? 0);
            cmd.Parameters.AddWithValue("$planId", planId);
            cmd.Parameters.AddWithValue("$telefono", (object?)telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$id", alumnoId);
            cmd.ExecuteNonQuery();

            RegistrarAuditoria("EDITAR_ALUMNO", $"Actualizó datos de '{alumno.Nombre}' al plan '{plan.Nombre}'");
        }

        public static void ActualizarAlumno(int alumnoId, int planVecesSemana, int clasesMes, string? telefono = null)
        {
            var alumno = ObtenerAlumno(alumnoId) ?? throw new ArgumentException("El alumno no existe.");
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var find = conn.CreateCommand();
            find.CommandText = @"SELECT id FROM planes_actividad WHERE actividad_id = $actividadId AND activo = 1 AND frecuencia_semanal = $frecuencia ORDER BY CASE WHEN clases_mes = $clases THEN 0 ELSE 1 END, id LIMIT 1;";
            find.Parameters.AddWithValue("$actividadId", alumno.ActividadId);
            find.Parameters.AddWithValue("$frecuencia", planVecesSemana);
            find.Parameters.AddWithValue("$clases", clasesMes);
            var planId = find.ExecuteScalar();
            if (planId == null) throw new ArgumentException("No se encontró un plan compatible con esos datos.");
            ActualizarAlumno(alumnoId, Convert.ToInt32(planId), telefono);
        }

        public static PlanActividad? ObtenerPlanActividad(int planId)
        {
            var repo = new Infrastructure.SQLite.SqlitePlanActividadRepository(ConnectionString);
            return repo.ObtenerPorId(planId);
        }

        public static string ObtenerDescripcionPlan(Alumno alumno)
        {
            var plan = ObtenerPlanActividad(alumno.PlanId);
            if (plan == null) return alumno.PlanVecesSemana > 0 ? $"{alumno.PlanVecesSemana}x por semana" : "Sin plan";

            return plan.DescripcionCompleta;
        }

        public static void EliminarAlumno(int alumnoId)
        {
            var alumno = ObtenerAlumno(alumnoId);
            string nom = alumno != null ? alumno.Nombre : $"ID {alumnoId}";

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE alumnos SET activo = 0 WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", alumnoId);
            cmd.ExecuteNonQuery();

            RegistrarAuditoria("DESACTIVAR_ALUMNO", $"Dio de baja al alumno '{nom}'");
        }

        public static List<Alumno> ListarAlumnos(bool soloActivos = true, int? actividadId = null)
        {
            actividadId ??= ActividadActualId;
            var lista = new List<Alumno>();

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();

            string sql = soloActivos
                ? "SELECT * FROM alumnos WHERE activo = 1"
                : "SELECT * FROM alumnos WHERE 1 = 1";

            if (actividadId.HasValue)
            {
                sql += " AND actividad_id = $actividadId";
                cmd.Parameters.AddWithValue("$actividadId", actividadId.Value);
            }

            sql += " ORDER BY nombre";

            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
                lista.Add(LeerAlumno(reader));

            return lista;
        }

        public static Alumno? ObtenerAlumno(int alumnoId)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM alumnos WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", alumnoId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return LeerAlumno(reader);
            return null;
        }

        public static Alumno? ObtenerAlumnoPorCodigo(string codigo)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM alumnos WHERE codigo = $codigo AND activo = 1 AND actividad_id = $actividadId";
            cmd.Parameters.AddWithValue("$actividadId", ActividadActualId);
            cmd.Parameters.AddWithValue("$codigo", codigo.Trim());

            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return LeerAlumno(reader);
            return null;
        }

        public static string SiguienteCodigoSugerido()
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM alumnos";
            var totalExistente = Convert.ToInt32(cmd.ExecuteScalar());
            return $"A{totalExistente + 1:D3}";
        }

        private static Alumno LeerAlumno(SqliteDataReader reader)
        {
            return new Alumno
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ActividadId = reader.GetInt32(reader.GetOrdinal("actividad_id")),
                Nombre = reader.GetString(reader.GetOrdinal("nombre")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("telefono")) ? null : reader.GetString(reader.GetOrdinal("telefono")),
                Cedula = reader.IsDBNull(reader.GetOrdinal("cedula")) ? null : reader.GetString(reader.GetOrdinal("cedula")),
                Codigo = reader.IsDBNull(reader.GetOrdinal("codigo")) ? "" : reader.GetString(reader.GetOrdinal("codigo")),
                FechaInscripcion = reader.GetString(reader.GetOrdinal("fecha_inscripcion")),
                PlanVecesSemana = reader.GetInt32(reader.GetOrdinal("plan_veces_semana")),
                ClasesMes = reader.GetInt32(reader.GetOrdinal("clases_mes")),
                PlanId = reader.GetInt32(reader.GetOrdinal("plan_id")),
                Activo = reader.GetInt32(reader.GetOrdinal("activo")) == 1,
                FechaInicioPlan = LeerFechaNullable(reader, "fecha_inicio_plan"),
                FechaVencimientoPlan = LeerFechaNullable(reader, "fecha_vencimiento_plan")
            };
        }

        private static DateTime? LeerFechaNullable(SqliteDataReader reader, string nombre)
        {
            int ordinal = reader.GetOrdinal(nombre);
            if (reader.IsDBNull(ordinal)) return null;
            return DateTime.TryParse(reader.GetString(ordinal), out var fecha) ? fecha : null;
        }

        public static void MarcarAsistencia(int alumnoId, DateTime fecha, bool presente)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO asistencias (alumno_id, fecha, presente)
                VALUES ($alumnoId, $fecha, $presente)
                ON CONFLICT(alumno_id, fecha) DO UPDATE SET presente = excluded.presente
            ";
            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue("$fecha", fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$presente", presente ? 1 : 0);
            cmd.ExecuteNonQuery();

            var alumno = ObtenerAlumno(alumnoId);
            string nom = alumno != null ? alumno.Nombre : $"ID {alumnoId}";
            RegistrarAuditoria("ASISTENCIA", $"{(presente ? "Marcó PRESENTE" : "Marcó AUSENTE")} a '{nom}' para el {fecha:dd/MM/yyyy}");
        }

        public static int ContarAsistenciasMes(int alumnoId, int anio, int mes)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT COUNT(*) FROM asistencias
                WHERE alumno_id = $alumnoId AND fecha LIKE $prefijo AND presente = 1
            ";
            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue("$prefijo", $"{anio:D4}-{mes:D2}%");
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }

        // ---------- PROCESAMIENTO OFFLINE / CARGA MASIVA ----------

        public static (int exito, int fallidos) CargarAsistenciasDesdeArchivo(string rutaArchivo, DateTime fecha)
        {
            int exito = 0;
            int fallidos = 0;

            if (!File.Exists(rutaArchivo)) return (0, 0);

            string[] lineas = File.ReadAllLines(rutaArchivo);

            foreach (string linea in lineas)
            {
                string codigoLimpio = linea.Replace("\"", "").Replace(",", "").Trim();
                if (string.IsNullOrWhiteSpace(codigoLimpio)) continue;

                var alumno = ObtenerAlumnoPorCodigo(codigoLimpio);
                if (alumno != null)
                {
                    MarcarAsistencia(alumno.Id, fecha, true);
                    exito++;
                }
                else
                {
                    fallidos++;
                }
            }

            return (exito, fallidos);
        }

        // ---------- SISTEMA DE ABONOS Y PAGOS ----------

        public static decimal ObtenerMontoAbonado(int alumnoId, int anio, int mes)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT SUM(monto) FROM pagos
                WHERE alumno_id = $alumnoId AND anio = $anio AND mes = $mes
            ";
            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue("$anio", anio);
            cmd.Parameters.AddWithValue("$mes", mes);

            var result = cmd.ExecuteScalar();
            return (result == DBNull.Value || result == null) ? 0m : Convert.ToDecimal(result);
        }

        public static decimal ObtenerMontoAbonadoDesdeFecha(int alumnoId, DateTime fechaInicio)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT COALESCE(SUM(monto),0) FROM pagos WHERE alumno_id=$alumnoId AND datetime(fecha_pago) >= datetime($fecha);";
            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue("$fecha", fechaInicio.ToString("yyyy-MM-dd HH:mm:ss"));
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        public static (bool existe, string detalle) ExisteReferencia(string referencia)
        {
            if (string.IsNullOrWhiteSpace(referencia)) return (false, "");

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT p.fecha_pago, p.monto, a.nombre 
                FROM pagos p
                INNER JOIN alumnos a ON p.alumno_id = a.id
                WHERE LOWER(TRIM(p.referencia)) = LOWER(TRIM($ref)) AND p.referencia != ''
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$ref", referencia.Trim());

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string fechaRaw = reader.GetString(0);
                string fechaStr = DateTime.TryParse(fechaRaw, out DateTime f) ? f.ToString("dd/MM/yyyy") : fechaRaw;
                decimal montoVal = reader.GetDecimal(1);
                string alumnoNom = reader.GetString(2);

                string detalle = $"Registrado el {fechaStr} por ${montoVal:0.##} a nombre de '{alumnoNom}'.";
                return (true, detalle);
            }

            return (false, "");
        }

        public static void RegistrarAbono(int alumnoId, int anio, int mes, decimal monto, string metodoPago = "Efectivo ($)", string referencia = "")
        {
            if (monto <= 0) return;

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO pagos (alumno_id, anio, mes, monto, fecha_pago, metodo_pago, referencia)
                VALUES ($alumnoId, $anio, $mes, $monto, $fecha, $metodo, $referencia)
            ";
            cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
            cmd.Parameters.AddWithValue("$anio", anio);
            cmd.Parameters.AddWithValue("$mes", mes);
            cmd.Parameters.AddWithValue("$monto", monto);
            cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("$metodo", metodoPago);
            cmd.Parameters.AddWithValue("$referencia", referencia.Trim());
            cmd.ExecuteNonQuery();

            var alumno = ObtenerAlumno(alumnoId);
            string nom = alumno != null ? alumno.Nombre : $"ID {alumnoId}";
            string refTexto = string.IsNullOrWhiteSpace(referencia) ? "" : $" (Ref: {referencia.Trim()})";
            RegistrarAuditoria("PAGO_ABONO", $"Registró abono de ${monto:0.##} ({metodoPago}){refTexto} a '{nom}' para el período {mes:D2}/{anio}");
        }

        // ---------- PRECIOS ----------

        public static decimal CalcularCuotaPorPlan(int planId, int? anio = null, int? mes = null)
        {
            if (planId <= 0) return 0m;
            int actividadId = ActividadActualId;
            int a = anio ?? DateTime.Today.Year;
            int m = mes ?? DateTime.Today.Month;
            int periodo = a * 100 + m;

            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using (var hist = conn.CreateCommand())
            {
                hist.CommandText = @"SELECT precio FROM planes_actividad_precio_historial WHERE plan_id = $planId AND (anio * 100 + mes) <= $periodo ORDER BY anio DESC, mes DESC LIMIT 1;";
                hist.Parameters.AddWithValue("$planId", planId);
                hist.Parameters.AddWithValue("$periodo", periodo);
                var r = hist.ExecuteScalar();
                if (r != null && r != DBNull.Value) return Convert.ToDecimal(r);
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT precio FROM planes_actividad WHERE id = $id AND actividad_id = $actividadId;";
            cmd.Parameters.AddWithValue("$id", planId);
            cmd.Parameters.AddWithValue("$actividadId", actividadId);
            var result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
        }

        public static decimal ObtenerPrecioPlanActividad(int planId, int anio, int mes)
            => CalcularCuotaPorPlan(planId, anio, mes);

        public static void ActualizarPrecioPlanActividad(int planId, decimal nuevoPrecio, int anio, int mes)
        {
            if (nuevoPrecio < 0) throw new ArgumentException("El precio no puede ser negativo");
            int actividadId = ActividadActualId;
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO planes_actividad_precio_historial(plan_id, anio, mes, precio) VALUES($planId,$anio,$mes,$precio) ON CONFLICT(plan_id, anio, mes) DO UPDATE SET precio=excluded.precio;";
                cmd.Parameters.AddWithValue("$planId", planId);
                cmd.Parameters.AddWithValue("$anio", anio);
                cmd.Parameters.AddWithValue("$mes", mes);
                cmd.Parameters.AddWithValue("$precio", nuevoPrecio);
                cmd.ExecuteNonQuery();
            }

            int periodoActual = DateTime.Today.Year * 100 + DateTime.Today.Month;
            int periodoSeleccionado = anio * 100 + mes;
            if (periodoSeleccionado == periodoActual)
            {
                using var cmdBase = conn.CreateCommand();
                cmdBase.CommandText = "UPDATE planes_actividad SET precio = $precio WHERE id = $planId AND actividad_id = $actividadId;";
                cmdBase.Parameters.AddWithValue("$precio", nuevoPrecio);
                cmdBase.Parameters.AddWithValue("$planId", planId);
                cmdBase.Parameters.AddWithValue("$actividadId", actividadId);
                cmdBase.ExecuteNonQuery();
            }
            RegistrarAuditoria("AJUSTE_PRECIO", $"Actualizó precio del plan #{planId} a ${nuevoPrecio:0.##} (Vigente desde {mes:D2}/{anio})");
        }

        // Compatibilidad con la funcionalidad antigua de precios.
        public static decimal CalcularCuota(int planVecesSemana, int? anio = null, int? mes = null)
        {
            var plan = ListarPlanesActividad(ActividadActualId).FirstOrDefault(p => p.FrecuenciaSemanal == planVecesSemana);
            return plan == null ? 0m : CalcularCuotaPorPlan(plan.Id, anio, mes);
        }

        public static Dictionary<int, decimal> ObtenerTodosLosPrecios(int? anio = null, int? mes = null)
        {
            var resultado = new Dictionary<int, decimal>();
            foreach (var plan in ListarPlanesActividad(ActividadActualId))
            {
                resultado[plan.Id] = CalcularCuotaPorPlan(plan.Id, anio ?? DateTime.Today.Year, mes ?? DateTime.Today.Month);
            }
            return resultado;
        }

        public static void ActualizarPrecioPlan(int planVecesSemana, decimal nuevoPrecio, int? anio = null, int? mes = null)
        {
            var plan = ListarPlanesActividad(ActividadActualId).FirstOrDefault(p => p.FrecuenciaSemanal == planVecesSemana);
            if (plan == null) throw new ArgumentException("No se encontró un plan compatible.");
            ActualizarPrecioPlanActividad(plan.Id, nuevoPrecio, anio ?? DateTime.Today.Year, mes ?? DateTime.Today.Month);
        }

        public static List<PlanActividad> ListarPlanesActividad(int actividadId)
        {
            var repo = new Infrastructure.SQLite.SqlitePlanActividadRepository(ConnectionString);
            return repo.ObtenerPorActividad(actividadId);
        }

        // ---------- DASHBOARD Y ESTADO ----------

        public static EstadoAlumnoMes? EstadoDeAlumno(int alumnoId, int anio, int mes)
        {
            var alumno = ObtenerAlumno(alumnoId);
            if (alumno == null) return null;
            var plan = ObtenerPlanActividad(alumno.PlanId);
            int corresponden = plan?.ClasesMes ?? (alumno.ClasesMes > 0 ? alumno.ClasesMes : 0);
            bool controlaClases = corresponden > 0;
            int tomadas = controlaClases ? ContarAsistenciasMes(alumnoId, anio, mes) : 0;
            decimal cuota = CalcularCuotaPorPlan(alumno.PlanId, anio, mes);
            DateTime? fechaInicio = alumno.FechaInicioPlan;
            DateTime? fechaVencimiento = alumno.FechaVencimientoPlan;
            int diasRestantes = int.MaxValue;
            decimal abonado;
            if (controlaClases)
                abonado = ObtenerMontoAbonado(alumnoId, anio, mes);
            else
            {
                fechaInicio ??= DateTime.TryParse(alumno.FechaInscripcion, out var ins) ? ins : DateTime.Today;
                fechaVencimiento ??= fechaInicio.Value.Date.AddDays(30);
                diasRestantes = (fechaVencimiento.Value.Date - DateTime.Today).Days;
                abonado = ObtenerMontoAbonadoDesdeFecha(alumnoId, fechaInicio.Value);
            }
            EstadoMes estado = EstadoMes.EnProgreso;
            int restantes = int.MaxValue;
            if (controlaClases)
            {
                restantes = Math.Max(corresponden - tomadas, 0);
                if (tomadas > corresponden) estado = EstadoMes.Excedido;
                else if (tomadas == corresponden) estado = EstadoMes.Completo;
            }
            return new EstadoAlumnoMes
            {
                AlumnoId=alumnoId, Nombre=alumno.Nombre, Codigo=alumno.Codigo, Cedula=alumno.Cedula,
                PlanVecesSemana=alumno.PlanVecesSemana, PlanId=alumno.PlanId,
                PlanNombre=plan?.DescripcionCompleta ?? (alumno.PlanVecesSemana > 0 ? $"{alumno.PlanVecesSemana}x/semana" : "Sin plan"),
                ControlaClases=controlaClases, ClasesMes=corresponden, Tomadas=tomadas, Restantes=restantes, Estado=estado,
                EsMembresia=!controlaClases, FechaInicioPlan=fechaInicio, FechaVencimientoPlan=fechaVencimiento, DiasRestantes=diasRestantes,
                CuotaTotal=cuota, MontoAbonado=abonado
            };
        }

        public static List<EstadoAlumnoMes> Dashboard(int anio, int mes, int? actividadId = null)
        {
            actividadId ??= ActividadActualId;
            var resultado = new List<EstadoAlumnoMes>();

            foreach (var alumno in ListarAlumnos(soloActivos: true, actividadId: actividadId))
            {
                if (DateTime.TryParse(alumno.FechaInscripcion, out DateTime fechaInscripcion))
                {
                    int periodoConsultado = (anio * 100) + mes;
                    int periodoInscripcion = (fechaInscripcion.Year * 100) + fechaInscripcion.Month;

                    if (periodoConsultado < periodoInscripcion)
                    {
                        continue;
                    }
                }

                var estado = EstadoDeAlumno(alumno.Id, anio, mes);
                if (estado != null)
                {
                    resultado.Add(estado);
                }
            }

            return resultado;
        }

        // ---------- MÉTRICAS PARA TARJETAS SUPERIORES ----------

        public static MetricasResumen ObtenerMetricasRapidas(int anio, int mes, int? actividadId = null)
        {
            var metricas = new MetricasResumen();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // 1. Alumnos Activos
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = actividadId.HasValue
                    ? "SELECT COUNT(*) FROM alumnos WHERE activo = 1 AND actividad_id = $actividadId;"
                    : "SELECT COUNT(*) FROM alumnos WHERE activo = 1;";

                if (actividadId.HasValue)
                    cmd.Parameters.AddWithValue("$actividadId", actividadId.Value);
                metricas.AlumnosActivos = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 2. Asistencias de Hoy
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM asistencias WHERE fecha = $hoy AND presente = 1;";
                cmd.Parameters.AddWithValue("$hoy", DateTime.Today.ToString("yyyy-MM-dd"));
                metricas.AsistenciasHoy = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 3. Planes Agotados o por vencer (Les queda 1 o 0 clases en el mes)
            var listaDashboard = Dashboard(anio, mes, actividadId);
            metricas.PlanesPorVencer = listaDashboard.Count(a => a.PorVencer);

            // 4. Total Recaudado en el Mes
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT SUM(monto) FROM pagos WHERE anio = $anio AND mes = $mes;";
                cmd.Parameters.AddWithValue("$anio", anio);
                cmd.Parameters.AddWithValue("$mes", mes);
                var result = cmd.ExecuteScalar();
                metricas.RecaudadoMes = (result == DBNull.Value || result == null) ? 0m : Convert.ToDecimal(result);
            }

            return metricas;
        }

        // ---------- SISTEMA DE RESPALDOS AUTOMÁTICOS ----------

        public static void CrearRespaldoAutomatico()
        {
            try
            {
                SqliteConnection.ClearAllPools();
                if (!File.Exists(DbPath)) return;

                string fecha = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string nombreArchivo = $"asistencia_natacion_backup_{fecha}.db";

                // A) Respaldo PORTÁTIL
                string rutaPortatil = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                GuardarEnRuta(rutaPortatil, nombreArchivo);

                // B) Respaldo SEGURO (Mis Documentos)
                string rutaDocumentos = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Respaldos_Natacion"
                );
                GuardarEnRuta(rutaDocumentos, nombreArchivo);
            }
            catch
            {
                // Manejo silencioso
            }
        }

        private static void GuardarEnRuta(string carpetaDestino, string nombreArchivo)
        {
            try
            {
                if (!Directory.Exists(carpetaDestino))
                {
                    Directory.CreateDirectory(carpetaDestino);
                }

                string rutaFinal = Path.Combine(carpetaDestino, nombreArchivo);
                File.Copy(DbPath, rutaFinal, overwrite: true);

                LimpiarRespaldosAntiguos(carpetaDestino, 30);
            }
            catch
            {
                // Ignorar si falla alguna ruta
            }
        }

        /// <summary>
        /// Devuelve la lista de IDs de alumnos que marcaron asistencia en el día de hoy.
        /// </summary>
        public static HashSet<int> ObtenerIdsAsistieronHoy()
        {
            var ids = new HashSet<int>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT alumno_id FROM asistencias WHERE fecha = $hoy AND presente = 1;";
            cmd.Parameters.AddWithValue("$hoy", DateTime.Today.ToString("yyyy-MM-dd"));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        private static void LimpiarRespaldosAntiguos(string carpetaDestino, int maximoArchivos)
        {
            try
            {
                var archivos = new DirectoryInfo(carpetaDestino)
                    .GetFiles("asistencia_natacion_backup_*.db")
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (archivos.Count > maximoArchivos)
                {
                    foreach (var archivo in archivos.Skip(maximoArchivos))
                    {
                        archivo.Delete();
                    }
                }
            }
            catch
            {
                // Ignorar errores al eliminar
            }
        }

        // ---------- SISTEMA DE AUDITORÍA (BITÁCORA DE EVENTOS) ----------

        public static void RegistrarAuditoria(string accion, string detalle, string? usuario = null)
        {
            try
            {
                string op = string.IsNullOrWhiteSpace(usuario) ? SesionUsuario.OperadorActual : usuario;
                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO auditoria (fecha, usuario, accion, detalle, actividad_id)
                    VALUES ($fecha, $usuario, $accion, $detalle, $actividadId);
                ";
                cmd.Parameters.AddWithValue("$fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("$usuario", op.Trim());
                cmd.Parameters.AddWithValue("$accion", accion.Trim());
                cmd.Parameters.AddWithValue("$detalle", detalle.Trim());
                cmd.Parameters.AddWithValue("$actividadId", ActividadActualId);
                cmd.ExecuteNonQuery();
            }
            catch
            {
                // La auditoría es silenciosa para no interrumpir operaciones
            }
        }

        public static List<EventoAuditoria> ObtenerAuditoria(DateTime? desde = null, DateTime? hasta = null, string? filtroTexto = null)
        {
            var resultado = new List<EventoAuditoria>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            string sql = "SELECT id, fecha, usuario, accion, detalle FROM auditoria WHERE actividad_id = $actividadId";
            cmd.Parameters.AddWithValue("$actividadId", ActividadActualId);

            if (desde.HasValue)
            {
                sql += " AND fecha >= $desde";
                cmd.Parameters.AddWithValue("$desde", desde.Value.ToString("yyyy-MM-dd 00:00:00"));
            }

            if (hasta.HasValue)
            {
                sql += " AND fecha <= $hasta";
                cmd.Parameters.AddWithValue("$hasta", hasta.Value.ToString("yyyy-MM-dd 23:59:59"));
            }

            if (!string.IsNullOrWhiteSpace(filtroTexto))
            {
                sql += " AND (LOWER(usuario) LIKE $txt OR LOWER(accion) LIKE $txt OR LOWER(detalle) LIKE $txt)";
                cmd.Parameters.AddWithValue("$txt", $"%{filtroTexto.Trim().ToLower()}%");
            }

            sql += " ORDER BY id DESC LIMIT 500;";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new EventoAuditoria
                {
                    Id = reader.GetInt32(0),
                    Fecha = reader.GetString(1),
                    Usuario = reader.GetString(2),
                    Accion = reader.GetString(3),
                    Detalle = reader.GetString(4)
                });
            }

            return resultado;
        }
    }
}
using AsistenciaNatacion.Application.App;
using AsistenciaNatacion.Domain;
using AsistenciaNatacion.Infrastructure.Logging;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace AsistenciaNatacion
{
    public static class Database
    {
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "asistencia_natacion.db");

        private static string ConnectionString => $"Data Source={DbPath}";

        public static string ObtenerConnectionString()
        {
            return ConnectionString;
        }

        public static void Inicializar()
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();

            // Modo WAL: la API móvil y la ventana principal escriben a la vez,
            // y WAL evita bloqueos y es más resistente a cortes de luz.
            // El modo queda guardado en el archivo de la base de datos.
            using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA journal_mode=WAL;";
                pragma.ExecuteNonQuery();
            }

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
                    fecha_vencimiento_plan TEXT,
                    foto_ruta TEXT
                );

                CREATE TABLE IF NOT EXISTS asistencias (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    alumno_id INTEGER NOT NULL,
                    fecha TEXT NOT NULL,
                    presente INTEGER NOT NULL DEFAULT 1,
                    UNIQUE(alumno_id, fecha),
                    FOREIGN KEY (alumno_id) REFERENCES alumnos(id) ON DELETE CASCADE
                );

                -- Operaciones de asistencia ya recibidas desde VikingMobile.
                -- Permite reconocer reenvíos del mismo escaneo y no repetirlos.
                CREATE TABLE IF NOT EXISTS asistencias_movil_ops (
                    id_operacion TEXT PRIMARY KEY,
                    alumno_id INTEGER NOT NULL,
                    fecha TEXT NOT NULL,
                    recibido_en TEXT NOT NULL
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
CREATE TABLE IF NOT EXISTS solicitudes_pago (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    alumno_id INTEGER NOT NULL,
    actividad_id INTEGER NOT NULL,
    anio INTEGER NOT NULL,
    mes INTEGER NOT NULL,
    monto_usd DECIMAL NOT NULL DEFAULT 0,
    tasa_bcv DECIMAL NOT NULL DEFAULT 0,
    concepto TEXT DEFAULT '',
    estado TEXT NOT NULL DEFAULT 'pendiente',
    comprobante BLOB,
    comprobante_content_type TEXT,
    fecha_creacion TEXT NOT NULL,
    fecha_comprobante TEXT,
    fecha_resolucion TEXT,
    comentario_admin TEXT DEFAULT '',
    folio_pago TEXT DEFAULT '',
    FOREIGN KEY (alumno_id) REFERENCES alumnos(id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS metodos_pago_actividad (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    actividad_id INTEGER NOT NULL,
    metodo TEXT NOT NULL,
    datos TEXT DEFAULT '',
    activo INTEGER NOT NULL DEFAULT 0,
    UNIQUE (actividad_id, metodo)
);
CREATE TABLE IF NOT EXISTS logo_actividad (
    actividad_id INTEGER PRIMARY KEY,
    logo BLOB,
    content_type TEXT DEFAULT 'image/png'
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
                catch (Exception ex)
                {
                    Logger.Warn("Migración de roles de operador omitida.", ex);
                }
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
            AsegurarColumna(conn, "alumnos", "cedula", "TEXT");
            AsegurarColumna(conn, "alumnos", "telefono", "TEXT");
            AsegurarColumna(conn, "alumnos", "foto_ruta", "TEXT");
            AsegurarColumna(conn, "alumnos", "contacto_emergencia_nombre", "TEXT");
            AsegurarColumna(conn, "alumnos", "contacto_emergencia_telefono", "TEXT");
            AsegurarColumna(conn, "alumnos", "tipo_sangre", "TEXT");
            AsegurarColumna(conn, "alumnos", "alertas_medicas", "TEXT");
            AsegurarColumna(conn, "alumnos", "nivel_categoria", "TEXT");

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

            AsegurarColumna(conn, "solicitudes_pago", "metodo_pago", "TEXT DEFAULT ''");

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
            catch (Exception ex)
            {
                Logger.Warn("Migración de planes heredados omitida.", ex);
            }

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
            catch (Exception ex)
            {
                Logger.Warn("Migración de opciones de plan omitida.", ex);
            }

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
            catch (Exception ex)
            {
                Logger.Warn("Migración de mapeo de planes omitida.", ex);
            }

            RevisarIntegridadReferencial(conn);
        }

        /// <summary>
        /// Diagnóstico: busca filas que violan claves foráneas y lo deja en el log.
        /// Todavía no se activa PRAGMA foreign_keys=ON en las conexiones porque
        /// datos antiguos con violaciones harían fallar inserciones. Cuando este
        /// diagnóstico salga limpio en los equipos de los clientes, se puede
        /// activar con "Foreign Keys=True" en la cadena de conexión.
        /// </summary>
        private static void RevisarIntegridadReferencial(SqliteConnection conn)
        {
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA foreign_key_check;";

                int violaciones = 0;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        violaciones++;
                        if (violaciones <= 20)
                        {
                            Logger.Warn(
                                $"Clave foránea violada: tabla '{reader.GetString(0)}', " +
                                $"fila {(reader.IsDBNull(1) ? "?" : reader.GetInt64(1).ToString())}, " +
                                $"referencia a '{reader.GetString(2)}'.");
                        }
                    }
                }

                if (violaciones > 0)
                    Logger.Warn($"Revisión de claves foráneas: {violaciones} violación(es).");
            }
            catch (Exception ex)
            {
                Logger.Warn("No se pudo revisar la integridad referencial.", ex);
            }
        }
    }
}

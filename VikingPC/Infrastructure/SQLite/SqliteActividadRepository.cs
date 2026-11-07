using System;
using AsistenciaNatacion.Application.Ports;
using AsistenciaNatacion.Domain;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace AsistenciaNatacion.Infrastructure.SQLite
{
    public class SqliteActividadRepository : IActividadRepository
    {
        private readonly string connectionString;

        public SqliteActividadRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<Actividad> ObtenerTodas()
        {
            var actividades = new List<Actividad>();

            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, nombre, activa
                FROM actividades
                WHERE activa = 1
                ORDER BY nombre;
            ";

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                actividades.Add(new Actividad
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Activa = reader.GetInt32(2) == 1
                });
            }

            return actividades;
        }

        public Actividad? ObtenerPorId(int id)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, nombre, activa
                FROM actividades
                WHERE id = $id;
            ";

            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Actividad
            {
                Id = reader.GetInt32(0),
                Nombre = reader.GetString(1),
                Activa = reader.GetInt32(2) == 1
            };
        }

        public int Crear(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la actividad es obligatorio.");

            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO actividades (nombre, activa)
        VALUES ($nombre, 1);

        SELECT last_insert_rowid();
    ";

            cmd.Parameters.AddWithValue("$nombre", nombre.Trim());

            int actividadId = Convert.ToInt32(cmd.ExecuteScalar());

            // Cada actividad nueva comienza con una copia independiente de
            // los precios actuales y del operador de la actividad base.
            // A partir de aquí, cualquier cambio queda aislado por actividad.
            using var cmdInicializacion = conn.CreateCommand();
            cmdInicializacion.CommandText = @"
                INSERT OR IGNORE INTO precios_plan_actividad
                    (actividad_id, plan_veces_semana, precio)
                SELECT $actividadId, plan_veces_semana, precio
                FROM precios_plan_actividad
                WHERE actividad_id = 1;

                INSERT OR IGNORE INTO precios_plan_historial_actividad
                    (actividad_id, plan_veces_semana, anio, mes, precio)
                SELECT $actividadId, plan_veces_semana, anio, mes, precio
                FROM precios_plan_historial_actividad
                WHERE actividad_id = 1;

                INSERT OR IGNORE INTO configuracion_operador_actividad
                    (actividad_id, nombre)
                SELECT $actividadId, nombre
                FROM configuracion_operador_actividad
                WHERE actividad_id = 1;
            ";
            cmdInicializacion.Parameters.AddWithValue("$actividadId", actividadId);
            cmdInicializacion.ExecuteNonQuery();

            return actividadId;
        }

        public void Desactivar(int id)
        {
            using var conn = new SqliteConnection(connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        UPDATE actividades
        SET activa = 0
        WHERE id = $id;
    ";

            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

    }
}
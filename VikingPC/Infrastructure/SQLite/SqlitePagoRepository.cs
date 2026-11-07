using Microsoft.Data.Sqlite;

namespace AsistenciaNatacion;

public class SqlitePagoRepository : IPagoRepository
{
    private readonly string connectionString;

    public SqlitePagoRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public Pago Crear(Pago pago)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        INSERT INTO pagos
        (
            alumno_id,
            anio,
            mes,
            monto,
            fecha_pago,
            metodo_pago,
            referencia,
            tasa_bcv,
            monto_bs,
            folio
        )
        VALUES
        (
            $alumnoId,
            $anio,
            $mes,
            $monto,
            $fecha,
            $metodoPago,
            $referencia,
            $tasaBcv,
            $montoBs,
            $folio
        );

        SELECT last_insert_rowid();
    ";

        cmd.Parameters.AddWithValue(
            "$alumnoId",
            pago.AlumnoId);

        cmd.Parameters.AddWithValue(
            "$anio",
            pago.Anio);

        cmd.Parameters.AddWithValue(
            "$mes",
            pago.Mes);

        cmd.Parameters.AddWithValue(
            "$monto",
            pago.MontoUsd);

        cmd.Parameters.AddWithValue(
            "$fecha",
            pago.FechaPago.ToString("yyyy-MM-dd HH:mm:ss"));

        cmd.Parameters.AddWithValue(
            "$metodoPago",
            pago.MetodoPago);

        cmd.Parameters.AddWithValue(
            "$referencia",
            pago.Referencia);

        cmd.Parameters.AddWithValue(
            "$tasaBcv",
            pago.TasaBcv);

        cmd.Parameters.AddWithValue(
            "$montoBs",
            pago.MontoBs);

        cmd.Parameters.AddWithValue(
            "$folio",
            pago.Folio);

        pago.Id = Convert.ToInt32(
            cmd.ExecuteScalar());

        return pago;
    }

    public Pago? ObtenerPorId(int id)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        SELECT
            id,
            alumno_id,
            anio,
            mes,
            monto,
            fecha_pago,
            metodo_pago,
            referencia,
            tasa_bcv,
            monto_bs,
            folio
        FROM pagos
        WHERE id = $id;
    ";

        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return LeerPago(reader);
    }

    private Pago LeerPago(SqliteDataReader reader)
    {
        return new Pago
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            AlumnoId = reader.GetInt32(reader.GetOrdinal("alumno_id")),
            Anio = reader.GetInt32(reader.GetOrdinal("anio")),
            Mes = reader.GetInt32(reader.GetOrdinal("mes")),
            MontoUsd = reader.GetDecimal(reader.GetOrdinal("monto")),
            FechaPago = DateTime.Parse(
                reader.GetString(reader.GetOrdinal("fecha_pago"))),
            MetodoPago = reader.GetString(
                reader.GetOrdinal("metodo_pago")),
            Referencia = reader.GetString(
                reader.GetOrdinal("referencia")),
            TasaBcv = reader.GetDecimal(
                reader.GetOrdinal("tasa_bcv")),
            MontoBs = reader.GetDecimal(
                reader.GetOrdinal("monto_bs")),
            Folio = reader.GetString(
                reader.GetOrdinal("folio"))
        };
    }


    public Pago? ObtenerPorFolio(string folio)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        SELECT
            id,
            alumno_id,
            anio,
            mes,
            monto,
            fecha_pago,
            metodo_pago,
            referencia,
            tasa_bcv,
            monto_bs,
            folio
        FROM pagos
        WHERE folio = $folio;
    ";

        cmd.Parameters.AddWithValue("$folio", folio);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return null;

        return LeerPago(reader);
    }

    public bool ExisteReferencia(string referencia)
    {
        if (string.IsNullOrWhiteSpace(referencia))
            return false;

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        SELECT COUNT(*)
        FROM pagos
        WHERE referencia = $referencia;
    ";

        cmd.Parameters.AddWithValue(
            "$referencia",
            referencia.Trim());

        var resultado = cmd.ExecuteScalar();

        return Convert.ToInt32(resultado) > 0;
    }

    public string GenerarSiguienteFolio(int anio)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        SELECT folio
        FROM pagos
        WHERE folio LIKE $prefijo
        ORDER BY id DESC
        LIMIT 1;
    ";

        cmd.Parameters.AddWithValue(
            "$prefijo",
            $"REC-{anio}-%");

        var resultado = cmd.ExecuteScalar();

        if (resultado == null || resultado == DBNull.Value)
            return $"REC-{anio}-0001";

        string ultimoFolio = resultado.ToString() ?? "";

        const int longitudNumerica = 4;

        if (ultimoFolio.Length < longitudNumerica)
            return $"REC-{anio}-0001";

        string parteNumerica =
            ultimoFolio[^longitudNumerica..];

        if (!int.TryParse(parteNumerica, out int ultimoNumero))
            return $"REC-{anio}-0001";

        return $"REC-{anio}-{ultimoNumero + 1:D4}";
    }

    public List<Pago> ObtenerPorAlumnoPeriodo(
    int alumnoId,
    int anio,
    int mes)
    {
        var pagos = new List<Pago>();

        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        SELECT
            id,
            alumno_id,
            anio,
            mes,
            monto,
            fecha_pago,
            metodo_pago,
            referencia,
            tasa_bcv,
            monto_bs,
            folio
        FROM pagos
        WHERE alumno_id = $alumnoId
          AND anio = $anio
          AND mes = $mes
        ORDER BY fecha_pago ASC, id ASC;
    ";

        cmd.Parameters.AddWithValue("$alumnoId", alumnoId);
        cmd.Parameters.AddWithValue("$anio", anio);
        cmd.Parameters.AddWithValue("$mes", mes);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var pago = new Pago
            {
                Id = Convert.ToInt32(reader["id"]),
                AlumnoId = Convert.ToInt32(reader["alumno_id"]),
                Anio = Convert.ToInt32(reader["anio"]),
                Mes = Convert.ToInt32(reader["mes"]),
                MontoUsd = Convert.ToDecimal(reader["monto"]),
                FechaPago = DateTime.Parse(
                    reader["fecha_pago"].ToString() ?? ""),
                MetodoPago = reader["metodo_pago"]?.ToString() ?? "",
                Referencia = reader["referencia"]?.ToString() ?? "",
                TasaBcv = Convert.ToDecimal(reader["tasa_bcv"]),
                MontoBs = Convert.ToDecimal(reader["monto_bs"]),
                Folio = reader["folio"]?.ToString() ?? ""
            };

            pagos.Add(pago);
        }

        return pagos;
    }

}
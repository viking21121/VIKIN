using AsistenciaNatacion.Application.Ports;
using Microsoft.Data.Sqlite;
namespace AsistenciaNatacion.Infrastructure.SQLite;
public class SqlitePrecioPlanRepository : IPrecioPlanRepository
{
    private readonly string cs; public SqlitePrecioPlanRepository(string cs)=>this.cs=cs;
    public decimal Obtener(int planId,int anio,int mes)
    {
        using var c=new SqliteConnection(cs); c.Open(); int periodo=anio*100+mes;
        using var h=c.CreateCommand(); h.CommandText="SELECT precio FROM planes_actividad_precio_historial WHERE plan_id=$id AND (anio*100+mes)<= $periodo ORDER BY anio DESC,mes DESC LIMIT 1"; h.Parameters.AddWithValue("$id",planId);h.Parameters.AddWithValue("$periodo",periodo); var v=h.ExecuteScalar(); if(v!=null&&v!=DBNull.Value)return Convert.ToDecimal(v);
        using var b=c.CreateCommand(); b.CommandText="SELECT COALESCE(precio,0) FROM planes_actividad WHERE id=$id";b.Parameters.AddWithValue("$id",planId);return Convert.ToDecimal(b.ExecuteScalar()??0m);
    }
    public void Actualizar(int planId,decimal precio,int anio,int mes,int actividadId)
    {
        if(precio<0)throw new ArgumentException("El precio no puede ser negativo."); using var c=new SqliteConnection(cs);c.Open();
        using var h=c.CreateCommand();h.CommandText="INSERT INTO planes_actividad_precio_historial(plan_id,anio,mes,precio) VALUES($id,$anio,$mes,$precio) ON CONFLICT(plan_id,anio,mes) DO UPDATE SET precio=excluded.precio";h.Parameters.AddWithValue("$id",planId);h.Parameters.AddWithValue("$anio",anio);h.Parameters.AddWithValue("$mes",mes);h.Parameters.AddWithValue("$precio",precio);h.ExecuteNonQuery();
        if(anio==DateTime.Today.Year&&mes==DateTime.Today.Month){using var b=c.CreateCommand();b.CommandText="UPDATE planes_actividad SET precio=$precio WHERE id=$id AND actividad_id=$actividadId";b.Parameters.AddWithValue("$precio",precio);b.Parameters.AddWithValue("$id",planId);b.Parameters.AddWithValue("$actividadId",actividadId);b.ExecuteNonQuery();}
    }
}

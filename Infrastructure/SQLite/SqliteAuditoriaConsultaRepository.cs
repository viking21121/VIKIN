using AsistenciaNatacion.Application.Ports;
using Microsoft.Data.Sqlite;
namespace AsistenciaNatacion.Infrastructure.SQLite;
public class SqliteAuditoriaConsultaRepository : IAuditoriaConsultaRepository
{
    private readonly string cs; public SqliteAuditoriaConsultaRepository(string cs)=>this.cs=cs;
    public List<EventoAuditoria> Obtener(DateTime? desde,DateTime? hasta,string? filtro,int actividadId)
    {
        var r=new List<EventoAuditoria>(); using var c=new SqliteConnection(cs);c.Open();using var cmd=c.CreateCommand();
        var sql="SELECT id,fecha,usuario,accion,detalle FROM auditoria WHERE actividad_id=$actividadId";cmd.Parameters.AddWithValue("$actividadId",actividadId);
        if(desde.HasValue){sql+=" AND datetime(fecha)>=datetime($desde)";cmd.Parameters.AddWithValue("$desde",desde.Value.Date.ToString("yyyy-MM-dd 00:00:00"));}
        if(hasta.HasValue){sql+=" AND datetime(fecha)<=datetime($hasta)";cmd.Parameters.AddWithValue("$hasta",hasta.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss"));}
        if(!string.IsNullOrWhiteSpace(filtro)){sql+=" AND (usuario LIKE $f OR accion LIKE $f OR detalle LIKE $f)";cmd.Parameters.AddWithValue("$f",$"%{filtro.Trim()}%");}
        sql+=" ORDER BY datetime(fecha) DESC";cmd.CommandText=sql;using var rd=cmd.ExecuteReader();while(rd.Read())r.Add(new EventoAuditoria{Id=rd.GetInt32(0),Fecha=rd.GetString(1),Usuario=rd.GetString(2),Accion=rd.GetString(3),Detalle=rd.GetString(4)});return r;
    }
}

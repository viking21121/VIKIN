using AsistenciaNatacion.Application.Ports;
using System.IO;

namespace AsistenciaNatacion.Infrastructure.SQLite;

public class SqliteRespaldoRepository : IRespaldoRepository
{
    private readonly string dbPath;
    public SqliteRespaldoRepository(string dbPath) => this.dbPath=dbPath;
    public void CrearAutomatico()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if(!File.Exists(dbPath)) return;
            var nombre=$"asistencia_natacion_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";
            Guardar(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Backups"),nombre);
            Guardar(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Respaldos_Natacion"),nombre);
        }catch { }
    }
    private void Guardar(string carpeta,string nombre){try{Directory.CreateDirectory(carpeta);var destino=Path.Combine(carpeta,nombre);File.Copy(dbPath,destino,true);var archivos=new DirectoryInfo(carpeta).GetFiles("asistencia_natacion_backup_*.db").OrderByDescending(x=>x.CreationTime).Skip(30).ToList();foreach(var a in archivos)a.Delete();}catch{}}
}

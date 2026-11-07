namespace AsistenciaNatacion;

public interface IConfiguracionRepository
{
    ConfiguracionEstablecimiento Obtener(int actividadId);

    void Guardar(int actividadId, ConfiguracionEstablecimiento configuracion);
}

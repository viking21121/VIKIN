namespace AsistenciaNatacion;

public interface IConfiguracionOperadorRepository
{
    ConfiguracionOperador Obtener(string rol);

    void Guardar(ConfiguracionOperador configuracion);
}

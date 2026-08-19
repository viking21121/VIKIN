namespace AsistenciaNatacion;

public class ObtenerConfiguracionOperador
{
    private readonly IConfiguracionOperadorRepository repository;

    public ObtenerConfiguracionOperador(
        IConfiguracionOperadorRepository repository)
    {
        this.repository = repository;
    }

    public ConfiguracionOperador Ejecutar(string rol)
    {
        if (string.IsNullOrWhiteSpace(rol))
            rol = "Recepción";

        return repository.Obtener(rol.Trim());
    }
}

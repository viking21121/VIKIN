namespace AsistenciaNatacion;

public class GuardarConfiguracionOperador
{
    private readonly IConfiguracionOperadorRepository repository;

    public GuardarConfiguracionOperador(
        IConfiguracionOperadorRepository repository)
    {
        this.repository = repository;
    }

    public void Ejecutar(ConfiguracionOperador configuracion)
    {
        if (string.IsNullOrWhiteSpace(configuracion.Rol))
            throw new ArgumentException(
                "El rol del operador es obligatorio.");

        if (string.IsNullOrWhiteSpace(configuracion.Nombre))
            throw new ArgumentException(
                "El nombre del operador no puede estar vacío.");

        configuracion.Rol = configuracion.Rol.Trim();
        configuracion.Nombre = configuracion.Nombre.Trim();

        repository.Guardar(configuracion);
    }
}

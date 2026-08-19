# Publicar una actualización de VIKING

VIKING comprueba automáticamente la última publicación de GitHub Releases del repositorio `viking21121/VIKIN`.

## Publicar una nueva versión

1. Cambia la versión en `AsistenciaNatacion.csproj`:

```xml
<Version>1.0.1</Version>
<AssemblyVersion>1.0.1.0</AssemblyVersion>
<FileVersion>1.0.1.0</FileVersion>
```

2. Guarda, compila y prueba VIKING.

3. Haz commit y push.

4. Crea y publica un tag con la misma versión:

```bash
git tag v1.0.1
git push origin v1.0.1
```

5. GitHub Actions ejecutará `.github/workflows/release.yml` y generará automáticamente `VIKING.zip` dentro del GitHub Release.

## Qué ocurre en el cliente

- VIKING consulta `releases/latest` al iniciar.
- Si la versión publicada es superior, descarga `VIKING.zip` mediante `VikingUpdater.exe`.
- VIKING se cierra.
- El updater crea un respaldo de `asistencia_natacion.db` en `Backups/update_...`.
- Los archivos de la aplicación se reemplazan.
- La base de datos del cliente nunca se reemplaza por la del paquete.
- VIKING vuelve a abrirse.
- `Database.Inicializar()` ejecuta las migraciones pendientes.

Si GitHub no está disponible, no existe un Release o hay cualquier problema con la comprobación, VIKING continúa iniciando normalmente.

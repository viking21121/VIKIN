# Actualización automática de VIKING

## Repositorio

`viking21121/VIKIN`

El repositorio es público y VIKING consulta únicamente el endpoint de GitHub Releases para comprobar la versión estable más reciente.

## Componentes

- `Infrastructure/Updates/UpdateChecker.cs`: comprueba la versión y arranca el updater.
- `Infrastructure/Updates/UpdateSettings.cs`: identifica el repositorio y el nombre del paquete.
- `Infrastructure/Updates/GitHubRelease.cs`: modelo de la respuesta de GitHub.
- `VikingUpdater/`: programa separado que descarga, respalda y reemplaza los archivos.
- `.github/workflows/release.yml`: genera el ZIP y crea el GitHub Release al publicar un tag `vX.Y.Z`.

## Datos del cliente

El paquete de actualización no contiene `asistencia_natacion.db`.

Antes de reemplazar archivos, `VikingUpdater.exe` crea:

```text
Backups/update_YYYY-MM-DD_HH-mm-ss/
```

y conserva la base de datos y sus archivos SQLite auxiliares si existen.

Después de actualizar, VIKING vuelve a ejecutar `Database.Inicializar()`, por lo que las migraciones existentes pueden transformar la base de datos del cliente sin sustituir sus datos.

## Comportamiento ante fallos

La comprobación de actualización tiene tolerancia a errores: si no hay Internet, GitHub no responde o no existe un Release válido, VIKING inicia normalmente.

Si el updater falla durante una actualización, registra el error en `update.log` e intenta iniciar nuevamente VIKING.

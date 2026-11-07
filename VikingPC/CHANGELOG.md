# Historial de cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/).

## [Sin publicar]

### Añadido
- Carnet impreso de dos caras (tamaño tarjeta 85,6 × 54 mm), en PDF: frente con logo, academia, disciplinas, foto, nombre, cédula, código y nivel; reverso con QR, contacto de emergencia, tipo de sangre y alertas, emisión, vigencia o estado y nota legal. Se abre con **Carnet impreso…** en la pantalla de edición del alumno. Campos nuevos en `alumnos`: contacto y teléfono de emergencia, tipo de sangre, alertas médicas y nivel/categoría (migración automática). Botón **Enviar PDF por WhatsApp**: copia el PDF al portapapeles y abre el chat con el número del representante o del alumno (código de país 58 por defecto); solo hay que pegar con Ctrl+V. El QR sigue siendo el código del alumno, así que los escáneres actuales funcionan igual.
- Instalador (`installer/VIKING.iss`, Inno Setup): instala en `C:\VIKING`, reserva el puerto 5050 de la API móvil y crea la regla de firewall para redes privadas. El flujo de publicación lo genera y lo adjunta al Release junto a `VIKING.zip`. Ver `docs/INSTALADOR_Y_FIRMA.md`. Aún sin firma digital.
- Asistente de bienvenida en la primera ejecución (negocio, operador y dirección para VikingMobile). Solo aparece en instalaciones nuevas, sin alumnos.
- Los recibos se generan también en PDF (con Microsoft Edge o Chrome sin ventana, sin librerías nuevas); si no se puede, se abre el HTML como antes.
- Asistencias de VikingMobile con identificador único de operación (`idOperacion`): si el teléfono reenvía el mismo escaneo, el servidor responde que ya estaba registrado y no repite el registro ni vuelve a mostrar el carnet. Tabla nueva `asistencias_movil_ops`. Es opcional, así que las versiones antiguas de VikingMobile siguen funcionando.
- Proyecto de pruebas `Viking.Tests` (xUnit): pagos, renovación de membresía, ciclo de pago y asistencias idempotentes contra un SQLite temporal.
- Registro de eventos y errores en `Logs/viking-AAAA-MM-DD.log`, con retención de 30 días.
- Diagnóstico de claves foráneas al iniciar: las violaciones se escriben en el registro.
- Flujo de GitHub Actions **Compilar y probar**, que compila la solución y corre las pruebas en cada push y pull request.
- Carpeta `docs/` con la documentación; `README.md` reescrito.

### Cambiado
- El recibo que descarga VikingMobile usa ahora la misma plantilla que el de VIKING PC (`ImpresionRecibo.ConstruirHtml`), con el logo del negocio. Se eliminó la copia antigua que tenía la API. En pantallas angostas la hoja ocupa todo el ancho.
- Recibo rediseñado en tamaño A5: encabezado con el logo y el nombre del negocio, sello PAGADO o ABONO, datos del alumno en dos columnas, monto destacado y resumen de cuota, abonado y resta. Se corrige el PDF, que cortaba el contenido por la derecha.
- El carnet digital que aparece al escanear ya no tapa la ventana del lector: se coloca a su lado (o a la derecha si no cabe). Además se quitó el cuadro vacío que salía en el título del estado, porque el emoji no se dibujaba.
- «Acerca de» muestra la versión real del ejecutable en lugar de un texto fijo.
- El carnet digital en pantalla vuelve a verse al escanear: faltaba configurar el temporizador de aparición (`Opacity` quedaba en 0). Los fallos al preparar o mostrar el carnet ahora se registran.
- La versión sale de una sola línea (`<Version>` en `AsistenciaNatacion.csproj`); `AssemblyVersion`, `FileVersion` e `InformationalVersion` se derivan de ella.
- La base de datos pasa a modo WAL, más resistente a cortes de luz y a escrituras simultáneas de la ventana principal y la API móvil.
- Los respaldos automáticos usan la API de respaldo de SQLite en lugar de copiar el archivo, para que sean consistentes con WAL.
- Los errores de arranque se guardan en `Logs/` en lugar de sobrescribir `crash.log`.
- Las migraciones que fallaban en silencio ahora dejan una advertencia en el registro.
- `/api/viking/health` devuelve la versión real del ejecutable.
- La comprobación de actualizaciones espera como máximo 5 segundos al iniciar (antes 8).

### Eliminado
- Cerca de 830 líneas de código heredado de `Database.cs` (alta, edición y consulta de alumnos, asistencias, abonos, precios, métricas, respaldos y auditoría). Nada fuera del propio archivo las usaba: todo eso ya lo hacen los repositorios de `Infrastructure/SQLite`. `Database.cs` queda solo con la creación del esquema, las migraciones y la cadena de conexión (de 1.470 a unas 600 líneas). `MetricasResumen` pasó a `Domain/MetricasResumen.cs`.

### Eliminado del paquete
- Carpeta `scratch/` (scripts de Word), archivos `.csproj.user`, bases de datos de ejemplo, `bin/` y `obj/`.

## Versiones anteriores

- [Edición comercial 2.2](docs/historial/RELEASE_NOTES_2.2.md)
- [Versión 1.1](docs/historial/RELEASE_NOTES_1.1.md)

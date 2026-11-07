# VIKING

Plataforma de gestión para academias, piscinas, CrossFit, entrenamiento funcional y gimnasios.
Controla alumnos, planes, pagos, asistencia por código QR y carnets, desde una app de escritorio
para Windows. Se complementa con la app **VikingMobile** (Android) para registrar asistencias y
consultar la cuenta desde el teléfono.

## Qué hace

- **Alumnos y carnets** con código QR, carnet imprimible y pantalla de carnet.
- **Varias actividades** (Natación, CrossFit, Funcional, Gimnasio…), cada una con sus propios alumnos y planes.
- **Planes configurables**: por clases (con conteo de clases tomadas y restantes) o membresía de 30 días con cuenta regresiva.
- **Pagos y abonos** con tasa BCV, método de pago, referencia (las duplicadas se rechazan), folio y recibos.
- **Asistencia rápida** (F2) y lectura de asistencias desde archivo.
- **Panel de gestión** con métricas del día y del mes, planes por vencer y recaudación.
- **Auditoría** de operaciones relevantes.
- **Respaldos automáticos** al cerrar (junto al programa y en Mis Documentos).
- **Actualización automática** desde GitHub Releases, con respaldo de la base de datos antes de cada actualización.
- **API para VikingMobile** en el puerto 5050.

## Requisitos

- Windows 10 o superior (64 bits).
- Para compilar: [.NET 8 SDK](https://dotnet.microsoft.com/download) y, opcionalmente, Visual Studio 2022.

## Instalación

Descarga `VIKING-Setup-X.Y.Z.exe` desde [Releases](../../releases) y ejecútalo. Detalles, permisos de red y firma en [`docs/INSTALADOR_Y_FIRMA.md`](docs/INSTALADOR_Y_FIRMA.md). En la primera ejecución, un asistente pide los datos del negocio y muestra la dirección para conectar VikingMobile.

## Compilar y ejecutar

```powershell
dotnet restore VIKING.sln
dotnet build VIKING.sln -c Release
dotnet run --project AsistenciaNatacion.csproj
```

Pruebas automáticas:

```powershell
dotnet test Viking.Tests/Viking.Tests.csproj
```

Para ver el proyecto de pruebas dentro de Visual Studio: `dotnet sln VIKING.sln add Viking.Tests/Viking.Tests.csproj`.

La base de datos (`asistencia_natacion.db`) se crea sola la primera vez, junto al ejecutable.

## Estructura

```text
Presentation/     Formularios y tema visual
Application/      Casos de uso y puertos (interfaces)
Domain/           Entidades del negocio
Infrastructure/   SQLite, API móvil, actualizaciones y registro de eventos
Viking.Tests/     Pruebas xUnit de casos de uso y repositorios
installer/        Script del instalador (Inno Setup)
VikingUpdater/    Programa aparte que descarga y aplica las actualizaciones
docs/             Manual de usuario, contratos y notas de versión
```

La arquitectura sigue un esquema hexagonal: `Presentation → Application/UseCases → Ports → Infrastructure/SQLite`.
`Database.cs` solo crea el esquema y aplica las migraciones de la base de datos; todo el acceso a datos pasa por los repositorios de `Infrastructure/SQLite`.

## Datos, respaldos y registros

| Qué | Dónde |
| --- | --- |
| Base de datos | `asistencia_natacion.db` (modo WAL: pueden aparecer también `-wal` y `-shm`) |
| Respaldos | `Backups/` y `Documentos/Respaldos_Natacion/` (se conservan los últimos 30) |
| Registro de eventos y errores | `Logs/viking-AAAA-MM-DD.log` (se conservan 30 días) |
| Registro del actualizador | `update.log` |

Los respaldos usan la API de respaldo en línea de SQLite, así que son consistentes aunque el programa esté abierto.
Para copiar la base a mano, copia **los tres archivos** (`.db`, `-wal` y `-shm`) con el programa cerrado.

## Publicar una versión

1. Sube los cambios a `main` y espera a que el flujo **Compilar** termine en verde.
2. Crea y sube un tag: `git tag v1.0.4 && git push origin v1.0.4`.
3. El flujo **Publicar VIKING** compila, empaqueta `VIKING.zip` y crea el Release en GitHub.

La versión del ejecutable sale del tag. Más detalles en [`docs/ACTUALIZACION_AUTOMATICA.md`](docs/ACTUALIZACION_AUTOMATICA.md).

## Documentación

- [Manual de usuario](docs/MANUAL_USUARIO.md)
- [Instalador y firma](docs/INSTALADOR_Y_FIRMA.md)
- [Actualización automática](docs/ACTUALIZACION_AUTOMATICA.md)
- [Contrato de sincronización con VikingMobile](docs/MOBILE_SYNC_CONTRACT.md)
- [Panel de gestión](docs/DASHBOARD_GESTION.md)
- [Historial de versiones](CHANGELOG.md)

## Licencia

Software propietario. Ver [`LICENSE_PROPIETARIA.md`](LICENSE_PROPIETARIA.md) y [`COPYRIGHT.md`](COPYRIGHT.md).

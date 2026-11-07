# VikingMobile

App móvil de VIKING (.NET MAUI, Android) para registrar asistencias escaneando el código QR del alumno y consultar la cuenta desde el teléfono. Se conecta a **VIKING PC** por red.

## Modos de uso

- **Escáner de asistencia:** lee el QR del carnet y registra la asistencia en VIKING PC. Sin conexión, guarda el escaneo en el teléfono y lo envía cuando vuelve la conexión.
- **Modo Cliente:** el alumno consulta su cuenta, pagos y recibos (usuario = código de alumno).
- **Modo Administrador:** panel del establecimiento con alumnos, pagos, reportes y revisión de comprobantes.

## Conectar con VIKING PC

1. El teléfono y la laptop deben estar en la **misma red Wi-Fi**.
2. En la laptop, abre una terminal y ejecuta `ipconfig`; anota la «Dirección IPv4», por ejemplo `192.168.1.5`.
3. En la app: **Configuración → SERVIDOR VIKING (IP)** → `http://192.168.1.5:5050`.
4. Para comprobar la red, abre `http://192.168.1.5:5050/api/viking/health` en el navegador del teléfono: debe devolver `"ok": true`.

## Compilar

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download), Visual Studio 2022 o posterior con el workload «.NET MAUI» y Android SDK.

```powershell
dotnet workload restore
dotnet restore VikingMobile.csproj
dotnet build VikingMobile.csproj -f net10.0-android -c Release
```

## Estructura

```text
Application/      Servicios (admin, cliente, autenticación, sincronización)
Infrastructure/   Canal Wi-Fi y cola de asistencias offline
Diagnostics/      Registro de errores (AppLog y CrashLogger)
Resources/        Icono, pantalla de inicio, colores y estilos
Platforms/        Código y manifiestos por plataforma
docs/             Contratos con VIKING PC y notas
```

## Registros de errores

El móvil guarda un registro por día en la carpeta de datos de la app (`Logs/viking-AAAA-MM-DD.log`, 14 días). El último fallo no controlado se puede ver desde la propia app.

## Versión

La versión sale de `ApplicationDisplayVersion` y `ApplicationVersion` en `VikingMobile.csproj`. Sube `ApplicationVersion` en cada publicación.

## Documentación

- [Contrato de sincronización con VIKING PC](docs/MOBILE_SYNC_CONTRACT.md)
- [Autenticación local](docs/MOBILE_AUTH_SETUP.md)
- [Recibos](docs/MOBILE_RECEIPTS.md)
- [Integración 1.0](docs/INTEGRACION_VIKING_MOBILE_1.0.md)

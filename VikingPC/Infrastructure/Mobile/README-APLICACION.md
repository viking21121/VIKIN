# Viking Mobile API — integración

Esta carpeta agrega la primera capa real de comunicación entre Viking y el móvil.

## Endpoints

### Health

GET:

`http://IP_DE_VIKING:5050/api/viking/health`

Respuesta:

```json
{
  "ok": true,
  "version": "1.0.2",
  "servicio": "Viking Mobile API"
}
```

### Registrar asistencia

POST:

`http://IP_DE_VIKING:5050/api/asistencia/registrar`

Body:

```json
{
  "codigoAlumno": "A0025",
  "qr": "VIKING|ALUMNO|A0025"
}
```

## Importante

El servidor HTTP NO registra directamente en SQLite.

La llamada termina en `IRegistrarAsistenciaMovil`, que debe reutilizar el caso de uso/repositorio de asistencia que ya existe en Viking.

## Activación

Desde el punto de arranque de Viking, después de inicializar la base de datos, se debe ejecutar:

```csharp
VikingMobileApiHost.Iniciar(
    request => RegistrarAsistenciaDesdeViking(request),
    5050);
```

El método `RegistrarAsistenciaDesdeViking` debe llamar al caso de uso actual de asistencia.

No se debe duplicar SQL dentro de esta carpeta.

## Firewall

Windows puede solicitar permiso para que Viking acepte conexiones en el puerto 5050.

El teléfono y la PC deben estar en la misma red Wi-Fi.

## Prueba inicial

Desde el navegador de otro dispositivo conectado a la misma red:

`http://IP_DE_VIKING:5050/api/viking/health`

Si devuelve JSON con `"ok": true`, la capa de red funciona.

## Por qué no se incluye aquí una llamada inventada a AppServices

El nombre/firma exacta del caso de uso de asistencia del proyecto fuente actual no está disponible en el material indexado de esta conversación. Este adaptador deja el punto de integración explícito para conectar la implementación real sin inventar una firma y romper la compilación.

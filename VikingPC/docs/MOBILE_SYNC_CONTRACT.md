# Contrato de sincronización Viking PC ↔ Viking Mobile

## Endpoint

`POST /api/asistencia/sincronizar`

La API utiliza el mismo caso de aplicación de registro de asistencia que el endpoint online:

`POST /api/asistencia/registrar`

Esto evita duplicar reglas de negocio entre el modo online y la sincronización offline.

## Request

```json
{
  "codigoAlumno": "A001",
  "qr": "A001",
  "actividadId": 1,
  "fechaHora": "2026-09-05T08:30:00"
}
```

Campos:
- `codigoAlumno`: obligatorio.
- `qr`: contenido leído del QR. El QR generado por Viking contiene el código del alumno.
- `actividadId`: actividad a la que pertenece el alumno.
- `fechaHora`: fecha/hora original en que se produjo la asistencia offline.

## Respuesta exitosa

HTTP `200`:

```json
{
  "ok": true,
  "mensaje": "Asistencia registrada correctamente para ..."
}
```

## Rechazo de negocio

HTTP `422`:

```json
{
  "ok": false,
  "mensaje": "No se encontró el alumno..."
}
```

Viking Mobile mantiene la asistencia pendiente cuando `ok` es `false`.

## Idempotencia

La tabla `asistencias` utiliza la combinación `(alumno_id, fecha)` como clave única lógica y `Marcar` usa:

`ON CONFLICT(alumno_id, fecha) DO UPDATE`

Por tanto, reenviar una asistencia pendiente ya aplicada no crea una segunda fila.

## Arquitectura

El endpoint HTTP vive en `Infrastructure/Mobile/VikingMobileApiServer`.

La lógica no accede directamente a SQLite. Se entrega a:

`IRegistrarAsistenciaMovil` → `RegistrarAsistenciaMovil` → casos/puertos de Application → repositorios SQLite.

El endpoint de sincronización reutiliza este mismo flujo para que las reglas sean idénticas al registro online.

## Identificador de operación (`idOperacion`)

El teléfono puede incluir `"idOperacion"` (texto único por escaneo) en `/api/asistencia/registrar` y `/api/asistencia/sincronizar`.

- Viking PC lo guarda en la tabla `asistencias_movil_ops` (clave primaria), dentro de la misma transacción que la asistencia.
- Si el mismo `idOperacion` llega otra vez, responde `ok: true` con el mensaje «ya estaba registrada», no repite el registro y no vuelve a mostrar el carnet.
- Es opcional: un Mobile antiguo que no lo envía sigue funcionando como antes.

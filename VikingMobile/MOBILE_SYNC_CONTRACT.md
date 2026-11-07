# Viking Mobile – Contrato de sincronización offline

## Endpoint

`POST /api/asistencia/sincronizar`

## Solicitud

El Mobile envía una asistencia pendiente con su fecha/hora original:

```json
{
  "codigoAlumno": "A001",
  "qr": "VIKING|ALUMNO|A001",
  "actividadId": 1,
  "fechaHora": "2026-09-06T09:15:00"
}
```

## Respuesta de éxito

```json
{
  "ok": true,
  "mensaje": "Asistencia sincronizada."
}
```

## Respuesta de rechazo

```json
{
  "ok": false,
  "mensaje": "El alumno no pertenece a la actividad seleccionada."
}
```

## Reglas que debe cumplir Viking PC

1. Validar que `codigoAlumno` exista.
2. Validar que el alumno pertenezca a `actividadId`.
3. Validar el QR cuando corresponda.
4. Registrar `fechaHora` recibida, no `DateTime.Today` del servidor.
5. No duplicar una asistencia si ya existe para el mismo alumno y fecha.
6. Un duplicado válido puede responder `ok: true` para que Mobile pueda marcarlo como sincronizado.
7. Una solicitud inválida debe responder `ok: false` con un mensaje claro.
8. El endpoint debe devolver HTTP 200 para respuestas de negocio (`ok: true/false`). Los errores inesperados pueden usar HTTP 5xx.

## Comportamiento del Mobile

- Las asistencias pendientes se mantienen localmente hasta recibir `ok: true`.
- Las rechazadas permanecen pendientes y guardan `UltimoError`.
- La sincronización manual usa este mismo endpoint.
- La sincronización automática intenta ejecutarse al conectar y periódicamente mientras el escáner está abierto en modo Online/Automático.
- Solo una sincronización puede ejecutarse a la vez.

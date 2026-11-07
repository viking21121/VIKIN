# Edición comercial 2.2

La aplicación está orientada a negocios que administran membresías, planes por clases, pagos y asistencia.

## Principios

- Arquitectura hexagonal: UI → casos de uso → puertos → adaptadores.
- Los alumnos conservan su identidad histórica; no se permite editar nombre, cédula o código QR desde la pantalla de edición.
- El cambio de plan y teléfono de un alumno se procesa mediante `ActualizarAlumno` + `IAlumnoRepository`.
- Las operaciones de edición relevantes generan auditoría cuando el repositorio está disponible.
- Los planes tienen descripción comercial y pueden ser por clases o por membresía de 30 días.

## Propiedad intelectual

Revisar `COPYRIGHT.md` y `LICENSE_PROPIETARIA.md` antes de distribuir el producto.

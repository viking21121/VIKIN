# Release 2.2 — Edición comercial

## Experiencia de uso
- Inicio convertido en centro de operaciones del establecimiento.
- Identidad comercial tomada de la configuración del establecimiento.
- Cabecera y controles con una línea visual consistente.
- Acceso visible a derechos de autor y versión del producto.
- Atajos principales comunicados desde la pantalla inicial.

## Edición segura de alumnos
- El usuario puede cambiar plan y teléfono.
- Nombre, cédula y código QR quedan protegidos desde `Editar cliente`.
- Dar de baja conserva historial y pasa por un caso de uso.
- Actualización y baja se registran en auditoría cuando está disponible.

## Arquitectura
- `ActualizarAlumno` y `DesactivarAlumno` viven en Application/UseCases.
- `IAlumnoRepository` define los puertos de lectura y escritura.
- `SqliteAlumnoRepository` implementa el adaptador de persistencia.
- `MainForm` y `EditarAlumnoForm` dejaron de llamar directamente a `Database` para estas operaciones.

## Propiedad intelectual
- Metadata de ensamblado con copyright y versión 2.2.0.
- `COPYRIGHT.md`.
- `LICENSE_PROPIETARIA.md`.
- `AcercaDeForm` accesible desde el menú principal.

## Nota de compilación
El entorno de trabajo usado para esta entrega no contiene el SDK de .NET. La solución fue revisada estáticamente, pero debe compilarse en Visual Studio/.NET 8 antes de distribución.

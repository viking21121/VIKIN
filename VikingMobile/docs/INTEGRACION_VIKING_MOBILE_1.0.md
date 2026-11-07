# Viking Mobile — Integración 1.0

- Autenticación de Cliente y Administrador gestionada por Mobile.
- Cliente: usuario = código de alumno.
- Administrador: usuario = nombre del establecimiento.
- PIN almacenado como hash SHA-256 en Mobile; no se envía al PC.
- Creación y recuperación de PIN mediante código de correo.
- Correo emisor configurado: avendanoalejandro841@gmail.com.
- El envío real requiere una contraseña de aplicación de Gmail, configurada desde Configuración > Correo de verificación.
- PC conserva la autoridad sobre alumnos, actividades y datos de negocio.
- API PC expone validación de identidad, cuenta cliente, dashboard admin y sincronización de asistencia.
- Navegación incluye Inicio/Atrás y cierre de sesión.

Nota: el código fuente fue preparado y validado estructuralmente, pero no compilado en este entorno porque no dispone del SDK .NET.

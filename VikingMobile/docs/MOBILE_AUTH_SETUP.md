# Viking Mobile 1.0 - Autenticación local y navegación

## Diseño
- Mobile administra las cuentas Mobile, PIN, sesiones y códigos de verificación.
- Viking PC sigue siendo la autoridad de alumnos, actividades y datos del negocio.
- Cliente: usuario = código del alumno.
- Administrador: usuario = nombre del establecimiento.
- El PIN nunca se envía ni se guarda en Viking PC.
- Los datos de cuenta del cliente pueden quedar en caché para consulta offline.

## Correo
Emisor configurado: avendanoalejandro841@gmail.com

En la pantalla Configuración > Correo de verificación se introduce una contraseña de aplicación de Google.
No usar la contraseña normal de Gmail.

Los códigos:
- tienen 6 dígitos;
- duran 10 minutos;
- permiten como máximo 5 intentos.

## Primera activación
1. Conectar Mobile con Viking PC.
2. Cliente: escribir código, pulsar CONFIGURAR MI PIN, correo, código y crear PIN.
3. Administrador: escribir exactamente el nombre del establecimiento, pulsar CONFIGURAR MI PIN, correo, código y crear PIN.

## Recuperación
La cuenta debe existir en Mobile. Se envía un nuevo código al correo registrado y se permite crear un PIN nuevo.

## Navegación
Las pantallas de login y cuenta/dashboard tienen navegación explícita. Cerrar sesión elimina la sesión de memoria.

## Dependencia PC
Esta versión espera los endpoints Mobile del Viking PC integrado:
- POST /api/cliente/validar
- POST /api/cliente/cuenta
- POST /api/admin/validar
- POST /api/admin/dashboard
- GET /api/actividades
- POST /api/asistencia/registrar
- POST /api/asistencia/sincronizar

## Nota de compilación
El entorno de preparación no tiene instalado el SDK de .NET, por lo que no se puede afirmar una compilación local aquí. El proyecto fue revisado estructuralmente, incluyendo XML/XAML y correspondencia de eventos.

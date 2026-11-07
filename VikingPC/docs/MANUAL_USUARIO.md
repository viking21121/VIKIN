# Manual de usuario — Asistencia 2.0

Sistema para piscinas/natación, CrossFit, funcional, gimnasios y academias. Cada actividad maneja sus propios alumnos y planes.

## Planes
Los planes muestran nombre, nivel, frecuencia, clases y precio. Ejemplo: `CrossFit · Intermedio · 3x/sem · 12 clases/mes · Por clases · $40`.

Un plan sin cantidad de clases es una **membresía de 30 días**.

## Nuevo alumno
1. Pulsa `+ Nuevo`.
2. Registra datos y QR.
3. Selecciona el plan leyendo la especificación completa.
4. Guarda.

En una membresía por tiempo se crea automáticamente una vigencia de 30 días.

## Cuenta regresiva
La pantalla principal muestra fecha de vencimiento y días restantes. Con 7 días o menos aparece en `Por vencer`; con 0 o menos queda `Vencida`.

Al registrar pagos, la membresía solo se renueva cuando los pagos acumulados del período cubren el precio completo del plan. Un abono parcial no reinicia la cuenta. Si la membresía sigue vigente, los 30 días siguientes parten desde su vencimiento; si ya venció, parten desde la fecha del pago.

## Planes por clases
El sistema controla clases tomadas y restantes. El filtro `Por vencer` considera 1 o 0 clases restantes.

## Pagos y recibos
Se conservan monto, tasa BCV, monto Bs, método, referencia y folio. Las referencias duplicadas se rechazan. El historial permite revisar los recibos.

## Actividades
Usa `Actividad` para cambiar entre Natación, CrossFit, Funcional, Gimnasio u otras actividades configuradas.

## Atajos
F2 asistencia rápida · F3 pago/abono · F4 nuevo alumno · F5 actualizar · Ctrl+F buscar · Esc limpiar búsqueda/filtro.

## Respaldos
Al cerrar se crean respaldos automáticos. Para venta comercial conviene copiar periódicamente la carpeta de backups fuera del equipo.

## Arquitectura
Se mantiene una arquitectura hexagonal híbrida: Presentation → Application/UseCases → Ports → Infrastructure/SQLite. El ciclo de pago usa `ICicloPagoRepository`, `ActualizarCicloPago` y `SqliteCicloPagoRepository`; `Database.cs` conserva código heredado mientras se completa la migración gradual.

## Siguiente nivel comercial
Caja diaria, cierres, permisos por usuario, morosos, reportes, horarios/turnos, grupos, notificaciones de vencimiento, exportación a Excel y restauración guiada de backups.

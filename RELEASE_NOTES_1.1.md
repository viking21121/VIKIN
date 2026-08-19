# VIKING 1.1 — Cambios

## Ciclo de pago individual por alumno
- Cada alumno puede tener una fecha de inscripción y una fecha de próximo pago propias.
- Al registrar un alumno, las fechas se pueden seleccionar manualmente.
- Las fechas también se pueden ajustar desde `EditarAlumnoForm`.
- El ciclo de pago se conserva en SQLite usando las columnas históricas `fecha_inicio_plan` y `fecha_vencimiento_plan` para mantener compatibilidad.
- Al completar la cuota del ciclo, VIKING avanza automáticamente el próximo pago un mes.
- Los abonos parciales no mueven el ciclo.
- El cálculo de cuota y abonado se realiza sobre el ciclo individual, no sobre el mes calendario.
- La arquitectura usa `ICicloPagoRepository` + `ActualizarCicloPago` + `SqliteCicloPagoRepository`.

## Asistencias en fechas anteriores
- La asistencia manual permite seleccionar una fecha anterior.
- El escáner de asistencia rápida también permite seleccionar la fecha.
- La carga offline ya utilizaba una fecha seleccionable y se conserva.
- Se limita la selección a hoy o fechas anteriores para evitar registrar asistencia futura accidentalmente.

## Selector de actividades
- Las tarjetas de actividades ahora tienen borde azul VIKING de 2 px.
- Se mantiene el centrado dinámico de las tarjetas.

## Arquitectura
- Los nuevos comportamientos se incorporan mediante casos de uso y puertos/adaptadores.
- No se añadió lógica nueva de negocio a `MainForm`.
- `Database.cs` conserva únicamente su papel de infraestructura heredada/migraciones, siguiendo la migración hexagonal híbrida.

# Panel de Gestión

La pantalla principal ahora está orientada a operación diaria del negocio.

## Indicadores
- Clientes activos.
- Asistencia de hoy.
- Atención requerida: membresías vencidas/por vencer y pagos pendientes.
- Cobrado del mes.
- Caja del día.
- Total por cobrar.

## Operación
El resumen operativo muestra caja, saldo pendiente, porcentaje de asistencia y número de clientes que requieren atención.

La tabla sigue siendo el centro de trabajo: búsqueda por nombre/código/cédula, plan completo, cobros, clases, vencimiento y estado.

## Arquitectura
Los indicadores nuevos se mantienen detrás del puerto `IMetricasRepository`; la consulta SQLite vive en `SqliteMetricasRepository`. La pantalla consume el caso de uso `ObtenerMetricasRapidas`.

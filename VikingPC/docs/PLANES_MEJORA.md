# Mejora de planes — versión 2.1

## Qué se mejoró

- Pantalla `AdministrarPlanesForm` rediseñada como panel comercial, con editor a la izquierda y catálogo de planes a la derecha.
- Modalidades explícitas: `Por clases` y `Membresía 30 días`.
- Frecuencia, nivel y clases se muestran con nombres legibles.
- Precio con formato monetario y validación.
- Botón para duplicar un plan y crear rápidamente variantes.
- Catálogos genéricos de nivel, frecuencia y clases preparados automáticamente por actividad cuando todavía no existen.
- Los planes antiguos pueden recuperar sus especificaciones desde sus opciones asociadas aunque los campos legacy estén vacíos.
- Los registros existentes no se modifican al desactivar o actualizar un plan desde esta pantalla; el cambio de plan de un alumno sigue siendo una operación separada.
- `PlanActividad.ToString()` ahora presenta una oferta entendible al seleccionar un plan desde el alta de alumno, evitando listas de opciones que solo muestran un nombre repetido.
- `AdministrarOpcionesPlanForm` rediseñado para que la configuración avanzada sea clara.

## Arquitectura

La UI no accede directamente a SQLite para administrar planes.

`Presentation -> Use Cases -> Ports -> SQLite Adapter`

Se añadió `PrepararCatalogoPlanActividad` para que la preparación de opciones genéricas permanezca detrás del puerto `IOpcionPlanActividadRepository`.

## Datos heredados

No se inventan niveles, frecuencias o cantidades para planes antiguos cuando la información original no existe. Se muestran como "Sin nivel", "Sin frecuencia" o "No usa clases" y pueden completarse desde `Administrar planes`.

## Importante

Esta mejora no modifica `NuevoAlumnoForm` ni `EditarAlumnoForm`. La selección del plan en el registro de alumnos utiliza únicamente la presentación legible de `PlanActividad`.

# Planes configurables por actividad

Cambios incluidos en este proyecto:

1. Catálogo configurable de opciones por actividad:
   - Nivel
   - Frecuencia
   - Clases mensuales
2. Administración de opciones desde `Presentation/Forms/AdministrarOpcionesPlanActividadForm.cs`.
3. Administración de planes por actividad usando las opciones configuradas.
4. Los planes pueden combinar nivel, frecuencia, clases mensuales o ninguna de esas características.
5. El precio pertenece al plan y tiene historial por `PlanId` para conservar vigencia por mes.
6. Los alumnos nuevos guardan `PlanId`.
7. Los alumnos existentes se migran al plan compatible con su frecuencia/clases cuando es posible.
8. `NuevoAlumnoForm` y `EditarAlumnoForm` usan los planes de la actividad actual.
9. Dashboard, historial, exportación y avisos muestran el nombre del plan y no la antigua frecuencia fija.
10. Precios se administran para los planes de la actividad actual.
11. Configuración, recibos y mensajes de WhatsApp usan la actividad del alumno/contexto actual.

## Verificación

Se realizó una revisión estática básica del código modificado. El entorno de ejecución de esta sesión no tiene `dotnet`/Visual Studio disponibles, por lo que la compilación final debe hacerse en Visual Studio antes de ejecutar la aplicación.

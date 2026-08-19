# Control de Asistencia - Natación (versión C# / WinForms)

App de escritorio en C# con WinForms + SQLite. Equivalente a la versión Python, misma lógica:
alta de alumno con selector de plan (4/3/2/1 veces por semana), marcar asistencia diaria, y un
dashboard que compara clases tomadas vs. clases del mes (En progreso / Completo / Excedido).

⚠️ **Nota importante**: este código fue escrito con mucho cuidado pero no pude compilarlo en este
entorno porque no tengo el SDK de .NET instalado ni acceso a internet. Revísalo la primera vez que
lo compiles en tu máquina, por si aparece algún error menor de sintaxis que se me haya escapado.

## Requisitos

- Windows (WinForms es específico de Windows)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) o superior
- Visual Studio 2022 (recomendado, pero no obligatorio) o solo el SDK + terminal

## Cómo compilar y ejecutar

### Opción A: con Visual Studio
1. Abre Visual Studio → "Abrir carpeta" → selecciona esta carpeta `AsistenciaNatacion_CSharp`.
2. Visual Studio detectará el `AsistenciaNatacion.csproj` y restaurará automáticamente el
   paquete NuGet `Microsoft.Data.Sqlite`.
3. Presiona F5 (o el botón ▶ "Iniciar") para compilar y ejecutar.

### Opción B: por terminal (con el SDK instalado)
Desde la carpeta del proyecto:

```
dotnet restore
dotnet build
dotnet run
```

Esto descarga automáticamente el paquete `Microsoft.Data.Sqlite` (necesita internet la primera vez).

## Estructura del proyecto

- `AsistenciaNatacion.csproj` — archivo de proyecto (.NET 8, WinForms, referencia a SQLite)
- `Program.cs` — punto de entrada
- `Models.cs` — clases `Alumno` y `EstadoAlumno`
- `Database.cs` — toda la lógica de base de datos (equivalente a `database.py`)
- `MainForm.cs` — ventana principal / dashboard
- `NuevoAlumnoForm.cs` — ventana emergente para dar de alta un alumno y elegir su plan
- `AsistenciaHoyForm.cs` — ventana para marcar presente/ausente el día de hoy
- `EditarAlumnoForm.cs` — ventana para editar el plan o dar de baja a un alumno
- `asistencia_natacion.db` — se crea automáticamente la primera vez que ejecutas el programa

## Cómo se usa

Igual que la versión Python:

1. **+ Nuevo alumno** → ventana con nombre, teléfono y selector de plan (4x/3x/2x/1x por semana).
   El campo "Clases del mes" se sugiere automáticamente pero es editable.
2. **Doble clic** en un alumno de la tabla (o botón "Marcar asistencia de hoy") → marca
   presente/ausente para hoy.
3. El **dashboard** central siempre muestra el estado de todos los alumnos del mes actual,
   con color: amarillo (en progreso), verde (completo), rojo (excedido).
4. **Editar alumno seleccionado** → cambia el plan/clases del mes o da de baja al alumno
   (conserva su historial).

## Diferencias con la versión Python

Funcionalmente son idénticas. La diferencia es solo tecnológica:
- Python usaba Tkinter + `sqlite3` (librería estándar, cero instalación).
- Esta versión usa WinForms + `Microsoft.Data.Sqlite` (requiere el SDK de .NET y NuGet).

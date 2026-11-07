VIKING PC 1.0.2 - MOBILE COMPATIBLE
====================================

Base: viking_logo.zip (the supplied Viking PC project).

This package keeps the existing automatic updater and existing PC functionality.
The Mobile API was extended only to support the current Viking Mobile client:

GET  /api/viking/health
GET  /api/actividades
POST /api/asistencia/registrar
POST /api/asistencia/sincronizar
POST /api/cliente/validar
POST /api/cliente/cuenta
GET  /api/cliente/recibo
POST /api/admin/validar
POST /api/admin/dashboard

Important:
- The local Mobile configuration remains a Mobile-side preference.
- ActivityId is honored for client/admin validation.
- Establishment names are compared ignoring case, accents and repeated spaces.
- The automatic updater files/settings were NOT redesigned or removed.
- The database is not replaced by this source package.

Build:
1. Open AsistenciaNatacion.csproj in Visual Studio.
2. Restore NuGet packages.
3. Rebuild the solution.
4. Confirm 0 errors.
5. Publish using the existing publish configuration so VikingUpdater.exe is copied.

Do not copy an old Viking.exe from a previous ZIP over the newly published build.

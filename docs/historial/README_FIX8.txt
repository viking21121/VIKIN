Viking Mobile Fix8 basado en Fix7

Cambios:
1. Se conserva la configuración local de servidor, actividad y modo de validación.
2. ClienteService ahora envía actividadId también al endpoint api/cliente/cuenta.
3. Se agregan consulta, visualización y descarga/compartición de recibos desde Mi Cuenta.
4. El canal Wi-Fi conserva el flujo de Fix7 y amplía el timeout de conexión de 5 a 10 segundos.
5. Los errores de timeout ahora muestran un mensaje claro.

Importante:
- La dirección por defecto sigue siendo http://192.168.1.6:5050.
- No se modifica el flujo de configuración local.
- Para validar contra Viking PC, el PC debe exponer los endpoints usados por Mobile.

No se pudo ejecutar dotnet build en este entorno porque .NET SDK no está instalado aquí. Se debe compilar en Visual Studio y confirmar 0 errores antes de probar en el teléfono.

# Instalador y firma de VIKING

## Qué hace el instalador

`installer/VIKING.iss` (Inno Setup 6.3 o superior) genera `VIKING-Setup-X.Y.Z.exe`. Al ejecutarlo en un equipo nuevo:

1. Instala VIKING en `C:\VIKING` y da permiso de escritura a los usuarios de Windows.
2. Crea el acceso directo en el menú Inicio (y en el escritorio si se marca la opción).
3. Reserva el puerto 5050 para la API de VikingMobile (`netsh http add urlacl`), así VIKING puede escuchar en la red sin ejecutarse como administrador.
4. Crea la regla de firewall **VIKING API** (entrada, TCP 5050) solo para redes **privadas y de dominio**.
5. Ofrece abrir VIKING al terminar.

Se pide permiso de administrador una sola vez, por los pasos 3 y 4.

**Instalar encima de una versión anterior** actualiza los archivos y conserva la base de datos, los respaldos y los registros. El instalador nunca incluye ni borra `asistencia_natacion.db`. **Desinstalar** quita el programa, la regla de firewall y la reserva del puerto, pero deja los datos.

### Por qué `C:\VIKING` y no «Archivos de programa»

VIKING guarda la base de datos, los respaldos y los registros junto al ejecutable, y el actualizador automático reemplaza archivos sin pedir administrador. En «Archivos de programa» no podría escribir. La contrapartida: cualquier usuario del equipo puede modificar esa carpeta. Para una laptop de un negocio es razonable; si se quisiera endurecer, habría que mover los datos a `%ProgramData%\VIKING` y dejar el programa en «Archivos de programa» (cambio de código pendiente).

### Redes públicas

La regla de firewall no abre el puerto en redes marcadas como **Públicas**. Si el teléfono no conecta, en Windows: Configuración → Red e Internet → Wi-Fi → la red → perfil de red **Privada**.

## Cómo se genera

- **Automático:** al subir un tag `vX.Y.Z`, el flujo *Publicar VIKING* compila la aplicación, empaqueta `VIKING.zip` (lo que usa el actualizador automático), construye el instalador y publica los dos en el Release de GitHub.
- **A mano:** publica en `publish\VIKING` y ejecuta
  ```powershell
  & "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.4 installer\VIKING.iss
  ```

## Firma del ejecutable (pendiente)

Sin firma, Windows SmartScreen muestra «Windows protegió su PC» al ejecutar el instalador o VIKING.exe. Firmar requiere un **certificado de firma de código**, que se compra a una entidad certificadora (o un servicio de firma como Azure Trusted Signing). Todavía no hay uno configurado, así que el flujo actual **no firma**.

Cuando exista el certificado, el camino es:

1. Guardar el `.pfx` (en base64) y su contraseña como secretos del repositorio en GitHub.
2. En el flujo de publicación, antes de empaquetar, firmar con `signtool`:
   ```powershell
   signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /f cert.pfx /p $env:CERT_PASSWORD publish\VIKING\Viking.exe publish\VIKING\VikingUpdater.exe
   ```
3. Firmar también el instalador ya construido (`VIKING-Setup-X.Y.Z.exe`).
4. Con un certificado nuevo, SmartScreen puede seguir avisando durante un tiempo hasta que el archivo gane reputación; con certificados EV desaparece de inmediato.

; Instalador de VIKING (Inno Setup 6.3 o superior)
;
; Se compila desde la raíz del repositorio, después de publicar en publish\VIKING:
;   ISCC.exe /DAppVersion=1.0.4 installer\VIKING.iss
; y genera publish\VIKING-Setup-1.0.4.exe

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppExe "Viking.exe"
#define ApiPort "5050"

[Setup]
; No cambiar el AppId: permite que una versión nueva actualice la instalada.
AppId={{DC6C8087-CD41-408D-A46F-987197D77446}
AppName=VIKING
AppVersion={#AppVersion}
AppVerName=VIKING {#AppVersion}
AppPublisher=VIKING
VersionInfoVersion={#AppVersion}

; Se instala en C:\VIKING, no en Archivos de programa, porque VIKING guarda la
; base de datos, los respaldos y los registros junto al ejecutable y el
; actualizador reemplaza archivos sin pedir permisos de administrador.
DefaultDirName={sd}\VIKING
DisableProgramGroupPage=yes
DefaultGroupName=VIKING

; Se pide administrador una sola vez para reservar el puerto de la API móvil
; y abrir el firewall (ver [Run]).
PrivilegesRequired=admin

OutputDir=..\publish
OutputBaseFilename=VIKING-Setup-{#AppVersion}
SetupIconFile=..\viking_logo.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Dirs]
; Los usuarios de Windows pueden escribir aquí (base de datos, respaldos, registros).
Name: "{app}"; Permissions: users-modify

[Files]
; La base de datos NO se incluye nunca: una actualización no debe tocar los datos del cliente.
Source: "..\publish\VIKING\*"; DestDir: "{app}"; Excludes: "*.db,*.db-wal,*.db-shm,Backups\*,Logs\*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\VIKING"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\VIKING"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
; Permite que VIKING escuche en la red local sin ejecutarse como administrador.
; Se borra primero por si ya existía de una instalación anterior.
Filename: "{sys}\netsh.exe"; Parameters: "http delete urlacl url=http://+:{#ApiPort}/"; Flags: runhidden; StatusMsg: "Preparando la conexión con VikingMobile..."
Filename: "{sys}\netsh.exe"; Parameters: "http add urlacl url=http://+:{#ApiPort}/ sddl=D:(A;;GX;;;WD)"; Flags: runhidden

; Regla de firewall solo para redes privadas y de dominio (no redes públicas).
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""VIKING API"""; Flags: runhidden
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""VIKING API"" dir=in action=allow protocol=TCP localport={#ApiPort} profile=private,domain program=""{app}\{#AppExe}"""; Flags: runhidden

Filename: "{app}\{#AppExe}"; Description: "Abrir VIKING"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""VIKING API"""; Flags: runhidden; RunOnceId: "QuitarReglaFirewall"
Filename: "{sys}\netsh.exe"; Parameters: "http delete urlacl url=http://+:{#ApiPort}/"; Flags: runhidden; RunOnceId: "QuitarReservaUrl"

; Al desinstalar NO se borran la base de datos, los respaldos ni los registros.

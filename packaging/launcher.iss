#define MyAppName "Rapor Araclari Launcher"
#define MyAppExeName "RaporAraclari.Launcher.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.1"
#endif

#ifndef SourceDir
  #define SourceDir "..\artifacts\publish\win-x64"
#endif

[Setup]
AppId={{4B915C5A-AD4E-4F70-A82E-2D7B8B7D7F49}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Eren Bektas
AppPublisherURL=https://github.com/erenbektas
DefaultDirName={autopf}\RaporAraclariLauncher
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=RaporAraclariLauncher-v{#MyAppVersion}-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaustu kisayolu olustur"; GroupDescription: "Ek gorevler:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

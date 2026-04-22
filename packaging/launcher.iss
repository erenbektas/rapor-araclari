#define MyAppName "Rapor Araclari Launcher"
#define MyAppExeName "RaporAraclari.Launcher.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.2"
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

[Code]
const
  DotNetDesktopRuntimeUrl = 'https://aka.ms/dotnet/8.0/windowsdesktop-runtime-win-x64.exe';
  DotNetDesktopRuntimeFileName = 'windowsdesktop-runtime-win-x64.exe';

var
  DownloadPage: TDownloadWizardPage;

function HasWindowsDesktopRuntime8: Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf}\dotnet\shared\Microsoft.WindowsDesktop.App\8.*'), FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function EnsureWindowsDesktopRuntime: Boolean;
var
  Error: String;
  InstallerPath: String;
  ResultCode: Integer;
begin
  Result := True;
  if HasWindowsDesktopRuntime8 then
    exit;

  if SuppressibleMsgBox(
    '.NET 8 Windows Desktop Runtime bulunamadi.'#13#13 +
    'Launcher''in acilabilmesi icin bu bilesen gereklidir.'#13 +
    'Kurulum bu bileseni simdi indirip kurabilir. Devam edilsin mi?',
    mbConfirmation, MB_YESNO, IDYES) <> IDYES then
  begin
    Result := False;
    exit;
  end;

  DownloadPage.Clear;
  DownloadPage.Add(DotNetDesktopRuntimeUrl, DotNetDesktopRuntimeFileName, '');
  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
    except
      if DownloadPage.AbortedByUser then
        Log('Runtime indirmesi kullanici tarafindan iptal edildi.')
      else
      begin
        Error := Format('%s: %s', [DownloadPage.LastBaseNameOrUrl, GetExceptionMessage]);
        SuppressibleMsgBox(AddPeriod(Error), mbCriticalError, MB_OK, IDOK);
      end;
      Result := False;
      exit;
    end;
  finally
    DownloadPage.Hide;
  end;

  InstallerPath := ExpandConstant('{tmp}\' + DotNetDesktopRuntimeFileName);
  if not Exec(InstallerPath, '/install /quiet /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
  begin
    SuppressibleMsgBox('.NET 8 Windows Desktop Runtime kurulumu baslatilamadi.', mbCriticalError, MB_OK, IDOK);
    Result := False;
    exit;
  end;

  if (ResultCode <> 0) and (ResultCode <> 3010) then
  begin
    SuppressibleMsgBox(
      Format('.NET 8 Windows Desktop Runtime kurulumu basarisiz oldu. Cikis kodu: %d', [ResultCode]),
      mbCriticalError, MB_OK, IDOK);
    Result := False;
    exit;
  end;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
  DownloadPage.ShowBaseNameInsteadOfUrl := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpReady then
    Result := EnsureWindowsDesktopRuntime;
end;

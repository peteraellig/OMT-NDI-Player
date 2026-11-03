; Compile with Inno Setup 7, or run Build-Installer.ps1 to publish and compile.
#define PublishDir SourcePath + "artifacts\publish\win-x64"
#define AppVersion GetVersionNumbersString(PublishDir + "\OMTplay.exe")

[Setup]
AppId={{38B96392-A52D-4EFE-86D3-B51F57F1E91C}
AppName=OMTplay
AppVersion={#AppVersion}
AppPublisher=Peter Aellig
AppPublisherURL=https://www.tvcrew.ch
DefaultDirName={autopf}\OMTplay
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\OMTplay.exe
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
PrivilegesRequired=admin
OutputDir=artifacts\installer
OutputBaseFilename=OMTplay-{#AppVersion}-Setup-x64
SetupIconFile=OMTplay.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#AppVersion}

[Languages]
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "OMTplay\third_party\omt\LICENSE.txt"; DestDir: "{app}\licenses"; DestName: "OMT-LICENSE.txt"; Flags: ignoreversion
Source: "installer\licenses\*.txt"; DestDir: "{app}\licenses"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\OMTplay"; Filename: "{app}\OMTplay.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\OMTplay"; Filename: "{app}\OMTplay.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\OMTplay.exe"; Description: "{cm:LaunchProgram,OMTplay}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

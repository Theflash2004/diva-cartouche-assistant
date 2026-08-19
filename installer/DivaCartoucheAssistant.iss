#define MyAppName "Diva Cartouche Assistant"
#define MyAppVersion GetVersionNumbersString("..\publish\DivaCartoucheAssistant.exe")
#define MyAppPublisher "Diva Cartouche Assistant contributors"
#define MyAppExeName "DivaCartoucheAssistant.exe"

[Setup]
AppId={{B2D0E7E7-CC8C-4F37-B1E8-7E5B7FD2E4A7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\DivaCartoucheAssistant
DefaultGroupName={#MyAppName}
PrivilegesRequired=lowest
OutputDir=..\artifacts
OutputBaseFilename=DivaCartoucheAssistant-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\Assets\diva-cat-logo.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Files]
Source: "..\publish\DivaCartoucheAssistant.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\Diva-cartouche-assistant-guide.pdf"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{autodesktop}\Diva Cartouche Assistant"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\Diva Cartouche Assistant"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Lancer Diva Cartouche Assistant"; Flags: nowait postinstall skipifsilent

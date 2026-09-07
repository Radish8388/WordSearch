[Setup]
AppName=Word Search
AppVersion=1.0.0
DefaultDirName={autopf}\Radish\Word Search
DefaultGroupName=Radish
SetupIconFile=images\search2.ico
UninstallDisplayIcon={app}\Word Search.exe
LicenseFile=LICENSE.txt
OutputBaseFilename=WordSearchSetup
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
AppPublisher=Radish
AppPublisherURL=https://radish-vert.vercel.app
AppId={{b4ae948c-580c-476e-9e57-06de1fa30e06}

[Files]
Source: "bin\Release\net10.0-windows\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Word Search"; Filename: "{app}\Word Search.exe"
Name: "{commondesktop}\Word Search"; Filename: "{app}\Word Search.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\Word Search.exe"; Description: "Launch Word Search"; Flags: nowait postinstall skipifsilent

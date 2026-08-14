; Traços 3D Studio — instalador Windows (Inno Setup 6)
; Pré-requisito: executar installer\publish.ps1 para gerar publish\win-x64

#define MyAppName "Traços 3D Studio"
; MyAppVersion / MyVersionLabel são passados via publish.ps1
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef MyVersionLabel
  #define MyVersionLabel "desenvolvimento"
#endif
#define MyAppPublisher "Traços"
#define MyAppExeName "Tracos3DStudio.exe"

[Setup]
AppId={{A7B3C4D5-E6F7-4890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} (build {#MyVersionLabel})
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=Tracos3DStudio-setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Build {#MyVersionLabel}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "Build {#MyVersionLabel}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Abrir {#MyAppName} (build {#MyVersionLabel})"; Flags: nowait postinstall skipifsilent

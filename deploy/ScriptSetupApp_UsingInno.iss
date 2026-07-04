#define MyAppName "PackWatch"
#define MyAppVersion "1.0.0"
#define MyAppExeName "PackWatch.App.exe"
#define RepoRoot AddBackslash(SourcePath) + ".."
#define DesktopProjectPath AddBackslash(RepoRoot) + "src\\PackWatch.App\\PackWatch.App.csproj"
#define BuildWorkingDir RepoRoot
#define BuildParams "publish """ + DesktopProjectPath + """ -c Release -f net8.0-windows10.0.19041.0 -r win-x64 --self-contained true -o """ + AddBackslash(RepoRoot) + "deploy\\dist"""
#define BuildExitCode Exec("dotnet", BuildParams, BuildWorkingDir, 1)

; Auto-build PackWatch.App in Release mode each time this installer script is compiled.
#if BuildExitCode != 0
  #error "dotnet publish failed with exit code " + Str(BuildExitCode)
#endif

#define MyAppSourceDir "dist"
#define MyAppIcon "..\src\PackWatch.App\icon.ico"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename={#MyAppName}_Setup
SetupIconFile={#MyAppIcon}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional options:"

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

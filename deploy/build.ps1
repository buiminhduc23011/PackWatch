param(
    [string]$OutputDir     = 'dist',
    [string]$Configuration = 'Release',
    [switch]$SelfContained,
    [string]$Runtime       = 'win-x64'
)

$ErrorActionPreference = 'Stop'

# --- Resolve paths ------------------------------------------------------------
$ScriptDir    = $PSScriptRoot                           # deploy/
$RepoRoot     = $ScriptDir | Split-Path -Parent         # repository root
$ProjectPath  = Join-Path $RepoRoot 'src\PackWatch.App\PackWatch.App.csproj'
$OutPath      = if ([System.IO.Path]::IsPathRooted($OutputDir)) { $OutputDir } else { Join-Path $RepoRoot $OutputDir }

Write-Host ''
Write-Host '========================================================' -ForegroundColor Cyan
Write-Host '|         PackWatch - Build and Package Script         |' -ForegroundColor Cyan
Write-Host '========================================================' -ForegroundColor Cyan
Write-Host ''
Write-Host "  Repo        : $RepoRoot"
Write-Host "  Output      : $OutPath"
Write-Host "  Config      : $Configuration"
if ($SelfContained) {
    Write-Host "  Mode        : Self-contained ($Runtime)"
} else {
    Write-Host '  Mode        : Framework-dependent'
}
Write-Host ''

# --- Step 1 : Publish WPF App ------------------------------------------------
Write-Host '[1/2] Publishing WPF Application...' -ForegroundColor Yellow

if (Test-Path $OutPath) { Remove-Item $OutPath -Recurse -Force }

$publishArgs = @(
    'publish', $ProjectPath,
    '-c', $Configuration,
    '-o', $OutPath,
    '-f', 'net8.0-windows10.0.19041.0'
)

if ($SelfContained) {
    $publishArgs += '--self-contained', 'true', '-r', $Runtime
} else {
    $publishArgs += '--self-contained', 'false'
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
Write-Host '      WPF App publish complete.' -ForegroundColor Green

# --- Step 2 : Copy GUIDE.md --------------------------------------------------
Write-Host ''
Write-Host '[2/2] Copying guide...' -ForegroundColor Yellow

$srcGuide = Join-Path $ScriptDir 'GUIDE.md'
if (Test-Path $srcGuide) {
    Copy-Item $srcGuide -Destination $OutPath -Force
    Write-Host "      Copied GUIDE.md" -ForegroundColor DarkGray
}

Write-Host ''
Write-Host '========================================================' -ForegroundColor Green
Write-Host '|               BUILD COMPLETE!                        |' -ForegroundColor Green
Write-Host '========================================================' -ForegroundColor Green
Write-Host ''
Write-Host "  Package location : $OutPath"
Write-Host ''

$ErrorActionPreference = 'Stop'
$ScriptDir = $PSScriptRoot

# --- Run build first ---------------------------------------------------------
Write-Host '========================================================' -ForegroundColor Cyan
Write-Host '|          PackWatch - Deploy / Package Builder        |' -ForegroundColor Cyan
Write-Host '========================================================' -ForegroundColor Cyan
Write-Host ''

# Run build.ps1 to publish the application into 'deploy/dist' as Self-Contained (embed .NET)
& "$ScriptDir\build.ps1" -OutputDir "$ScriptDir\dist" -SelfContained

# --- Run Inno Setup if available ---------------------------------------------
Write-Host ''
Write-Host 'Checking for Inno Setup Compiler (ISCC.exe)...' -ForegroundColor Yellow

$isccPath = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($isccPath) {
    Write-Host "Found Inno Setup Compiler at: $isccPath" -ForegroundColor Green
    Write-Host 'Compiling Installer using Inno Setup...' -ForegroundColor Yellow
    
    # Run ISCC
    & $isccPath "$ScriptDir\ScriptSetupApp_UsingInno.iss"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ''
        Write-Host '========================================================' -ForegroundColor Green
        Write-Host '|       INSTALLER CREATED SUCCESSFULLY!                |' -ForegroundColor Green
        Write-Host '========================================================' -ForegroundColor Green
        Write-Host "  Installer Location: $ScriptDir\Output\PackWatch_Setup.exe" -ForegroundColor Green
        Write-Host ''
    } else {
        Write-Warning 'Inno Setup compiler reported an error during build.'
    }
} else {
    Write-Host ''
    Write-Host '========================================================' -ForegroundColor Yellow
    Write-Host '|  Inno Setup Compiler not found on this machine.      |' -ForegroundColor Yellow
    Write-Host '========================================================' -ForegroundColor Yellow
    Write-Host '  - The application was published successfully to:'
    Write-Host "    $ScriptDir\dist\" -ForegroundColor Green
    Write-Host '  - To build the Installer (.exe), please install Inno Setup 6'
    Write-Host '    and compile ScriptSetupApp_UsingInno.iss manually.'
    Write-Host ''
}

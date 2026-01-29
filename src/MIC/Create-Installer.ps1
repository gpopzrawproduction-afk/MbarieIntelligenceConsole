<#
.SYNOPSIS
  Creates a standalone installer for MIC (Mbarie Intelligence Console)
.DESCRIPTION
  This script publishes MIC.Desktop.Avalonia as a self-contained application,
  creates an installer package in Release\Installer, and generates setup scripts.
.EXAMPLE
  PS> .\Create-Installer.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "=== MIC Installer Creation Script ===" -ForegroundColor Cyan

# 1. Clean Release\Installer directory
Write-Host "1. Cleaning Release\Installer directory..." -ForegroundColor Yellow
if (Test-Path "Release\Installer") {
    Remove-Item -Path "Release\Installer\*" -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -Path "Release\Installer" -ItemType Directory -Force | Out-Null
}

# 2. Publish the application as self-contained
Write-Host "2. Publishing MIC.Desktop.Avalonia as self-contained..." -ForegroundColor Yellow
$publishDir = "MIC.Desktop.Avalonia\bin\Release\net9.0\win-x64\publish"
dotnet publish MIC.Desktop.Avalonia\MIC.Desktop.Avalonia.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishReadyToRun=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "Publish failed"
}

# 3. Copy published files to Release\Installer
Write-Host "3. Copying published files to Release\Installer..." -ForegroundColor Yellow
Copy-Item -Path "$publishDir\*" -Destination "Release\Installer\" -Recurse -Force

# 4. Create application shortcut batch file
Write-Host "4. Creating application launchers..." -ForegroundColor Yellow

# Create Windows batch file launcher
$batchContent = @'
@echo off
echo Starting Mbarie Intelligence Console...
echo.
echo If this is your first time running the application, make sure:
echo 1. You have .NET 9.0 Runtime installed (if not using self-contained)
echo 2. Your database connection is properly configured
echo.
MIC.Desktop.Avalonia.exe
pause
'@

Set-Content -Path "Release\Installer\Run-MIC.bat" -Value $batchContent -Encoding ASCII

# Create PowerShell launcher
$psLauncher = @'
<#
.SYNOPSIS
  Launcher for Mbarie Intelligence Console
#>
Write-Host "Starting Mbarie Intelligence Console..." -ForegroundColor Cyan
Write-Host "Application: $PSScriptRoot\MIC.Desktop.Avalonia.exe" -ForegroundColor Yellow

# Check for .NET runtime
try {
    dotnet --list-runtimes | findstr "Microsoft.NETCore.App 9." > $null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ .NET 9.0 Runtime detected" -ForegroundColor Green
    } else {
        Write-Host "⚠ .NET 9.0 Runtime not found. The application may not run." -ForegroundColor Yellow
        Write-Host "  Download from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ Could not check .NET runtime" -ForegroundColor Yellow
}

# Run the application
Start-Process -FilePath "$PSScriptRoot\MIC.Desktop.Avalonia.exe" -Wait
'@

Set-Content -Path "Release\Installer\Launch-MIC.ps1" -Value $psLauncher -Encoding UTF8

# 5. Create README file
Write-Host "5. Creating README and documentation..." -ForegroundColor Yellow

$readmeContent = @'
# Mbarie Intelligence Console (MIC)

## Installation

### Quick Start
1. Copy the entire "Installer" folder to your desired location
2. Run `Run-MIC.bat` (Windows) or `Launch-MIC.ps1` (PowerShell)

### System Requirements
- Windows 10/11 (64-bit)
- .NET 9.0 Runtime (if not using self-contained build)
- 4GB RAM minimum
- 500MB free disk space

### First Time Setup
1. Run the application
2. Configure database connection in Settings
3. Set up email accounts for email intelligence features
4. Configure AI services for advanced analytics

### Configuration Files
- `appsettings.json`: Main configuration file
- `appsettings.Development.json`: Development settings (if exists)
- `appsettings.Production.json`: Production settings (if exists)

## Features
- Email intelligence and analysis
- Real-time dashboard with operational metrics
- AI-driven insights and alerts
- Asset monitoring and management
- Multi-user authentication

## Troubleshooting

### Application won't start
1. Ensure .NET 9.0 Runtime is installed
2. Check Windows Event Viewer for errors
3. Verify all files are present in the installation directory

### Database connection errors
1. Check connection string in appsettings.json
2. Ensure PostgreSQL is running (if using PostgreSQL)
3. Verify database credentials

### Email synchronization issues
1. Check email account credentials
2. Verify internet connection
3. Ensure email provider settings are correct

## Support
For issues and feature requests, contact MBARIE Services Ltd.

## Version
1.0.0
Built: {BUILD_DATE}
'@ -replace "{BUILD_DATE}", (Get-Date).ToString("yyyy-MM-dd")

Set-Content -Path "Release\Installer\README.txt" -Value $readmeContent -Encoding UTF8

# 6. Create a simple setup script
Write-Host "6. Creating setup script..." -ForegroundColor Yellow

$setupScript = @'
<#
.SYNOPSIS
  Setup script for Mbarie Intelligence Console
.DESCRIPTION
  This script helps set up MIC on a new system
#>
param(
    [string]$InstallPath = "$env:ProgramFiles\MBARIE Intelligence Console"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Mbarie Intelligence Console Setup ===" -ForegroundColor Cyan

# Check for administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "⚠ This setup may require administrator privileges for certain operations." -ForegroundColor Yellow
}

# Create installation directory
Write-Host "Creating installation directory: $InstallPath" -ForegroundColor Yellow
New-Item -Path $InstallPath -ItemType Directory -Force | Out-Null

# Copy files
Write-Host "Copying application files..." -ForegroundColor Yellow
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Copy-Item -Path "$scriptPath\*" -Destination $InstallPath -Recurse -Force

# Create desktop shortcut (if admin)
if ($isAdmin) {
    Write-Host "Creating desktop shortcut..." -ForegroundColor Yellow
    $WshShell = New-Object -ComObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Mbarie Intelligence Console.lnk")
    $Shortcut.TargetPath = "$InstallPath\MIC.Desktop.Avalonia.exe"
    $Shortcut.WorkingDirectory = $InstallPath
    $Shortcut.Description = "Mbarie Intelligence Console"
    $Shortcut.Save()
}

Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
Write-Host "Installation directory: $InstallPath" -ForegroundColor Yellow
Write-Host "To run the application:" -ForegroundColor Yellow
Write-Host "1. Navigate to: $InstallPath" -ForegroundColor Cyan
Write-Host "2. Run: MIC.Desktop.Avalonia.exe" -ForegroundColor Cyan
Write-Host "`nOr use the desktop shortcut (if created)" -ForegroundColor Cyan

if (-not $isAdmin) {
    Write-Host "`n⚠ Note: Run this script as Administrator to create desktop shortcuts and install to Program Files" -ForegroundColor Yellow
}
'@

Set-Content -Path "Release\Installer\Setup-MIC.ps1" -Value $setupScript -Encoding UTF8

# 7. Create a simple uninstall script
Write-Host "7. Creating uninstall script..." -ForegroundColor Yellow

$uninstallScript = @'
<#
.SYNOPSIS
  Uninstall script for Mbarie Intelligence Console
#>
param(
    [string]$InstallPath = "$env:ProgramFiles\MBARIE Intelligence Console"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Mbarie Intelligence Console Uninstall ===" -ForegroundColor Cyan

# Check if installation directory exists
if (-not (Test-Path $InstallPath)) {
    Write-Host "Installation not found at: $InstallPath" -ForegroundColor Yellow
    Write-Host "Please specify the correct installation path using -InstallPath parameter" -ForegroundColor Yellow
    exit 1
}

Write-Host "Removing installation directory: $InstallPath" -ForegroundColor Yellow
Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction SilentlyContinue

# Remove desktop shortcut if exists
$desktopShortcut = "$env:USERPROFILE\Desktop\Mbarie Intelligence Console.lnk"
if (Test-Path $desktopShortcut) {
    Write-Host "Removing desktop shortcut..." -ForegroundColor Yellow
    Remove-Item -Path $desktopShortcut -Force -ErrorAction SilentlyContinue
}

Write-Host "`n=== Uninstall Complete ===" -ForegroundColor Green
Write-Host "Mbarie Intelligence Console has been removed from your system." -ForegroundColor Yellow
Write-Host "Note: User data and configuration files in AppData may still exist." -ForegroundColor Yellow
'@

Set-Content -Path "Release\Installer\Uninstall-MIC.ps1" -Value $uninstallScript -Encoding UTF8

# 8. Summary
Write-Host "`n=== Installer Creation Complete ===" -ForegroundColor Green
Write-Host "Installer files created in: Release\Installer\" -ForegroundColor Yellow
Write-Host "`nContents:" -ForegroundColor Cyan
Get-ChildItem "Release\Installer\" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Test the installer by running: Release\Installer\Run-MIC.bat" -ForegroundColor Yellow
Write-Host "2. For full installation, run: Release\Installer\Setup-MIC.ps1" -ForegroundColor Yellow
Write-Host "3. Package the Release\Installer folder for distribution" -ForegroundColor Yellow

Write-Host "`nTotal size:" -ForegroundColor Cyan
$size = (Get-ChildItem "Release\Installer\" -Recurse | Measure-Object -Property Length -Sum).Sum
Write-Host "$([math]::Round($size/1MB, 2)) MB" -ForegroundColor Yellow
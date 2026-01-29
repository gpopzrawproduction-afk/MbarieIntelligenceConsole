<#
.SYNOPSIS
    Sets up the Mbarie Intelligence Console development environment.
.DESCRIPTION
    Restores NuGet packages, builds all projects, runs tests,
    applies EF Core migrations, and launches MIC.Console.
    Optionally checks for PostgreSQL for full DB support.
.NOTES
    Author: Haroon Ahmed Amin (GpopzRaw)
    Date: 2026-01-24
#>

Write-Host "=== Starting MIC Development Setup ===`n"

# ----------------------------
# 1. Check for PostgreSQL
# ----------------------------
Write-Host "Checking for PostgreSQL (psql)..."
try {
    $psqlVersion = & psql --version 2>$null
    if ($psqlVersion) {
        Write-Host "PostgreSQL detected: $psqlVersion`n"
    } else {
        Write-Host "PostgreSQL not detected. MIC will default to SQLite.`n"
    }
} catch {
    Write-Host "PostgreSQL not detected. MIC will default to SQLite.`n"
}

# ----------------------------
# 2. Restore NuGet packages
# ----------------------------
Write-Host "Restoring NuGet packages..."
dotnet restore
Write-Host "Restore complete.`n"

# ----------------------------
# 3. Build all projects
# ----------------------------
Write-Host "Building solution (Debug)..."
dotnet build -c Debug
Write-Host "Build complete.`n"

# ----------------------------
# 4. Run Unit, Integration, and E2E tests
# ----------------------------
Write-Host "Running all Unit, Integration, and E2E tests..."
dotnet test -c Debug
Write-Host "Tests completed.`n"

# ----------------------------
# 5. Apply EF Core migrations
# ----------------------------
Write-Host "Applying EF Core migrations..."
$efProject = "C:\MbarieIntelligenceConsole\src\mic\MIC.Infrastructure.Data"
$startupProject = "C:\MbarieIntelligenceConsole\src\mic\MIC.Console"

# Ensure database is up to date
dotnet ef database update -p $efProject -s $startupProject
Write-Host "Database is up to date.`n"

# ----------------------------
# 6. Launch MIC.Console
# ----------------------------
Write-Host "Launching MIC.Console..."
dotnet run --project $startupProject
Write-Host "`n=== MIC Development Setup Complete ==="

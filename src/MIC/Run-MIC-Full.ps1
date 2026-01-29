# ==============================================================
# Mbarie Intelligence Console (MIC) Full Dev Script
# Automates: Restore, Build, Test, Migrations, Run
# Optional: PostgreSQL configuration and EF Core value comparers
# ==============================================================

# Set script to stop on errors
$ErrorActionPreference = "Stop"

Write-Host "========== MIC Development Script Started ==========" -ForegroundColor Cyan

# 1️⃣ Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore C:\MbarieIntelligenceConsole\src\mic\MIC.slnx

# 2️⃣ Build the solution
Write-Host "Building the solution..." -ForegroundColor Yellow
dotnet build C:\MbarieIntelligenceConsole\src\mic\MIC.slnx -c Debug

# 3️⃣ Run all unit, integration, and E2E tests
Write-Host "Running all tests..." -ForegroundColor Yellow
dotnet test C:\MbarieIntelligenceConsole\src\mic\MIC.slnx -c Debug

# 4️⃣ Navigate to the data project for migrations
Write-Host "Navigating to MIC.Infrastructure.Data..." -ForegroundColor Yellow
cd C:\MbarieIntelligenceConsole\src\mic\MIC.Infrastructure.Data

# 5️⃣ Add pending migration (if any)
Write-Host "Adding pending migration..." -ForegroundColor Yellow
dotnet ef migrations add PendingModelChanges -s ..\MIC.Console

# 6️⃣ Update the database
Write-Host "Updating database..." -ForegroundColor Yellow
dotnet ef database update -s ..\MIC.Console

# 7️⃣ Optional: Configure PostgreSQL (replace connection string if needed)
Write-Host "Optional: Configure PostgreSQL for development/production" -ForegroundColor Cyan
Write-Host "Edit appsettings.json or environment variables in MIC.Console project:" -ForegroundColor Cyan
Write-Host "Example connection string:" -ForegroundColor Cyan
Write-Host '{ "ConnectionStrings": { "DefaultConnection": "Host=localhost;Port=5432;Database=mic_db;Username=postgres;Password=YourPassword" } }'

# 8️⃣ Optional: Add ValueComparers for collection properties in EF Core
Write-Host "Optional: Add ValueComparer to prevent EF Core collection warnings" -ForegroundColor Cyan
Write-Host "Example in OnModelCreating:" -ForegroundColor Cyan
@"
using Microsoft.EntityFrameworkCore.ChangeTracking;

// For AssetMonitor.AssociatedMetrics
builder.Property(a => a.AssociatedMetrics)
       .HasConversion(
           v => string.Join(',', v),
           v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
       )
       .Metadata.SetValueComparer(
           new ValueComparer<List<string>>(
               (c1, c2) => c1.SequenceEqual(c2),
               c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
               c => c.ToList()
           )
       );
"@

# 9️⃣ Run the console application
Write-Host "Running MIC.Console..." -ForegroundColor Yellow
cd ..\MIC.Console
dotnet run

# 1️⃣0️⃣ Workflow Tip
Write-Host "========== MIC is ready for feature development ==========" -ForegroundColor Green
Write-Host "All packages restored, solution built, tests passed, migrations applied, and console running." -ForegroundColor Green
Write-Host "You can now focus on building new features." -ForegroundColor Green

# 1️⃣1️⃣ Feature Recommendations
Write-Host "========== Recommended Features ==========" -ForegroundColor Cyan
Write-Host "- PostgreSQL support for production-grade database operations"
Write-Host "- Real-time dashboard for analytics and operational metrics"
Write-Host "- AI-driven intelligence alerts with custom thresholds"
Write-Host "- Email integration with automated action items and keyword extraction"
Write-Host "- Asset monitoring UI with visualization of metrics"
Write-Host "- Multi-user roles with identity management"
Write-Host "- Notification system (desktop and email) for critical alerts"
Write-Host "- Logging and monitoring system integration (already partially implemented)"
Write-Host "- Extendable plugin architecture for AI modules and services"

Write-Host "=========================================================" -ForegroundColor Cyan

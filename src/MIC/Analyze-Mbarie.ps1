# ============================================================
# MBARIE PROJECT ANALYZER
# Run from: C:\MbarieIntelligenceConsole\src\mic
# ============================================================

param(
    [string]$OutputFile = "MBARIE-Project-Analysis-$(Get-Date -Format 'yyyyMMdd-HHmm').txt"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "SilentlyContinue"

$Root = $PSScriptRoot   # <- Anchor EVERYTHING here

Write-Host "Analyzing MBARIE project structure..." -ForegroundColor Cyan

# ------------------------------------------------------------
# Helpers
# ------------------------------------------------------------

function Section($title) {
    @("", $title, "-" * 80, "")
}

function FileSizeKB($bytes) {
    [Math]::Round($bytes / 1KB, 2)
}

# ------------------------------------------------------------
# Report Header
# ------------------------------------------------------------

$report = @()
$report += "=" * 80
$report += "MBARIE INTELLIGENCE CONSOLE - COMPLETE PROJECT ANALYSIS"
$report += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$report += "Root Path: $Root"
$report += "=" * 80
$report += ""

# ============================================================
# 1. SOLUTION OVERVIEW
# ============================================================

$report += Section "1. SOLUTION OVERVIEW"

$slnPath = Join-Path $Root "MIC.slnx"
$slnFile = Get-Item $slnPath -ErrorAction SilentlyContinue

if ($slnFile) {
    $report += "Solution File : $($slnFile.Name)"
    $report += "Location      : $($slnFile.Directory.FullName)"
    $report += "Size          : $(FileSizeKB $slnFile.Length) KB"
    $report += "Modified      : $($slnFile.LastWriteTime)"
    $report += ""
}

$projects = dotnet sln $slnPath list 2>$null
$report += "Total Projects: $($projects.Count)"
$report += "Projects:"
$projects | ForEach-Object { $report += "  - $_" }

# ============================================================
# 2. PROJECT STRUCTURE & FILE COUNTS
# ============================================================

$report += Section "2. PROJECT STRUCTURE & FILE COUNTS"

$projectDirs = Get-ChildItem -Path $Root -Directory |
    Where-Object { $_.Name -match '^MIC\.' }

foreach ($projDir in $projectDirs) {

    $report += "[PROJECT] $($projDir.Name)"

    $csFiles   = Get-ChildItem $projDir.FullName -Recurse -Filter "*.cs"
    $xamlFiles = Get-ChildItem $projDir.FullName -Recurse -Filter "*.xaml"
    $jsonFiles = Get-ChildItem $projDir.FullName -Recurse -Filter "*.json"

    $csSize   = ($csFiles   | Measure-Object Length -Sum).Sum
    $xamlSize = ($xamlFiles | Measure-Object Length -Sum).Sum

    $report += "  C# Files     : $($csFiles.Count) ($(FileSizeKB $csSize) KB)"
    $report += "  XAML Files   : $($xamlFiles.Count) ($(FileSizeKB $xamlSize) KB)"
    $report += "  Config Files : $($jsonFiles.Count)"
    $report += "  Total Size   : $(FileSizeKB ($csSize + $xamlSize)) KB"

    $csproj = Get-ChildItem $projDir.FullName -Filter "*.csproj" | Select-Object -First 1
    if ($csproj) {
        $report += "  Project File : $($csproj.Name) ($(FileSizeKB $csproj.Length) KB)"
    }

    $report += ""
}

# ============================================================
# 3. FEATURE IMPLEMENTATION STATUS
# ============================================================

$report += Section "3. FEATURE IMPLEMENTATION STATUS"

$features = @{
    "Dashboard"       = "MIC.Desktop.Avalonia\Views\DashboardView.axaml"
    "Email Inbox"     = "MIC.Desktop.Avalonia\Views\EmailInboxView.axaml"
    "Chat Interface"  = "MIC.Desktop.Avalonia\Views\ChatView.axaml"
    "Alerts"          = "MIC.Core.Application\Alerts"
    "Metrics"         = "MIC.Core.Application\Metrics"
    "Email Service"   = "MIC.Infrastructure.Data\Repositories\EmailRepository.cs"
    "AI Service"      = "MIC.Infrastructure.AI\Services\ChatService.cs"
    "Semantic Kernel" = "MIC.Infrastructure.AI\Services\SemanticKernelConfig.cs"
}

foreach ($feature in $features.GetEnumerator()) {
    $path   = Join-Path $Root $feature.Value
    $exists = Test-Path $path
    $status = if ($exists) { "IMPLEMENTED" } else { "PENDING" }
    $report += "[$status] $($feature.Key) -> $($feature.Value)"
}

# ============================================================
# 4. METRICS (FAST, SAFE)
# ============================================================

$report += Section "4. DEVELOPMENT METRICS"

$allCs   = Get-ChildItem $Root -Recurse -Filter "*.cs"
$allXaml = Get-ChildItem $Root -Recurse -Filter "*.xaml"

$report += "Total C# Files   : $($allCs.Count)"
$report += "Total XAML Files : $($allXaml.Count)"
$report += "Total LOC (C#)   : $((Get-Content $allCs.FullName | Measure-Object -Line).Lines)"

# ============================================================
# OUTPUT
# ============================================================

$OutputPath = Join-Path $Root $OutputFile
$report | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "Report generated successfully:" -ForegroundColor Green
Write-Host $OutputPath -ForegroundColor Cyan

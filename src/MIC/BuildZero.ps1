<#
.SYNOPSIS
  Builds MIC.sln with zero-tolerance for warnings or errors.
.EXAMPLE
  PS> .\BuildZero.ps1
  Returns 0 on success, writes summary to console.
#>
$ErrorActionPreference = "Stop"

Write-Host "`n???  MIC zero-warning build" -ForegroundColor Cyan

dotnet restore MIC.sln | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

dotnet build MIC.sln --no-incremental /warnaserror /nologo /clp:Summary
if ($LASTEXITCODE -ne 0) { throw "Build failed (see above)" }

Write-Host "`n? Build clean: 0 errors, 0 warnings" -ForegroundColor Green
exit 0

param(
    [string]$ApiBaseUrl = "http://localhost:5176",
    [string]$PreparedDir = "C:\Backup old\dev\bankinsight\migration_work\prepared",
    [string]$Email = "",
    [string]$Password = "",
    [string]$Token = "",
    [int]$TimeoutMinutes = 30,
    [switch]$SkipGlAccounts,
    [switch]$SkipChequeInventory
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$importScript = Join-Path $scriptRoot "import-bankinsight-migration.ps1"
$verifyScript = Join-Path $scriptRoot "verify-bankinsight-import.ps1"
$chequeScript = Join-Path $scriptRoot "import-cheque-inventory.ps1"

if (-not (Test-Path $importScript)) { throw "Missing script: $importScript" }
if (-not (Test-Path $verifyScript)) { throw "Missing script: $verifyScript" }
if (-not (Test-Path $chequeScript)) { throw "Missing script: $chequeScript" }

Write-Host "Starting full BankInsight migration run..." -ForegroundColor Cyan

$common = @{
    ApiBaseUrl = $ApiBaseUrl
    PreparedDir = $PreparedDir
    TimeoutMinutes = $TimeoutMinutes
}

if (-not [string]::IsNullOrWhiteSpace($Token)) {
    $common.Token = $Token
}
else {
    $common.Email = $Email
    $common.Password = $Password
}

Write-Host "Step 1: Importing primary datasets" -ForegroundColor Cyan
if ($SkipGlAccounts) {
    & $importScript @common -SkipGlAccounts
}
else {
    & $importScript @common
}

Write-Host "Step 2: Verifying imported counts" -ForegroundColor Cyan
& $verifyScript @common

if (-not $SkipChequeInventory) {
    Write-Host "Step 3: Seeding cheque inventory" -ForegroundColor Cyan
    & $chequeScript @common
}
else {
    Write-Host "Step 3: Skipped cheque inventory import" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Full migration sequence completed." -ForegroundColor Green
Write-Host "Check outputs in: $PreparedDir" -ForegroundColor Green

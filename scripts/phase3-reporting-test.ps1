#!/usr/bin/env pwsh
param(
    [string]$BaseUrl = "http://localhost:5176",
    [string]$AdminEmail = "admin@bankinsight.local",
    [string]$AdminPassword = "password123"
)

$ErrorActionPreference = 'Stop'
$results = @{ Passed = 0; Failed = 0; Tests = @() }

function Write-TestResult {
    param([string]$TestName, [bool]$Passed, [string]$Details)
    $status = if ($Passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "$status : $TestName" -ForegroundColor $color
    if ($Details) { Write-Host "        $Details" -ForegroundColor Gray }
    if ($Passed) { $results.Passed++ } else { $results.Failed++ }
    $results.Tests += @{ Name = $TestName; Passed = $Passed; Details = $Details }
}

Write-Host "BankInsight Phase 3 - Reporting Smoke Test" -ForegroundColor Cyan
Write-Host "API: $BaseUrl" -ForegroundColor Gray
Write-Host "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

try {
    $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
    $token = $login.token
    $headers = @{ Authorization = "Bearer $token" }
    Write-TestResult "Authenticate Admin" (![string]::IsNullOrWhiteSpace($token)) "JWT acquired"
}
catch {
    Write-TestResult "Authenticate Admin" $false $_.Exception.Message
    exit 1
}

try {
    $asOfDate = (Get-Date).ToString("yyyy-MM-dd")
    $month = (Get-Date).Month
    $year = (Get-Date).Year
    $startOfMonth = (Get-Date -Day 1).ToString("yyyy-MM-dd")
    $today = (Get-Date).ToString("yyyy-MM-dd")

    Write-Host "`n[1/3] REGULATORY REPORTS" -ForegroundColor Cyan

    $daily = Invoke-RestMethod -Uri "$BaseUrl/api/regulatory-reports/daily-position?reportDate=$asOfDate" -Method Get -Headers $headers
    Write-TestResult "Daily Position Report" ($null -ne $daily) "Generated"

    $mr1 = Invoke-RestMethod -Uri "$BaseUrl/api/regulatory-reports/monthly-return-1?month=$month&year=$year" -Method Get -Headers $headers
    Write-TestResult "Monthly Return 1" ($null -ne $mr1) "Generated"

    $prudential = Invoke-RestMethod -Uri "$BaseUrl/api/regulatory-reports/prudential?asOfDate=$asOfDate" -Method Get -Headers $headers
    Write-TestResult "Prudential Return" ($null -ne $prudential) "Generated"

    $history = Invoke-RestMethod -Uri "$BaseUrl/api/regulatory-reports/history" -Method Get -Headers $headers
    Write-TestResult "Regulatory History" ($null -ne $history) "Fetched"

    Write-Host "`n[2/3] FINANCIAL REPORTS" -ForegroundColor Cyan

    $balanceSheet = Invoke-RestMethod -Uri "$BaseUrl/api/financial-reports/balance-sheet?asOfDate=$asOfDate" -Method Get -Headers $headers
    Write-TestResult "Balance Sheet" ($null -ne $balanceSheet) "Generated"

    $income = Invoke-RestMethod -Uri "$BaseUrl/api/financial-reports/income-statement?periodStart=$startOfMonth&periodEnd=$today" -Method Get -Headers $headers
    Write-TestResult "Income Statement" ($null -ne $income) "Generated"

    $cashFlow = Invoke-RestMethod -Uri "$BaseUrl/api/financial-reports/cash-flow?periodStart=$startOfMonth&periodEnd=$today" -Method Get -Headers $headers
    Write-TestResult "Cash Flow" ($null -ne $cashFlow) "Generated"

    $trial = Invoke-RestMethod -Uri "$BaseUrl/api/financial-reports/trial-balance?asOfDate=$asOfDate" -Method Get -Headers $headers
    Write-TestResult "Trial Balance" ($null -ne $trial) "Generated"

    Write-Host "`n[3/3] REPORTING CONTROLLERS" -ForegroundColor Cyan

    $reportDaily = Invoke-RestMethod -Uri "$BaseUrl/api/report/regulatory/daily-position?reportDate=$asOfDate" -Method Get -Headers $headers
    Write-TestResult "Report Controller Daily Position" ($null -ne $reportDaily) "Generated"

    $reportBalance = Invoke-RestMethod -Uri "$BaseUrl/api/report/financial/balance-sheet?asOfDate=$asOfDate" -Method Get -Headers $headers
    Write-TestResult "Report Controller Balance Sheet" ($null -ne $reportBalance) "Generated"
}
catch {
    Write-TestResult "Phase 3 Reporting Operations" $false $_.Exception.Message
}

Write-Host "`n" + ("=" * 70) -ForegroundColor Cyan
Write-Host "PHASE 3 REPORTING TEST SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "Total Tests: $($results.Passed + $results.Failed)"
Write-Host "Passed: $($results.Passed)" -ForegroundColor Green
Write-Host "Failed: $($results.Failed)" -ForegroundColor Red
Write-Host ("=" * 70) -ForegroundColor Cyan

if ($results.Failed -eq 0) {
    Write-Host "`n[SUCCESS] ALL PHASE 3 REPORTING TESTS PASSED" -ForegroundColor Green
    exit 0
}

Write-Host "`n[FAILURE] SOME PHASE 3 REPORTING TESTS FAILED" -ForegroundColor Red
$results.Tests | Where-Object { -not $_.Passed } | ForEach-Object {
    Write-Host "  - $($_.Name): $($_.Details)" -ForegroundColor Red
}
exit 1

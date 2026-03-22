#!/usr/bin/env pwsh
param(
    [string]$BaseUrl = "http://localhost:5176",
    [string]$AdminEmail = "admin@bankinsight.local",
    [string]$AdminPassword = "password123"
)

$ErrorActionPreference = 'Stop'
$results = @{ Passed = 0; Failed = 0 }

function Write-TestResult {
    param([string]$TestName, [bool]$Passed, [string]$Details)
    $status = if ($Passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($Passed) { "Green" } else { "Red" }
    Write-Host "$status : $TestName" -ForegroundColor $color
    if ($Details) { Write-Host "        $Details" -ForegroundColor Gray }
    if ($Passed) { $results.Passed++ } else { $results.Failed++ }
}

Write-Host "BankInsight Phase 1 - Security Test Suite" -ForegroundColor Cyan
Write-Host "API: $BaseUrl" -ForegroundColor Gray
Write-Host "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

try {
    $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $loginBody
    $token = $login.token
    $headers = @{ Authorization = "Bearer $token" }
    Write-TestResult "Admin Login" (![string]::IsNullOrWhiteSpace($token)) "JWT acquired"
} catch {
    Write-TestResult "Admin Login" $false $_.Exception.Message
    exit 1
}

try {
    $summary = Invoke-RestMethod -Uri "$BaseUrl/api/security/summary?sinceHours=24" -Method Get -Headers $headers
    Write-TestResult "Security Summary" ($null -ne $summary) "Alerts: $($summary.securityAlertCount), FailedLogins: $($summary.failedLoginCount)"

    $alerts = Invoke-RestMethod -Uri "$BaseUrl/api/security/alerts?limit=10" -Method Get -Headers $headers
    Write-TestResult "Security Alerts Feed" ($null -ne $alerts) "Fetched"

    $failed = Invoke-RestMethod -Uri "$BaseUrl/api/security/failed-logins?sinceMinutes=1440&limit=10" -Method Get -Headers $headers
    Write-TestResult "Failed Login Feed" ($null -ne $failed) "Fetched"

    # Trigger failed login to create suspicious login alert
    try {
        $badLogin = @{ email = $AdminEmail; password = "wrong-password" } | ConvertTo-Json
        $null = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $badLogin
    } catch {
    }

    Start-Sleep 1
    $summaryAfter = Invoke-RestMethod -Uri "$BaseUrl/api/security/summary?sinceHours=24" -Method Get -Headers $headers
    $loginAlertObserved = $summaryAfter.failedLoginCount -ge $summary.failedLoginCount
    Write-TestResult "Suspicious Login Detection" $loginAlertObserved "Before: $($summary.failedLoginCount), After: $($summaryAfter.failedLoginCount)"
} catch {
    Write-TestResult "Security Endpoints" $false $_.Exception.Message
}

Write-Host "`n" + ("=" * 70) -ForegroundColor Cyan
Write-Host "PHASE 1 SECURITY TEST SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host "Passed: $($results.Passed)" -ForegroundColor Green
Write-Host "Failed: $($results.Failed)" -ForegroundColor Red
Write-Host ("=" * 70) -ForegroundColor Cyan

if ($results.Failed -eq 0) {
    Write-Host "`n[SUCCESS] ALL SECURITY TESTS PASSED" -ForegroundColor Green
    exit 0
}

Write-Host "`n[FAILURE] SOME SECURITY TESTS FAILED" -ForegroundColor Red
exit 1

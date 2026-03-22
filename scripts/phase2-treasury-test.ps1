#!/usr/bin/env pwsh
<#
.SYNOPSIS
    BankInsight Phase 2 Treasury Management Smoke Test
.DESCRIPTION
    Comprehensive test suite for Treasury Management features:
    - FX Rate Management
    - Treasury Position Tracking
    - FX Trading Desk
    - Investment Portfolio
    - Risk Analytics
.PARAMETER BaseUrl
    API base URL (default: http://localhost:5176)
.PARAMETER AdminEmail
    Admin user email (default: admin@bankinsight.local)
.PARAMETER AdminPassword
    Admin user password (default: password123)
#>

param(
    [string]$BaseUrl = "http://localhost:5176",
    [string]$AdminEmail = "admin@bankinsight.local",
    [string]$AdminPassword = "password123"
)

$ErrorActionPreference = 'Stop'
$colors = @{
    Success = "Green"
    Error = "Red"
    Info = "Cyan"
    Warn = "Yellow"
}

$results = @{ Passed = 0; Failed = 0; Tests = @() }

function Write-TestResult {
    param([string]$TestName, [bool]$Passed, [string]$Details)
    $status = if ($Passed) { "[PASS]" } else { "[FAIL]" }
    $color = if ($Passed) { $colors.Success } else { $colors.Error }
    Write-Host "$status : $TestName" -ForegroundColor $color
    if ($Details) { Write-Host "        $Details" -ForegroundColor Gray }
    if ($Passed) { $results.Passed++ } else { $results.Failed++ }
    $results.Tests += @{ Name = $TestName; Passed = $Passed; Details = $Details }
}

function Test-FxRateManagement {
    Write-Host "`n[1/5] FX RATE MANAGEMENT" -ForegroundColor $colors.Info
    
    try {
        $rate = @{
            baseCurrency = "GHS"
            targetCurrency = "USD"
            buyRate = 11.50
            sellRate = 11.60
            midRate = 11.55
            source = "BOG"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/fxrate" -Method Post `
            -Headers $headers -ContentType "application/json" -Body $rate
        
        Write-TestResult "Create FX Rate (GHS/USD)" ($response.id -gt 0) "Rate ID: $($response.id)"
        
        # List rates
        $rates = Invoke-RestMethod -Uri "$BaseUrl/api/fxrate" -Method Get -Headers $headers
        Write-TestResult "List FX Rates" ($rates -is [array] -or $null -ne $rates) "Found rates"
        
        # Get latest rate
        $latest = Invoke-RestMethod -Uri "$BaseUrl/api/fxrate/latest/GHS/USD" -Method Get -Headers $headers
        Write-TestResult "Get Latest Rate" ($latest.buyRate -gt 0) "Buy Rate: $($latest.buyRate), Sell: $($latest.sellRate)"
    } catch {
        Write-TestResult "FX Rate Operations" $false $_.Exception.Message
    }
}

function Test-TreasuryPositions {
    Write-Host "`n[2/5] TREASURY POSITION TRACKING" -ForegroundColor $colors.Info
    
    try {
        $position = @{
            positionDate = (Get-Date).ToString("yyyy-MM-dd")
            currency = "GHS"
            openingBalance = 1000000
            deposits = 500000
            withdrawals = 200000
            fxGainsLosses = 50000
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/treasuryposition" -Method Post `
            -Headers $headers -ContentType "application/json" -Body $position
        
        Write-TestResult "Create Treasury Position (GHS)" ($response.id -gt 0) "Position ID: $($response.id)"
        
        # List positions
        $positions = Invoke-RestMethod -Uri "$BaseUrl/api/treasuryposition" -Method Get -Headers $headers
        Write-TestResult "List Treasury Positions" ($positions -is [array] -or $null -ne $positions) "Positions retrieved"
        
        # Get latest position for currency
        $latest = Invoke-RestMethod -Uri "$BaseUrl/api/treasuryposition/latest/GHS" -Method Get -Headers $headers
        Write-TestResult "Get Latest Position (GHS)" ($null -ne $latest) "Latest position retrieved"
    } catch {
        Write-TestResult "Treasury Position Operations" $false $_.Exception.Message
    }
}

function Test-FxTrading {
    Write-Host "`n[3/5] FX TRADING DESK" -ForegroundColor $colors.Info
    
    try {
        $trade = @{
            tradeDate = (Get-Date).ToString("yyyy-MM-dd")
            valueDate = (Get-Date).AddDays(2).ToString("yyyy-MM-dd")
            tradeType = "Spot"
            direction = "Buy"
            baseCurrency = "USD"
            baseAmount = 100000
            counterCurrency = "GHS"
            counterAmount = 1150000
            exchangeRate = 11.50
            customerRate = 11.50
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/fxtrading" -Method Post `
            -Headers $headers -ContentType "application/json" -Body $trade
        
        Write-TestResult "Create FX Trade (USD/GHS)" ($null -ne $response.dealNumber) `
            "Deal: $($response.dealNumber), Status: $($response.status)"
        
        # Get trade stats
        $stats = Invoke-RestMethod -Uri "$BaseUrl/api/fxtrading/stats" -Method Get -Headers $headers
        Write-TestResult "Get Trade Statistics" ($null -ne $stats) "Stats retrieved"
        
        # List pending trades
        $pending = Invoke-RestMethod -Uri "$BaseUrl/api/fxtrading/pending" -Method Get -Headers $headers
        Write-TestResult "Get Pending Trades" ($null -ne $pending) "Pending trades retrieved"
    } catch {
        Write-TestResult "FX Trading Operations" $false $_.Exception.Message
    }
}

function Test-InvestmentPortfolio {
    Write-Host "`n[4/5] INVESTMENT PORTFOLIO" -ForegroundColor $colors.Info
    
    try {
        $investment = @{
            investmentType = "T-Bill"
            instrument = "91-Day T-Bill"
            counterparty = "Bank of Ghana"
            currency = "GHS"
            principalAmount = 500000
            interestRate = 18.5
            placementDate = (Get-Date).ToString("yyyy-MM-dd")
            maturityDate = (Get-Date).AddDays(91).ToString("yyyy-MM-dd")
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/investment" -Method Post `
            -Headers $headers -ContentType "application/json" -Body $investment
        
        Write-TestResult "Create Investment (T-Bill)" ($null -ne $response.investmentNumber) `
            "Investment: $($response.investmentNumber)"
        
        # Get portfolio summary
        $portfolio = Invoke-RestMethod -Uri "$BaseUrl/api/investment/portfolio" -Method Get -Headers $headers
        Write-TestResult "Get Portfolio Summary" ($null -ne $portfolio) `
            "Total Principal: $($portfolio.totalPrincipal), Accrued: $($portfolio.totalAccruedInterest)"
        
        # Get maturing investments
        $maturing = Invoke-RestMethod -Uri "$BaseUrl/api/investment/maturing" -Method Get -Headers $headers
        Write-TestResult "Get Maturing Investments" ($null -ne $maturing) "Maturing investments retrieved"
    } catch {
        Write-TestResult "Investment Portfolio Operations" $false $_.Exception.Message
    }
}

function Test-RiskAnalytics {
    Write-Host "`n[5/5] RISK ANALYTICS & MONITORING" -ForegroundColor $colors.Info
    
    try {
        # Create metric manually to test create endpoint
        $createMetric = @{
            metricDate = (Get-Date).ToString("yyyy-MM-dd")
            metricType = "VaR_Test"
            currency = "GHS"
            metricValue = 50000
            threshold = 100000
            confidenceLevel = 95
            timeHorizonDays = 1
            calculationMethod = "Historical"
        } | ConvertTo-Json
        
        $metric = Invoke-RestMethod -Uri "$BaseUrl/api/riskanalytics" -Method Post `
            -Headers $headers -ContentType "application/json" -Body $createMetric
        
        Write-TestResult "Create Risk Metric" ($null -ne $metric.id) "Metric ID: $($metric.id)"
        
        # Get risk dashboard
        $dashboard = Invoke-RestMethod -Uri "$BaseUrl/api/riskanalytics/dashboard" -Method Get -Headers $headers
        Write-TestResult "Get Risk Dashboard" ($null -ne $dashboard) "Dashboard retrieved"
        
        # List metrics
        $metrics = Invoke-RestMethod -Uri "$BaseUrl/api/riskanalytics" -Method Get -Headers $headers
        Write-TestResult "List Risk Metrics" ($metrics -is [array] -or $null -ne $metrics) "Metrics retrieved"
        
        # Get alerts
        $alerts = Invoke-RestMethod -Uri "$BaseUrl/api/riskanalytics/alerts" -Method Get -Headers $headers
        Write-TestResult "Get Risk Alerts" ($null -ne $alerts) "Alerts retrieved"
    } catch {
        Write-TestResult "Risk Analytics Operations" $false $_.Exception.Message
    }
}

function Show-Summary {
    Write-Host "`n" + ("=" * 70) -ForegroundColor $colors.Info
    Write-Host "PHASE 2 TREASURY MANAGEMENT TEST SUMMARY" -ForegroundColor $colors.Info
    Write-Host ("=" * 70) -ForegroundColor $colors.Info
    
    $total = $results.Passed + $results.Failed
    Write-Host "Total Tests: $total" -ForegroundColor $colors.Info
    Write-Host "Passed: $($results.Passed)" -ForegroundColor $colors.Success
    Write-Host "Failed: $($results.Failed)" -ForegroundColor $(if ($results.Failed -gt 0) { $colors.Error } else { $colors.Success })
    Write-Host ("=" * 70) -ForegroundColor $colors.Info
    
    if ($results.Failed -eq 0) {
        Write-Host "`n[SUCCESS] ALL TREASURY MANAGEMENT TESTS PASSED" -ForegroundColor $colors.Success
        return 0
    } else {
        Write-Host "`n[FAILURE] SOME TESTS FAILED" -ForegroundColor $colors.Error
        $results.Tests | Where-Object { -not $_.Passed } | ForEach-Object {
            Write-Host "  - $($_.Name): $($_.Details)" -ForegroundColor $colors.Error
        }
        return 1
    }
}

# Main Execution
Write-Host "BankInsight Phase 2 - Treasury Management Test Suite" -ForegroundColor Cyan
Write-Host "API: $BaseUrl" -ForegroundColor Gray
Write-Host "Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

try {
    $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post `
        -ContentType "application/json" -Body $loginBody
    $token = $login.token
    $global:headers = @{ Authorization = "Bearer $token" }
    
    Test-FxRateManagement
    Test-TreasuryPositions
    Test-FxTrading
    Test-InvestmentPortfolio
    Test-RiskAnalytics
    
    $exitCode = Show-Summary
    exit $exitCode
} catch {
    Write-Host "`n[FATAL] $($_.Exception.Message)" -ForegroundColor $colors.Error
    exit 2
}

# Test script for new loan operations endpoints
$BaseUrl = "http://localhost:5176"

Write-Host "=== Testing New Loan Operations Endpoints ===" -ForegroundColor Cyan

# 1. Login
Write-Host "`n1. Logging in..." -ForegroundColor Yellow
$loginBody = @{
    username = "admin"
    password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $_" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# 2. Test credit bureau providers list
Write-Host "`n2. Fetching credit bureau providers..." -ForegroundColor Yellow
try {
    $providers = Invoke-RestMethod -Uri "$BaseUrl/api/loans/credit-bureau/providers" -Method GET -Headers $headers
    Write-Host "✓ Retrieved $($providers.Count) provider(s)" -ForegroundColor Green
    $providers | ForEach-Object { Write-Host "  - $($_.name) [$($_.providerType)]" -ForegroundColor Gray }
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# 3. Test delinquency dashboard
Write-Host "`n3. Fetching delinquency dashboard..." -ForegroundColor Yellow
try {
    $delinquency = Invoke-RestMethod -Uri "$BaseUrl/api/loans/delinquency-dashboard" -Method GET -Headers $headers
    Write-Host "✓ Retrieved delinquency data" -ForegroundColor Green
    Write-Host "  - Total Loans: $($delinquency.totalLoans)" -ForegroundColor Gray
    Write-Host "  - Delinquent Loans: $($delinquency.delinquentLoans)" -ForegroundColor Gray
    Write-Host "  - PAR 30: $('{0:P2}' -f ($delinquency.par30Rate))" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# 4. Test profitability report
Write-Host "`n4. Fetching profitability report..." -ForegroundColor Yellow
try {
    $profitability = Invoke-RestMethod -Uri "$BaseUrl/api/loans/reports/profitability" -Method GET -Headers $headers
    Write-Host "✓ Retrieved profitability report" -ForegroundColor Green
    Write-Host "  - Total Interest Income: $($profitability.totalInterestIncome)" -ForegroundColor Gray
    Write-Host "  - Total Fees: $($profitability.totalFees)" -ForegroundColor Gray
    Write-Host "  - Total Impairment: $($profitability.totalImpairmentExpense)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# 5. Test balance sheet report
Write-Host "`n5. Fetching balance sheet report..." -ForegroundColor Yellow
try {
    $balanceSheet = Invoke-RestMethod -Uri "$BaseUrl/api/loans/reports/balance-sheet" -Method GET -Headers $headers
    Write-Host "✓ Retrieved balance sheet report" -ForegroundColor Green
    Write-Host "  - Gross Loan Portfolio: $($balanceSheet.grossLoanPortfolio)" -ForegroundColor Gray
    Write-Host "  - Loan Loss Allowance: $($balanceSheet.loanLossAllowance)" -ForegroundColor Gray
    Write-Host "  - Net Loan Portfolio: $($balanceSheet.netLoanPortfolio)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

# 6. Test accounting profile config (get all)
Write-Host "`n6. Fetching loan accounting profiles..." -ForegroundColor Yellow
try {
    $profiles = Invoke-RestMethod -Uri "$BaseUrl/api/loans/config/accounting-profiles" -Method GET -Headers $headers
    Write-Host "✓ Retrieved $($profiles.Count) accounting profile(s)" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan

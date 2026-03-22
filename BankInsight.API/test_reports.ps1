# BankInsight Phase 3 Reporting API Test Script

$baseUrl = "http://localhost:5000/api"
$username = "admin"
$password = "Admin@123"

Write-Host "=== BankInsight Phase 3 Reporting API Tests ===" -ForegroundColor Green
Write-Host ""

# 1. Login to get JWT token
Write-Host "1. Logging in..." -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post `
        -Body (@{username=$username; password=$password} | ConvertTo-Json) `
        -ContentType "application/json"
    
    $token = $loginResponse.token
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 2. Get Report Catalog
Write-Host "2. Fetching Report Catalog..." -ForegroundColor Yellow
try {
    $catalog = Invoke-RestMethod -Uri "$baseUrl/report/catalog" -Method Get -Headers $headers
    Write-Host "✓ Report Catalog retrieved: $($catalog.Count) reports" -ForegroundColor Green
    $catalog | ForEach-Object {
        Write-Host "   - $($_.reportCode): $($_.reportName) [$($_.reportType)]"
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to retrieve report catalog: $($_.Exception.Message)" -ForegroundColor Red
}

# 3. Get Daily Position Report
Write-Host "3. Generating Daily Position Report..." -ForegroundColor Yellow
try {
    $reportDate = (Get-Date).ToString("yyyy-MM-dd")
    $dailyPosition = Invoke-RestMethod -Uri "$baseUrl/report/regulatory/daily-position?reportDate=$reportDate" `
        -Method Get -Headers $headers
    Write-Host "✓ Daily Position Report generated" -ForegroundColor Green
    Write-Host "   Total Positions: $($dailyPosition.positions.Count)"
    Write-Host "   Total GHS: GHS $($dailyPosition.totalPositionGHS)"
    Write-Host "   Total USD: USD $($dailyPosition.totalPositionUSD)"
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate daily position report: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Get Balance Sheet
Write-Host "4. Generating Balance Sheet..." -ForegroundColor Yellow
try {
    $asOfDate = (Get-Date).ToString("yyyy-MM-dd")
    $balanceSheet = Invoke-RestMethod -Uri "$baseUrl/report/financial/balance-sheet?asOfDate=$asOfDate" `
        -Method Get -Headers $headers
    Write-Host "✓ Balance Sheet generated" -ForegroundColor Green
    Write-Host "   Total Assets: GHS $($balanceSheet.totalAssets)"
    Write-Host "   Total Liabilities: GHS $($balanceSheet.totalLiabilities)"
    Write-Host "   Total Equity: GHS $($balanceSheet.totalEquity)"
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate balance sheet: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. Get Income Statement
Write-Host "5. Generating Income Statement..." -ForegroundColor Yellow
try {
    $periodStart = (Get-Date).AddMonths(-1).ToString("yyyy-MM-dd")
    $periodEnd = (Get-Date).ToString("yyyy-MM-dd")
    $incomeStatement = Invoke-RestMethod -Uri "$baseUrl/report/financial/income-statement?periodStart=$periodStart&periodEnd=$periodEnd" `
        -Method Get -Headers $headers
    Write-Host "✓ Income Statement generated" -ForegroundColor Green
    Write-Host "   Total Revenue: GHS $($incomeStatement.totalRevenue)"
    Write-Host "   Total Expenses: GHS $($incomeStatement.totalExpenses)"
    Write-Host "   Net Profit: GHS $($incomeStatement.netProfit)"
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate income statement: $($_.Exception.Message)" -ForegroundColor Red
}

# 6. Get Customer Segmentation Analytics
Write-Host "6. Generating Customer Segmentation Analytics..." -ForegroundColor Yellow
try {
    $asOfDate = (Get-Date).ToString("yyyy-MM-dd")
    $segmentation = Invoke-RestMethod -Uri "$baseUrl/report/analytics/customer-segmentation?asOfDate=$asOfDate" `
        -Method Get -Headers $headers
    Write-Host "✓ Customer Segmentation Analytics generated" -ForegroundColor Green
    Write-Host "   Segments: $($segmentation.segments.Count)"
    $segmentation.segments | ForEach-Object {
        Write-Host "   - $($_.segmentName): $($_.customerCount) customers, GHS $($_.totalBalance)"
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate customer segmentation: $($_.Exception.Message)" -ForegroundColor Red
}

# 7. Get Transaction Trends
Write-Host "7. Generating Transaction Trends..." -ForegroundColor Yellow
try {
    $periodStart = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")
    $periodEnd = (Get-Date).ToString("yyyy-MM-dd")
    $trends = Invoke-RestMethod -Uri "$baseUrl/report/analytics/transaction-trends?periodStart=$periodStart&periodEnd=$periodEnd" `
        -Method Get -Headers $headers
    Write-Host "✓ Transaction Trends generated" -ForegroundColor Green
    Write-Host "   Total Transactions: $($trends.totalTransactions)"
    Write-Host "   Total Volume: GHS $($trends.totalVolume)"
    Write-Host "   Average Daily Volume: GHS $($trends.averageDailyVolume)"
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate transaction trends: $($_.Exception.Message)" -ForegroundColor Red
}

# 8. Get Product Analytics
Write-Host "8. Generating Product Analytics..." -ForegroundColor Yellow
try {
    $asOfDate = (Get-Date).ToString("yyyy-MM-dd")
    $productAnalytics = Invoke-RestMethod -Uri "$baseUrl/report/analytics/product-analytics?asOfDate=$asOfDate" `
        -Method Get -Headers $headers
    Write-Host "✓ Product Analytics generated" -ForegroundColor Green
    Write-Host "   Total Products: $($productAnalytics.totalProducts)"
    Write-Host "   Total Accounts: $($productAnalytics.totalAccounts)"
    Write-Host "   Total Balance: GHS $($productAnalytics.totalBalance)"
    Write-Host ""
} catch {
    Write-Host "✗ Failed to generate product analytics: $($_.Exception.Message)" -ForegroundColor Red
}

# 9. Get Regulatory Returns List
Write-Host "9. Fetching Regulatory Returns History..." -ForegroundColor Yellow
try {
    $returns = Invoke-RestMethod -Uri "$baseUrl/report/regulatory/returns" -Method Get -Headers $headers
    Write-Host "✓ Regulatory Returns retrieved: $($returns.Count) returns" -ForegroundColor Green
    if ($returns.Count -gt 0) {
        $returns | Select-Object -First 5 | ForEach-Object {
            Write-Host "   - ID $($_.id): $($_.returnType) - $($_.submissionStatus) [$($_.returnDate)]"
        }
    }
    Write-Host ""
} catch {
    Write-Host "✗ Failed to retrieve regulatory returns: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "=== Phase 3 Reporting API Tests Complete ===" -ForegroundColor Green

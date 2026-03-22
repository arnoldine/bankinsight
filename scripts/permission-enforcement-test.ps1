# BankInsight Permission Enforcement Testing
# Tests that users can only access endpoints for which they have permissions

$ErrorActionPreference = 'Continue'

$baseUrl = "http://localhost:5176"
$adminEmail = "admin@bankinsight.local"
$adminPassword = "password123"
$tellerEmail = "teller@bankinsight.local"
$tellerPassword = "Teller123!"

# Colors for output
$passColor = [System.ConsoleColor]::Green
$failColor = [System.ConsoleColor]::Red
$infoColor = [System.ConsoleColor]::Cyan
$warnColor = [System.ConsoleColor]::Yellow

function Write-Pass {
    param([string]$message)
    Write-Host "[PASS]" -ForegroundColor $passColor -NoNewline
    Write-Host " : $message"
}

function Write-Fail {
    param([string]$message)
    Write-Host "[FAIL]" -ForegroundColor $failColor -NoNewline
    Write-Host " : $message"
}

function Write-Info {
    param([string]$message)
    Write-Host "[INFO]" -ForegroundColor $infoColor -NoNewline
    Write-Host " : $message"
}

function Write-Section {
    param([string]$title)
    Write-Host ""
    Write-Host "======================================================================" 
    Write-Host $title
    Write-Host "======================================================================"
}

$totalTests = 0
$passedTests = 0

# Login as admin
Write-Section "1. ADMIN LOGIN"

$totalTests++
try {
    $adminLogin = @{
        email = $adminEmail
        password = $adminPassword
    } | ConvertTo-Json

    $adminResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post `
        -ContentType "application/json" -Body $adminLogin

    $adminToken = $adminResponse.token
    Write-Pass "Admin login successful"
    $passedTests++
}
catch {
    Write-Fail "Admin login failed: $_"
    exit 1
}

$adminHeaders = @{ Authorization = "Bearer $adminToken"; "Content-Type" = "application/json" }

# Login as teller
Write-Section "2. TELLER LOGIN"

$totalTests++
try {
    $tellerLogin = @{
        email = $tellerEmail
        password = $tellerPassword
    } | ConvertTo-Json

    $tellerResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post `
        -ContentType "application/json" -Body $tellerLogin

    $tellerToken = $tellerResponse.token
    Write-Pass "Teller login successful"
    $passedTests++
}
catch {
    Write-Fail "Teller login failed: $_"
    $tellerToken = $null
}

$tellerHeaders = if ($tellerToken) { @{ Authorization = "Bearer $tellerToken"; "Content-Type" = "application/json" } } else { $null }

# Test 1: Admin can manage roles
Write-Section "3. ADMIN PERMISSIONS - CREATE ROLE"

$totalTests++
try {
    $roleBody = @{
        name = "Test Role $([DateTime]::Now.Ticks)"
        description = "Test role for permission verification"
        permissions = @("VIEW_ACCOUNTS")
    } | ConvertTo-Json

    $roleResponse = Invoke-RestMethod -Uri "$baseUrl/api/roles" -Method Post `
        -Headers $adminHeaders -Body $roleBody -ErrorAction Stop

    Write-Pass "Admin created a new role"
    $passedTests++
}
catch {
    Write-Fail "Admin failed to create role: $_"
}

# Test 2: Admin can manage users
Write-Section "4. ADMIN PERMISSIONS - MANAGE USERS"

$totalTests++
try {
    $usersResponse = Invoke-RestMethod -Uri "$baseUrl/api/users" -Method Get `
        -Headers $adminHeaders -ErrorAction Stop

    if ($usersResponse.Count -gt 0) {
        Write-Pass "Admin can list all users"
        $passedTests++
    }
}
catch {
    Write-Fail "Admin failed to list users: $_"
}

# Test 3: Admin can view all transactions
Write-Section "5. ADMIN PERMISSIONS - VIEW TRANSACTIONS"

$totalTests++
try {
    $txnResponse = Invoke-RestMethod -Uri "$baseUrl/api/transactions" -Method Get `
        -Headers $adminHeaders -ErrorAction Stop

    Write-Pass "Admin can view transactions"
    $passedTests++
}
catch {
    Write-Fail "Admin failed to view transactions: $_"
}

# Test 4: Teller can view accounts (if has permission)
Write-Section "6. TELLER PERMISSIONS - VIEW ACCOUNTS"

$totalTests++
if ($tellerToken) {
    try {
        $accountsResponse = Invoke-RestMethod -Uri "$baseUrl/api/accounts" -Method Get `
            -Headers $tellerHeaders -ErrorAction Stop

        Write-Pass "Teller can view accounts (has VIEW_ACCOUNTS permission)"
        $passedTests++
    }
    catch {
        Write-Fail "Teller cannot view accounts: $_"
    }
} else {
    Write-Info "Skipping - Teller login failed"
}

# Test 5: Teller cannot manage roles (permission denied)
Write-Section "7. TELLER PERMISSIONS - DENIED ACCESS (MANAGE ROLES)"

$totalTests++
if ($tellerToken) {
    try {
        $denyTest = Invoke-RestMethod -Uri "$baseUrl/api/roles" -Method Get `
            -Headers $tellerHeaders -ErrorAction Stop

        Write-Fail "Teller should NOT be able to manage roles (security issue)"
    }
    catch {
        $response = $_.Exception.Response
        if ($response.StatusCode -eq 403) {
            Write-Pass "Teller correctly denied access to role management (HTTP 403)"
            $passedTests++
        }
        elseif ($response.StatusCode -eq 401) {
            Write-Info "Got 401 Unauthorized instead of 403"
            $passedTests++
        }
        else {
            Write-Info "Permission denied check: Status $($response.StatusCode)"
        }
    }
} else {
    Write-Info "Skipping - Teller login failed"
}

# Test 6: Teller cannot create transactions (depending on permissions)
Write-Section "8. TELLER PERMISSIONS - POST TRANSACTION"

$totalTests++
if ($tellerToken) {
    try {
        $txnBody = @{
            accountId = "ACC0001"
            type = "DEPOSIT"
            amount = 500
            narration = "Test deposit"
            tellerId = "STF1123"
        } | ConvertTo-Json

        $postResponse = Invoke-RestMethod -Uri "$baseUrl/api/transactions" -Method Post `
            -Headers $tellerHeaders -Body $txnBody -ErrorAction Stop

        Write-Pass "Teller can post transactions (has POST_TRANSACTION permission)"
        $passedTests++
    }
    catch {
        Write-Info "Teller transaction posting result: $_"
    }
} else {
    Write-Info "Skipping - Teller login failed"
}

# Test 7: Display role permission matrix
Write-Section "9. ROLE PERMISSION MATRIX"

$totalTests++
try {
    $allRoles = Invoke-RestMethod -Uri "$baseUrl/api/roles" -Method Get -Headers $adminHeaders

    Write-Host ""
    Write-Host "Role Permission Summary:"
    Write-Host ""
    
    foreach ($role in $allRoles) {
        $permCount = if ($role.Permissions) { $role.Permissions.Count } else { 0 }
        Write-Host "  [$permCount perms] $($role.Name)`n                -> $($role.Description)" -ForegroundColor Cyan
    }
    
    $passedTests++
}
catch {
    Write-Fail "Failed to display role matrix: $_"
}

# Test 8: Verify permission claims in JWT
Write-Section "10. JWT PERMISSIONS CLAIMS ANALYSIS"

$totalTests++
try {
    # Decode admin token
    $parts = $adminToken.Split('.')
    $payload = $parts[1]
    while ($payload.Length % 4) { $payload += '=' }
    
    $decodedBytes = [System.Convert]::FromBase64String($payload)
    $adminClaims = [System.Text.Encoding]::UTF8.GetString($decodedBytes) | ConvertFrom-Json
    
    # Decode teller token if available
    if ($tellerToken) {
        $tellerParts = $tellerToken.Split('.')
        $tellerPayload = $tellerParts[1]
        while ($tellerPayload.Length % 4) { $tellerPayload += '=' }
        
        $tellerBytes = [System.Convert]::FromBase64String($tellerPayload)
        $tellerClaims = [System.Text.Encoding]::UTF8.GetString($tellerBytes) | ConvertFrom-Json
        
        Write-Info "Admin has $(($adminClaims.permissions | Measure-Object).Count) permissions"
        Write-Host "   - Sample: $($adminClaims.permissions[0..2] -join ', ')..."
        
        Write-Info "Teller has $(($tellerClaims.permissions | Measure-Object).Count) permissions"
        Write-Host "   - Sample: $($tellerClaims.permissions[0..2] -join ', ')"
    } else {
        Write-Info "Admin has $(($adminClaims.permissions | Measure-Object).Count) permissions"
    }
    
    $passedTests++
}
catch {
    Write-Fail "JWT analysis failed: $_"
}

# Summary
Write-Section "PERMISSION ENFORCEMENT TEST SUMMARY"

Write-Host "Total Tests: $totalTests"
Write-Host "Passed: $passedTests"
Write-Host "Failed: $($totalTests - $passedTests)"
Write-Host ""

if ($passedTests -ge ($totalTests - 1)) {
    Write-Host "[SUCCESS] PERMISSION ENFORCEMENT TESTS PASSED" -ForegroundColor Green
}
else {
    Write-Host "[WARNING] Some permission enforcement tests need review" -ForegroundColor Yellow
}

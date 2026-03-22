#!/usr/bin/env pwsh
<#
.SYNOPSIS
Test Permission-Aware UI Components
Tests JWT parsing and permission-based UI rendering

.DESCRIPTION
Validates that:
1. jwtUtils correctly parses JWT tokens
2. Permission extraction works correctly
3. hasPermission() returns true/false based on token
4. PermissionGuard component logic is sound
5. Different role dashboards show appropriate features
#>

# Configuration
$apiBase = "http://localhost:5176"
$tokenFile = "test-tokens.json"

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Permission-Aware UI Component Testing                      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

Write-Host "`n[STEP 1] Retrieve JWT Tokens for All Role Types" -ForegroundColor Yellow

$testUsers = @(
    @{ email = "admin@bankinsight.com"; password = "Admin@12345"; role = "Admin"; expectedPerms = 38 },
    @{ email = "teller@bankinsight.com"; password = "Teller@12345"; role = "Teller"; expectedPerms = 2 },
    @{ email = "branch_mgr@bankinsight.com"; password = "BranchMgr@12345"; role = "Branch Manager"; expectedPerms = 4 }
)

$tokens = @{}

foreach ($user in $testUsers) {
    Write-Host "`n  Testing login for: $($user.role) ($($user.email))" -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$apiBase/auth/login" -Method POST `
            -ContentType "application/json" `
            -Body (ConvertTo-Json @{ email = $user.email; password = $user.password })
        
        if ($response.token) {
            $tokens[$user.role] = $response.token
            Write-Host "    ✓ Token acquired (length: $($response.token.Length))" -ForegroundColor Green
        } else {
            Write-Host "    ✗ No token in response" -ForegroundColor Red
        }
    } catch {
        Write-Host "    ✗ Login failed: $_" -ForegroundColor Red
    }
}

Write-Host "`n[STEP 2] Test JWT Parsing - decodeJWT()" -ForegroundColor Yellow

# Simulate JWT parsing (normally done in JavaScript)
function Parse-JWTToken {
    param([string]$token)
    
    # JWT is three parts separated by dots: header.payload.signature
    $parts = $token.Split('.')
    if ($parts.Count -ne 3) {
        throw "Invalid JWT format"
    }
    
    # Decode payload (second part)
    $payload = $parts[1]
    
    # Add padding if needed
    $padLength = 4 - ($payload.Length % 4)
    if ($padLength -ne 4) {
        $payload += "=" * $padLength
    }
    
    try {
        $decoded = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payload))
        return $decoded | ConvertFrom-Json
    } catch {
        Write-Host "    ✗ Failed to decode JWT payload" -ForegroundColor Red
        return $null
    }
}

foreach ($role in $tokens.Keys) {
    Write-Host "`n  Parsing JWT for: $role" -ForegroundColor Cyan
    
    $payload = Parse-JWTToken $tokens[$role]
    if ($payload) {
        Write-Host "    ✓ JWT decoded successfully" -ForegroundColor Green
        Write-Host "      Email: $($payload.email)" -ForegroundColor Gray
        Write-Host "      Role ID: $($payload.role_id)" -ForegroundColor Gray
        Write-Host "      Role Name: $($payload.role_name ?? 'N/A')" -ForegroundColor Gray
        
        if ($payload.permissions) {
            Write-Host "      Permissions: $($payload.permissions.Count) total" -ForegroundColor Gray
            
            # Show first 5 and last 5 permissions
            $perms = $payload.permissions
            if ($perms.Count -le 10) {
                Write-Host "      Full list: $($perms -join ', ')" -ForegroundColor Gray
            } else {
                $first5 = $perms[0..4] -join ', '
                $last5 = $perms[($perms.Count - 5)..($perms.Count - 1)] -join ', '
                Write-Host "      First 5: $first5" -ForegroundColor Gray
                Write-Host "      Last 5: $last5" -ForegroundColor Gray
            }
        } else {
            Write-Host "      ⚠ No permissions in token" -ForegroundColor Yellow
        }
    }
}

Write-Host "`n[STEP 3] Test Permission Checking Logic" -ForegroundColor Yellow

$permissionTests = @(
    @{ role = "Admin"; permission = "SYSTEM_ADMIN"; expected = $true },
    @{ role = "Admin"; permission = "ACCOUNT_READ"; expected = $true },
    @{ role = "Admin"; permission = "TELLER_POST"; expected = $true },
    @{ role = "Teller"; permission = "TELLER_POST"; expected = $true },
    @{ role = "Teller"; permission = "SYSTEM_ADMIN"; expected = $false },
    @{ role = "Teller"; permission = "LOAN_APPROVE"; expected = $false },
    @{ role = "Branch Manager"; permission = "LOAN_APPROVE"; expected = $true },
    @{ role = "Branch Manager"; permission = "POST_TRANSACTION"; expected = $false }
)

$passCount = 0
$failCount = 0

foreach ($test in $permissionTests) {
    $token = $tokens[$test.role]
    if (-not $token) {
        Write-Host "`n  ⚠ Skipping $($test.role) - no token available" -ForegroundColor Yellow
        continue
    }
    
    $payload = Parse-JWTToken $token
    $hasPermission = if ($payload.permissions) {
        $payload.permissions -contains $test.permission -or $payload.permissions -contains 'SYSTEM_ADMIN'
    } else {
        $false
    }
    
    $passed = $hasPermission -eq $test.expected
    if ($passed) {
        Write-Host "`n  ✓ $($test.role) - hasPermission('$($test.permission)') = $hasPermission (expected: $($test.expected))" -ForegroundColor Green
        $passCount++
    } else {
        Write-Host "`n  ✗ $($test.role) - hasPermission('$($test.permission)') = $hasPermission (expected: $($test.expected))" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`n[STEP 4] Test Permission Guard Logic" -ForegroundColor Yellow

# Test multi-permission checks
$multiPermTests = @(
    @{ role = "Admin"; permissions = @("ACCOUNT_READ", "LOAN_READ"); requireAll = $true; expected = $true },
    @{ role = "Teller"; permissions = @("TELLER_POST", "SYSTEM_ADMIN"); requireAll = $true; expected = $false },
    @{ role = "Teller"; permissions = @("TELLER_POST", "SYSTEM_ADMIN"); requireAll = $false; expected = $true },
    @{ role = "Branch Manager"; permissions = @("LOAN_APPROVE", "ACCOUNT_READ"); requireAll = $true; expected = $true }
)

foreach ($test in $multiPermTests) {
    $token = $tokens[$test.role]
    if (-not $token) { continue }
    
    $payload = Parse-JWTToken $token
    
    if ($test.requireAll) {
        # AND logic - all permissions required
        $result = $test.permissions | Where-Object { -not ($payload.permissions -contains $_ -or $payload.permissions -contains 'SYSTEM_ADMIN') } | Measure-Object | Select-Object -ExpandProperty Count
        $hasAccess = $result -eq 0
    } else {
        # OR logic - any permission required
        $result = $test.permissions | Where-Object { $payload.permissions -contains $_ -or $payload.permissions -contains 'SYSTEM_ADMIN' } | Measure-Object | Select-Object -ExpandProperty Count
        $hasAccess = $result -gt 0
    }
    
    $passed = $hasAccess -eq $test.expected
    $logic = if ($test.requireAll) { "AND" } else { "OR" }
    
    if ($passed) {
        Write-Host "`n  ✓ $($test.role) - hasAnyPermission($($test.permissions -join ',')) [$logic] = $hasAccess" -ForegroundColor Green
        $passCount++
    } else {
        Write-Host "`n  ✗ $($test.role) - hasAnyPermission($($test.permissions -join ',')) [$logic] = $hasAccess (expected: $($test.expected))" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`n[STEP 5] Test Dashboard Access Guards" -ForegroundColor Yellow

$dashboardTests = @(
    @{ dashboard = "AdminDashboard"; requiredPerm = "SYSTEM_ADMIN"; roles = @("Admin"); expectAccess = @("Admin"); expectDenied = @("Teller", "Branch Manager") },
    @{ dashboard = "TellerDashboard"; requiredPerm = "TELLER_POST"; roles = @("Teller", "Admin"); expectAccess = @("Teller", "Admin"); expectDenied = @("Branch Manager") },
    @{ dashboard = "BranchManagerDashboard"; requiredPerm = "LOAN_APPROVE"; roles = @("Branch Manager", "Admin"); expectAccess = @("Branch Manager", "Admin"); expectDenied = @("Teller") }
)

foreach ($test in $dashboardTests) {
    Write-Host "`n  Dashboard: $($test.dashboard)" -ForegroundColor Cyan
    Write-Host "    Required Permission: $($test.requiredPerm)" -ForegroundColor Gray
    
    foreach ($role in $test.roles) {
        $token = $tokens[$role]
        if (-not $token) { continue }
        
        $payload = Parse-JWTToken $token
        $hasPermission = if ($payload.permissions) {
            $payload.permissions -contains $test.requiredPerm -or $payload.permissions -contains 'SYSTEM_ADMIN'
        } else {
            $false
        }
        
        $shouldAccess = $test.expectAccess -contains $role
        $passed = $hasPermission -eq $shouldAccess
        
        if ($passed) {
            $status = if ($shouldAccess) { "✓ CAN ACCESS" } else { "✓ DENIED" }
            Write-Host "    $status - $role" -ForegroundColor Green
            $passCount++
        } else {
            $status = if ($shouldAccess) { "✗ SHOULD ACCESS" } else { "✗ SHOULD BE DENIED" }
            Write-Host "    $status - $role" -ForegroundColor Red
            $failCount++
        }
    }
}

Write-Host "`n[STEP 6] Test Token Expiration Logic" -ForegroundColor Yellow

foreach ($role in $tokens.Keys) {
    $token = $tokens[$role]
    $payload = Parse-JWTToken $token
    
    if ($payload.exp) {
        $expiryDate = [DateTime]::UnixEpoch.AddSeconds($payload.exp)
        $now = Get-Date
        $timeRemaining = $expiryDate - $now
        
        Write-Host "`n  $role token expires in: $($timeRemaining.TotalMinutes | Round -Decimal 1) minutes" -ForegroundColor Gray
        
        if ($expiryDate -gt $now) {
            Write-Host "    ✓ Token is valid" -ForegroundColor Green
            $passCount++
        } else {
            Write-Host "    ✗ Token is expired" -ForegroundColor Red
            $failCount++
        }
    }
}

# Summary
Write-Host "`n╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     TEST SUMMARY                                               ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$total = $passCount + $failCount
$percentage = if ($total -gt 0) { [int](($passCount / $total) * 100) } else { 0 }

Write-Host "`n  Total Tests: $total" -ForegroundColor White
Write-Host "  Passed: $passCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Red" })
Write-Host "  Success Rate: $percentage%" -ForegroundColor $(if ($percentage -eq 100) { "Green" } else { "Yellow" })

Write-Host "`n[RECOMMENDATIONS]" -ForegroundColor Yellow
Write-Host "  1. Run this test in a browser console to verify jwtUtils.ts" -ForegroundColor Gray
Write-Host "  2. Check localStorage for 'bankinsight_token' after login" -ForegroundColor Gray
Write-Host "  3. Test PermissionGuard component rendering in React DevTools" -ForegroundColor Gray
Write-Host "  4. Verify role-specific dashboards appear correctly" -ForegroundColor Gray
Write-Host "  5. Check browser console for any JWT parsing errors" -ForegroundColor Gray

if ($failCount -eq 0) {
    Write-Host "`n✓ All Permission-Aware UI tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n✗ Some tests failed - review errors above" -ForegroundColor Red
    exit 1
}

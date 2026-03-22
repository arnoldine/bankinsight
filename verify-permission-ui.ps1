#!/usr/bin/env pwsh
<#
.SYNOPSIS
Permission-Aware UI Component Verification Script

.DESCRIPTION
Verifies that all permission-aware UI components are properly created and integrated
#>

Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Permission-Aware UI Component Verification                   ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan

$basePath = "c:\Backup old\dev\bankinsight"
$components = @(
    @{ name = "jwtUtils.ts"; path = "$basePath\lib\jwtUtils.ts"; type = "Utility Library"; description = "JWT parsing and permission checking" },
    @{ name = "PermissionGuard.tsx"; path = "$basePath\components\PermissionGuard.tsx"; type = "React Component"; description = "Conditional rendering based on permissions" },
    @{ name = "RoleBasedDashboard.tsx"; path = "$basePath\components\RoleBasedDashboard.tsx"; type = "React Component"; description = "Role information and feature overview" },
    @{ name = "TellerDashboard.tsx"; path = "$basePath\components\TellerDashboard.tsx"; type = "React Component"; description = "Teller-specific dashboard view" },
    @{ name = "BranchManagerDashboard.tsx"; path = "$basePath\components\BranchManagerDashboard.tsx"; type = "React Component"; description = "Branch Manager-specific dashboard view" },
    @{ name = "AdminDashboard.tsx"; path = "$basePath\components\AdminDashboard.tsx"; type = "React Component"; description = "Admin-specific dashboard view" }
)

Write-Host "`n[COMPONENT VERIFICATION]`n" -ForegroundColor Yellow

$passCount = 0
$failCount = 0

foreach ($component in $components) {
    $exists = Test-Path $component.path
    
    if ($exists) {
        $fileSize = (Get-Item $component.path).Length
        $lines = (Get-Content $component.path | Measure-Object -Line).Lines
        
        Write-Host "✓ $($component.name)" -ForegroundColor Green
        Write-Host "  Type: $($component.type)" -ForegroundColor Gray
        Write-Host "  Description: $($component.description)" -ForegroundColor Gray
        Write-Host "  Size: $([math]::Round($fileSize / 1KB, 2)) KB | Lines: $lines" -ForegroundColor Gray
        Write-Host ""
        $passCount++
    } else {
        Write-Host "✗ $($component.name) - NOT FOUND" -ForegroundColor Red
        Write-Host "  Expected path: $($component.path)" -ForegroundColor Red
        Write-Host ""
        $failCount++
    }
}

Write-Host "`n[INTEGRATION POINTS]`n" -ForegroundColor Yellow

# Check App.tsx for integration
$appPath = "$basePath\App.tsx"
if (Test-Path $appPath) {
    $appContent = Get-Content $appPath -Raw
    
    if ($appContent -match "RoleBasedDashboard") {
        Write-Host "✓ App.tsx imports RoleBasedDashboard" -ForegroundColor Green
    } else {
        Write-Host "✗ App.tsx does not import RoleBasedDashboard" -ForegroundColor Red
    }
    
    if ($appContent -match "<RoleBasedDashboard") {
        Write-Host "✓ App.tsx uses RoleBasedDashboard component" -ForegroundColor Green
    } else {
        Write-Host "✗ App.tsx does not use RoleBasedDashboard component" -ForegroundColor Red
    }
}

# Check useBankingSystem for integration
$hookPath = "$basePath\hooks\useBankingSystem.ts"
if (Test-Path $hookPath) {
    $hookContent = Get-Content $hookPath -Raw
    
    if ($hookContent -match "from.*jwtUtils") {
        Write-Host "✓ useBankingSystem imports jwtUtils" -ForegroundColor Green
    } else {
        Write-Host "✗ useBankingSystem does not import jwtUtils" -ForegroundColor Red
    }
    
    if ($hookContent -match "checkPermission\(authToken") {
        Write-Host "✓ useBankingSystem uses jwtUtils for permission checking" -ForegroundColor Green
    } else {
        Write-Host "⚠ useBankingSystem may not be using jwtUtils correctly" -ForegroundColor Yellow
    }
}

Write-Host "`n[KEY FEATURES]`n" -ForegroundColor Yellow

$features = @(
    @{ feature = "JWT Token Parsing"; file = "jwtUtils.ts"; check = "decodeJWT" },
    @{ feature = "Single Permission Check"; file = "jwtUtils.ts"; check = "hasPermission" },
    @{ feature = "Multi-Permission (OR)"; file = "jwtUtils.ts"; check = "hasAnyPermission" },
    @{ feature = "Multi-Permission (AND)"; file = "jwtUtils.ts"; check = "hasAllPermissions" },
    @{ feature = "Permission Extraction"; file = "jwtUtils.ts"; check = "getPermissionsFromToken" },
    @{ feature = "Role Extraction"; file = "jwtUtils.ts"; check = "getRoleFromToken" },
    @{ feature = "Token Expiration Check"; file = "jwtUtils.ts"; check = "isTokenExpired" },
    @{ feature = "PermissionGuard Component"; file = "PermissionGuard.tsx"; check = "<PermissionGuard" },
    @{ feature = "Role Dashboard Header"; file = "RoleBasedDashboard.tsx"; check = "RoleBasedDashboard" },
    @{ feature = "Teller Dashboard"; file = "TellerDashboard.tsx"; check = "TellerDashboard" },
    @{ feature = "Branch Manager Dashboard"; file = "BranchManagerDashboard.tsx"; check = "BranchManagerDashboard" },
    @{ feature = "Admin Dashboard"; file = "AdminDashboard.tsx"; check = "AdminDashboard" }
)

foreach ($feature in $features) {
    $filePath = "$basePath\$($feature.file -replace 'jwtUtils.ts', 'lib/jwtUtils.ts' -replace '\.tsx$', '' | ForEach-Object { if ($_.contains('Dashboard')) { "components/$_" } else { $_ } }).tsx"
    if ($feature.file -match 'jwtUtils') {
        $filePath = "$basePath\lib\jwtUtils.ts"
    }
    
    if ((Test-Path $filePath) -and ((Get-Content $filePath -Raw) -match [regex]::Escape($feature.check))) {
        Write-Host "  ✓ $($feature.feature)" -ForegroundColor Green
        $passCount++
    } else {
        Write-Host "  ✗ $($feature.feature)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "`n[DOCUMENTATION]`n" -ForegroundColor Yellow

$docs = @(
    @{ name = "PERMISSION-AWARE-UI-GUIDE.md"; path = "$basePath\PERMISSION-AWARE-UI-GUIDE.md" }
)

foreach ($doc in $docs) {
    if (Test-Path $doc.path) {
        $docSize = (Get-Item $doc.path).Length
        $docLines = (Get-Content $doc.path | Measure-Object -Line).Lines
        
        Write-Host "✓ $($doc.name)" -ForegroundColor Green
        Write-Host "  Size: $([math]::Round($docSize / 1KB, 2)) KB | Lines: $docLines" -ForegroundColor Gray
    } else {
        Write-Host "✗ $($doc.name) - NOT FOUND" -ForegroundColor Red
    }
}

Write-Host "`n[TESTING]`n" -ForegroundColor Yellow

$testFile = "$basePath\permission-aware-ui-test.ps1"
if (Test-Path $testFile) {
    Write-Host "✓ permission-aware-ui-test.ps1 exists" -ForegroundColor Green
    Write-Host "  Run with: .\permission-aware-ui-test.ps1" -ForegroundColor Gray
} else {
    Write-Host "✗ permission-aware-ui-test.ps1 not found" -ForegroundColor Red
}

Write-Host "`n[SUMMARY]`n" -ForegroundColor Cyan

$allComponents = $components.Count
$foundComponents = $passCount
$percentage = [int](($foundComponents / $allComponents) * 100)

Write-Host "  Components Created: $foundComponents / $allComponents" -ForegroundColor $(if ($foundComponents -eq $allComponents) { "Green" } else { "Yellow" })
Write-Host "  Completion: $percentage%" -ForegroundColor $(if ($percentage -eq 100) { "Green" } else { "Yellow" })

Write-Host "`n[NEXT STEPS]`n" -ForegroundColor Yellow

Write-Host "  1. Run permission-aware-ui-test.ps1 to verify backend JWT handling" -ForegroundColor Gray
Write-Host "  2. Test login with different roles and check UI adaptation" -ForegroundColor Gray
Write-Host "  3. Verify sidebar items appear/disappear based on permissions" -ForegroundColor Gray
Write-Host "  4. Check role-specific dashboards display correctly" -ForegroundColor Gray
Write-Host "  5. Wrap additional components with PermissionGuard as needed" -ForegroundColor Gray
Write-Host "  6. Deploy to test environment for user acceptance testing" -ForegroundColor Gray

Write-Host "`n[COMMAND REFERENCE]`n" -ForegroundColor Yellow

Write-Host "  Login as Admin:" -ForegroundColor Gray
Write-Host "    Email: admin@bankinsight.com" -ForegroundColor Gray
Write-Host "    Password: Admin@12345" -ForegroundColor Gray
Write-Host "    Expected: All features visible" -ForegroundColor Gray
Write-Host ""

Write-Host "  Login as Teller:" -ForegroundColor Gray
Write-Host "    Email: teller@bankinsight.com" -ForegroundColor Gray
Write-Host "    Password: Teller@12345" -ForegroundColor Gray
Write-Host "    Expected: Limited to Teller operations" -ForegroundColor Gray
Write-Host ""

Write-Host "  Login as Branch Manager:" -ForegroundColor Gray
Write-Host "    Email: branch_mgr@bankinsight.com" -ForegroundColor Gray
Write-Host "    Password: BranchMgr@12345" -ForegroundColor Gray
Write-Host "    Expected: Approvals and branch management visible" -ForegroundColor Gray

if ($failCount -eq 0) {
    Write-Host "`n✓ All permission-aware UI components successfully created! ✓`n" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠ Some components need attention. Review errors above.`n" -ForegroundColor Yellow
    exit 1
}

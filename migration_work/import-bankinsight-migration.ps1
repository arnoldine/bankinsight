param(
    [string]$ApiBaseUrl = "http://localhost:5176",
    [string]$PreparedDir = "C:\Backup old\dev\bankinsight\migration_work\prepared",
    [string]$Email = "",
    [string]$Password = "",
    [string]$Token = "",
    [switch]$SkipGlAccounts
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost {
    param(
        [string]$Uri,
        [object]$Body
    )

    return Invoke-RestMethod -Uri $Uri -Method Post -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 10)
}

function Get-AuthHeaders {
    param([string]$ApiBaseUrl, [string]$Email, [string]$Password, [string]$Token)

    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        return @{ Authorization = "Bearer $Token" }
    }

    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        throw "Provide either -Token or both -Email and -Password."
    }

    $login = Invoke-JsonPost -Uri "$ApiBaseUrl/api/auth/login" -Body @{
        email = $Email
        password = $Password
    }

    if ($login.mfaRequired -eq $true) {
        $code = if ($login.debugCode) {
            Write-Host "Using development MFA debug code." -ForegroundColor Yellow
            $login.debugCode
        }
        else {
            Read-Host "Enter MFA code"
        }

        $session = Invoke-JsonPost -Uri "$ApiBaseUrl/api/auth/mfa/verify" -Body @{
            mfaToken = $login.mfaToken
            code = $code
        }

        return @{ Authorization = "Bearer $($session.token)" }
    }

    return @{ Authorization = "Bearer $($login.token)" }
}

function Invoke-MigrationImport {
    param(
        [string]$ApiBaseUrl,
        [hashtable]$Headers,
        [string]$Dataset,
        [string]$FilePath
    )

    if (-not (Test-Path $FilePath)) {
        throw "File not found: $FilePath"
    }

    Write-Host "Importing dataset '$Dataset' from '$FilePath'..." -ForegroundColor Cyan

    $form = @{
        file = Get-Item $FilePath
    }

    $response = Invoke-RestMethod -Uri "$ApiBaseUrl/api/migration/import/$Dataset" -Method Post -Headers $Headers -Form $form

    Write-Host ("  Imported: {0}, Updated: {1}, Failed: {2}" -f $response.imported, $response.updated, $response.failed) -ForegroundColor Green

    if ($response.errors -and $response.errors.Count -gt 0) {
        Write-Host "  Errors:" -ForegroundColor Yellow
        $response.errors | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    }

    return $response
}

$headers = Get-AuthHeaders -ApiBaseUrl $ApiBaseUrl -Email $Email -Password $Password -Token $Token

$datasets = @(
    @{ Name = "customers"; File = (Join-Path $PreparedDir "customers.csv") },
    @{ Name = "products"; File = (Join-Path $PreparedDir "products.csv") },
    @{ Name = "accounts"; File = (Join-Path $PreparedDir "accounts.csv") },
    @{ Name = "loans"; File = (Join-Path $PreparedDir "loans.csv") }
)

if (-not $SkipGlAccounts) {
    $datasets += @{ Name = "gl_accounts"; File = (Join-Path $PreparedDir "gl_accounts_template.csv") }
}

$results = foreach ($dataset in $datasets) {
    Invoke-MigrationImport -ApiBaseUrl $ApiBaseUrl -Headers $headers -Dataset $dataset.Name -FilePath $dataset.File
}

$summaryPath = Join-Path $PreparedDir "import-results-summary.json"
$results | ConvertTo-Json -Depth 10 | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host ""
Write-Host "Import run complete." -ForegroundColor Green
Write-Host "Summary written to: $summaryPath" -ForegroundColor Green

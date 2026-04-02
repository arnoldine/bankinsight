param(
    [string]$ApiBaseUrl = "http://localhost:5176",
    [string]$PreparedDir = "C:\Backup old\dev\bankinsight\migration_work\prepared",
    [string]$Email = "",
    [string]$Password = "",
    [string]$Token = ""
)

$ErrorActionPreference = "Stop"

function Get-AuthHeaders {
    param([string]$ApiBaseUrl, [string]$Email, [string]$Password, [string]$Token)

    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        return @{ Authorization = "Bearer $Token" }
    }

    if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
        throw "Provide either -Token or both -Email and -Password."
    }

    $login = Invoke-RestMethod -Uri "$ApiBaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body (@{
        email = $Email
        password = $Password
    } | ConvertTo-Json)

    if ($login.mfaRequired -eq $true) {
        $code = if ($login.debugCode) { $login.debugCode } else { Read-Host "Enter MFA code" }
        $session = Invoke-RestMethod -Uri "$ApiBaseUrl/api/auth/mfa/verify" -Method Post -ContentType "application/json" -Body (@{
            mfaToken = $login.mfaToken
            code = $code
        } | ConvertTo-Json)
        return @{ Authorization = "Bearer $($session.token)" }
    }

    return @{ Authorization = "Bearer $($login.token)" }
}

$headers = Get-AuthHeaders -ApiBaseUrl $ApiBaseUrl -Email $Email -Password $Password -Token $Token

$expected = [ordered]@{
    customers = (Import-Csv (Join-Path $PreparedDir "customers.csv")).Count
    products  = (Import-Csv (Join-Path $PreparedDir "products.csv")).Count
    accounts  = (Import-Csv (Join-Path $PreparedDir "accounts.csv")).Count
    loans     = (Import-Csv (Join-Path $PreparedDir "loans.csv")).Count
}

$actual = [ordered]@{
    customers = (Invoke-RestMethod -Uri "$ApiBaseUrl/api/customers" -Headers $headers -Method Get).Count
    products  = (Invoke-RestMethod -Uri "$ApiBaseUrl/api/products" -Headers $headers -Method Get).Count
    accounts  = (Invoke-RestMethod -Uri "$ApiBaseUrl/api/accounts" -Headers $headers -Method Get).Count
    loans     = (Invoke-RestMethod -Uri "$ApiBaseUrl/api/loans" -Headers $headers -Method Get).Count
}

$rows = foreach ($key in $expected.Keys) {
    [pscustomobject]@{
        dataset = $key
        expected_count = $expected[$key]
        actual_count = $actual[$key]
        delta = ($actual[$key] - $expected[$key])
        status = if ($actual[$key] -ge $expected[$key]) { "OK_OR_HIGHER" } else { "LOWER_THAN_EXPECTED" }
    }
}

$resultPath = Join-Path $PreparedDir "post-import-verification.csv"
$rows | Export-Csv -Path $resultPath -NoTypeInformation -Encoding UTF8

$rows | Format-Table -AutoSize
Write-Host "Verification written to: $resultPath" -ForegroundColor Green

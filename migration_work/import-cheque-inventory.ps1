param(
    [string]$ApiBaseUrl = "http://localhost:5176",
    [string]$PreparedDir = "C:\Backup old\dev\bankinsight\migration_work\prepared",
    [string]$Email = "",
    [string]$Password = "",
    [string]$Token = "",
    [switch]$SkipIssue
)

$ErrorActionPreference = "Stop"

function Invoke-JsonPost {
    param(
        [string]$Uri,
        [object]$Body,
        [hashtable]$Headers
    )

    return Invoke-RestMethod -Uri $Uri -Method Post -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 10)
}

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

function Get-SeriesPrefix {
    param([string]$ChequeNo)
    return ([regex]::Match($ChequeNo, '^[^\d]*')).Value.ToUpperInvariant()
}

function Get-SerialNumber {
    param([string]$ChequeNo)
    $digits = ([regex]::Match($ChequeNo, '(\d+)$')).Groups[1].Value
    if ([string]::IsNullOrWhiteSpace($digits)) { return $null }
    return [long]$digits
}

$headers = Get-AuthHeaders -ApiBaseUrl $ApiBaseUrl -Email $Email -Password $Password -Token $Token
$inventory = Import-Csv (Join-Path $PreparedDir "cheque_inventory.csv")

$books = $inventory |
    Where-Object { $_.account_id -and $_.range -and $_.cheque_no } |
    Group-Object account_id,range

$results = New-Object System.Collections.Generic.List[object]
$usedLeaves = New-Object System.Collections.Generic.List[object]
$reconciledLeaves = New-Object System.Collections.Generic.List[object]

foreach ($group in $books) {
    $sample = $group.Group | Select-Object -First 1
    $prefix = Get-SeriesPrefix $sample.cheque_no
    $startSerial = if ($sample.first_number) { [long]$sample.first_number } else { Get-SerialNumber $sample.cheque_no }
    $endSerial = if ($sample.last_number) { [long]$sample.last_number } else { ($group.Group | ForEach-Object { Get-SerialNumber $_.cheque_no } | Measure-Object -Maximum).Maximum }
    $leafCount = [int](($endSerial - $startSerial) + 1)
    $branchId = "BR001"

    if (-not $startSerial -or -not $endSerial -or $leafCount -le 0) {
        $results.Add([pscustomobject]@{
            account_id = $sample.account_id
            range = $sample.range
            status = "SKIPPED"
            reason = "Invalid cheque serial range"
        }) | Out-Null
        continue
    }

    try {
        $stock = Invoke-JsonPost -Uri "$ApiBaseUrl/api/payments/cheque-books/stock" -Headers $headers -Body @{
            branchId = $branchId
            seriesPrefix = $prefix
            startSerialNumber = $startSerial
            leafCount = $leafCount
            remarks = "Seeded from legacy cheque inventory range $($sample.range)"
        }

        if (-not $SkipIssue) {
            $issued = Invoke-JsonPost -Uri "$ApiBaseUrl/api/payments/cheque-books/$($stock.id)/issue" -Headers $headers -Body @{
                accountId = $sample.account_id
                issuedBy = "migration"
                remarks = "Issued from legacy cheque inventory"
            }

            $results.Add([pscustomobject]@{
                account_id = $sample.account_id
                range = $sample.range
                book_id = $issued.id
                book_reference = $issued.bookReference
                status = "ISSUED"
                leaf_count = $issued.leafCount
            }) | Out-Null
        }
        else {
            $results.Add([pscustomobject]@{
                account_id = $sample.account_id
                range = $sample.range
                book_id = $stock.id
                book_reference = $stock.bookReference
                status = "IN_STOCK"
                leaf_count = $stock.leafCount
            }) | Out-Null
        }
    }
    catch {
        $results.Add([pscustomobject]@{
            account_id = $sample.account_id
            range = $sample.range
            status = "FAILED"
            reason = $_.Exception.Message
        }) | Out-Null
    }

    foreach ($leaf in $group.Group | Where-Object { $_.used_flag -eq 'YES' -or ($_.cheque_status -and $_.cheque_status -ne 'PassedToClient') }) {
        try {
            $history = Invoke-JsonPost -Uri "$ApiBaseUrl/api/payments/cheque-books/leaves/use-history" -Headers $headers -Body @{
                accountId = $leaf.account_id
                chequeNumber = $leaf.cheque_no
                historicalTransactionId = if ($leaf.date_used) { "LEGACY-$($leaf.cheque_no)-$($leaf.date_used)" } else { "LEGACY-$($leaf.cheque_no)" }
                usedAt = if ($leaf.date_used) { $leaf.date_used } else { $null }
                remarks = "Historical cheque leaf usage imported from legacy CBS"
            }

            $reconciledLeaves.Add([pscustomobject]@{
                account_id = $leaf.account_id
                cheque_no = $leaf.cheque_no
                cheque_status = $leaf.cheque_status
                used_flag = $leaf.used_flag
                date_used = $leaf.date_used
                result = "MARKED_USED"
                book_id = $history.id
            }) | Out-Null
        }
        catch {
            $usedLeaves.Add([pscustomobject]@{
                account_id = $leaf.account_id
                cheque_no = $leaf.cheque_no
                cheque_status = $leaf.cheque_status
                used_flag = $leaf.used_flag
                date_used = $leaf.date_used
                note = $_.Exception.Message
            }) | Out-Null
        }
    }
}

$resultsPath = Join-Path $PreparedDir "cheque-inventory-import-results.csv"
$usedPath = Join-Path $PreparedDir "cheque-inventory-used-leaves-review.csv"
$reconciledPath = Join-Path $PreparedDir "cheque-inventory-used-leaves-imported.csv"

$results | Export-Csv -Path $resultsPath -NoTypeInformation -Encoding UTF8
$usedLeaves | Export-Csv -Path $usedPath -NoTypeInformation -Encoding UTF8
$reconciledLeaves | Export-Csv -Path $reconciledPath -NoTypeInformation -Encoding UTF8

Write-Host "Cheque inventory seeding complete." -ForegroundColor Green
Write-Host "Results: $resultsPath" -ForegroundColor Green
Write-Host "Used/reconcile review: $usedPath" -ForegroundColor Yellow
Write-Host "Historically marked used leaves: $reconciledPath" -ForegroundColor Green

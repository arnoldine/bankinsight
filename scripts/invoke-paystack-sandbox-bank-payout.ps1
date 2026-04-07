param(
    [string]$BaseUrl = 'http://localhost:5176',
    [string]$IdempotencyKey = ([guid]::NewGuid().ToString()),
    [guid]$SourceWalletId = '11111111-1111-1111-1111-111111111111'
)

$payload = @{
    sourceWalletId = $SourceWalletId
    bankCode = '057'
    accountNumber = '0000000000'
    amount = 125.50
    currency = 'NGN'
    accountName = 'Paystack Test Beneficiary'
    narrative = 'BankInsight Paystack sandbox smoke test'
} | ConvertTo-Json

$headers = @{
    'Idempotency-Key' = $IdempotencyKey
}

Write-Host "Posting sandbox bank payout to $BaseUrl/api/v1/transfers/bank"
Write-Host "Using Paystack official test transfer account details for Nigerian merchants."
Write-Host "This is a provider connectivity smoke test, not a Ghana production payout simulation."

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/v1/transfers/bank" -Headers $headers -ContentType 'application/json' -Body $payload

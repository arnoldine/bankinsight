try {
    $body = @{ email = "admin@faymo.com"; password = "password123" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:5176/api/auth/login" -Method Post -Body $body -ContentType "application/json" -SkipHttpErrorCheck
    Write-Host "Response:"
    Write-Host ($response | ConvertTo-Json)
} catch {
    Write-Host "Failed."
}

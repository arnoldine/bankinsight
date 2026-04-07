param(
    [string]$Urls = 'http://localhost:5176;https://localhost:7100'
)

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Persistence__Provider = 'InMemory'

Write-Host 'Starting BankInsight.API in Development with InMemory fintech persistence.'
Write-Host 'Paystack bank-transfer settings come from appsettings.Development.json plus local user secrets.'

dotnet run --project 'C:\Backup old\dev\bankinsight\BankInsight.API\BankInsight.API.csproj' --urls $Urls

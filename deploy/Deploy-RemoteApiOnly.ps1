[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$RemoteHost = "root@198.23.221.10",
    [string]$RemoteRoot = "/opt/bankinsight-api-runtime",
    [string]$ComposeFileName = "docker-compose.api-only.yml",
    [string]$LocalRuntimeDir = "deploy/remote-api-runtime",
    [string]$LocalComposeFile = "deploy/bankinsight-api-only.compose.yml",
    [string]$ProjectFile = "BankInsight.API/BankInsight.API.csproj",
    [string]$BuildConfiguration = "Release",
    [switch]$StageOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$runtimePath = Join-Path $repoRoot $LocalRuntimeDir
$publishPath = Join-Path $runtimePath "publish"
$dockerfilePath = Join-Path $runtimePath "Dockerfile"
$composeSourcePath = Join-Path $repoRoot $LocalComposeFile
$projectPath = Join-Path $repoRoot $ProjectFile
$nugetConfigPath = Join-Path $repoRoot "NuGet.Local.Config"
$bankingOsSource = Join-Path $repoRoot "BankingOS"
$bankingOsTarget = Join-Path $runtimePath "BankingOS"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)][string]$Message,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )

    Write-Host "==> $Message" -ForegroundColor Cyan
    & $Action
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter()][string[]]$ArgumentList = @()
    )

    & $FilePath @ArgumentList

    if ($LASTEXITCODE -ne 0) {
        $joinedArgs = if ($ArgumentList.Count -gt 0) { " $($ArgumentList -join ' ')" } else { "" }
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath$joinedArgs"
    }
}

function Remove-DirectoryContents {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    Get-ChildItem -LiteralPath $Path -Force | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}

Invoke-Step -Message "Preparing local runtime staging folder" -Action {
    if (-not (Test-Path $runtimePath)) {
        New-Item -ItemType Directory -Path $runtimePath | Out-Null
    }

    if (-not (Test-Path $publishPath)) {
        New-Item -ItemType Directory -Path $publishPath | Out-Null
    }

    Remove-DirectoryContents -Path $publishPath

    if (Test-Path $bankingOsTarget) {
        Remove-Item -LiteralPath $bankingOsTarget -Recurse -Force
    }
}

Invoke-Step -Message "Publishing BankInsight.API for Linux runtime packaging" -Action {
    $publishArgs = @(
        "publish",
        $projectPath,
        "-c", $BuildConfiguration,
        "-o", $publishPath
    )

    if (Test-Path $nugetConfigPath) {
        $publishArgs += @("--configfile", $nugetConfigPath)
    }

    Invoke-NativeCommand -FilePath "dotnet" -ArgumentList $publishArgs
}

Invoke-Step -Message "Copying BankingOS support assets into runtime bundle" -Action {
    Copy-Item -Path $bankingOsSource -Destination $bankingOsTarget -Recurse -Force
}

if (-not (Test-Path $dockerfilePath)) {
    throw "Remote API runtime Dockerfile not found at $dockerfilePath"
}

if (-not (Test-Path $composeSourcePath)) {
    throw "Compose file not found at $composeSourcePath"
}

if ($StageOnly) {
    Write-Host "Local runtime bundle prepared at $runtimePath" -ForegroundColor Green
    return
}

$remoteRuntimeDir = "$RemoteRoot/remote-api-runtime"
$remoteComposePath = "$RemoteRoot/$ComposeFileName"

Invoke-Step -Message "Ensuring remote deployment directory exists" -Action {
    Invoke-NativeCommand -FilePath "ssh" -ArgumentList @($RemoteHost, "mkdir -p $RemoteRoot")
}

Invoke-Step -Message "Uploading runtime bundle and compose file to remote host" -Action {
    Invoke-NativeCommand -FilePath "scp" -ArgumentList @("-r", $runtimePath, "${RemoteHost}:$remoteRuntimeDir")
    Invoke-NativeCommand -FilePath "scp" -ArgumentList @($composeSourcePath, "${RemoteHost}:$remoteComposePath")
}

Invoke-Step -Message "Building and restarting the remote API-only stack" -Action {
    $remoteCommand = @"
set -e
cd $RemoteRoot
docker build -t bankinsight-api-runtime:latest ./remote-api-runtime
docker compose -f $ComposeFileName up -d --build
"@
    Invoke-NativeCommand -FilePath "ssh" -ArgumentList @($RemoteHost, $remoteCommand)
}

Invoke-Step -Message "Checking remote API health" -Action {
    Invoke-NativeCommand -FilePath "ssh" -ArgumentList @($RemoteHost, "curl -fsS http://localhost:5176/health")
}

Write-Host "Remote API-only deployment completed successfully." -ForegroundColor Green

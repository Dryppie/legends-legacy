param(
    [switch]$SkipDockerBuild,
    [switch]$SkipFrontendTests
)

$ErrorActionPreference = "Stop"

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$dashboardPath = Join-Path $root "LL/src/Presentation/liveops"
$artifactPath = Join-Path $root "artifacts/api.liveops"
$dockerfilePath = Join-Path $root "build/ll-liveops.dockerfile"
$npmCache = Join-Path ([System.IO.Path]::GetTempPath()) "legends-legacy-liveops-npm-cache"
$imageTag = if ([string]::IsNullOrWhiteSpace($env:IMAGE_TAG)) {
    "dev1"
} else {
    $env:IMAGE_TAG
}
$dockerRegistry = $env:DOCKER_REGISTRY

if (-not $SkipDockerBuild -and [string]::IsNullOrWhiteSpace($dockerRegistry)) {
    throw "DOCKER_REGISTRY is required when building and pushing the LiveOps image."
}

if (Test-Path -LiteralPath $artifactPath) {
    Remove-Item -LiteralPath $artifactPath -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactPath -Force | Out-Null

$previousNpmCache = $env:npm_config_cache
try {
    $env:npm_config_cache = $npmCache
    & npm --prefix $dashboardPath ci
    if ($LASTEXITCODE -ne 0) {
        throw "LiveOps dashboard dependency installation failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipFrontendTests) {
        & npm --prefix $dashboardPath test -- --no-progress
        if ($LASTEXITCODE -ne 0) {
            throw "LiveOps dashboard tests failed with exit code $LASTEXITCODE."
        }
    }

    & npm --prefix $dashboardPath run build
    if ($LASTEXITCODE -ne 0) {
        throw "LiveOps dashboard build failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:npm_config_cache = $previousNpmCache
}

& dotnet publish `
    (Join-Path $root "LL/src/API/API.LiveOps/API.LiveOps.csproj") `
    -c Release `
    -o $artifactPath `
    /p:UseAppHost=false `
    --no-self-contained
if ($LASTEXITCODE -ne 0) {
    throw "LiveOps API publish failed with exit code $LASTEXITCODE."
}

if ($SkipDockerBuild) {
    Write-Host "LiveOps artifact created at '$artifactPath'."
    return
}

& docker build `
    --push `
    --file $dockerfilePath `
    --tag "$($dockerRegistry)ll-liveops:$imageTag" `
    "$artifactPath/."
if ($LASTEXITCODE -ne 0) {
    throw "LiveOps image build failed with exit code $LASTEXITCODE."
}

<#
.SYNOPSIS
    Local entry point for the LL backend correctness tests.

.EXAMPLE
    ./build/run-tests.ps1

.EXAMPLE
    ./build/run-tests.ps1 -NoBuild
#>
param(
    [switch]$NoBuild,
    [string]$Configuration = "Release",
    [string]$Filter,
    [string]$ArtifactsPath
)

$ErrorActionPreference = "Stop"

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$testProject = Join-Path $root "LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj"
$artifactArguments = @()
if (-not [string]::IsNullOrWhiteSpace($ArtifactsPath)) {
    $artifactArguments = @("--artifacts-path", [IO.Path]::GetFullPath($ArtifactsPath, $root))
}

if (-not $NoBuild) {
    & dotnet build $testProject --configuration $Configuration @artifactArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Test project build failed with exit code $LASTEXITCODE."
    }
}

$filterArguments = @()
if (-not [string]::IsNullOrWhiteSpace($Filter)) {
    $filterArguments = @("--filter", $Filter)
}

& dotnet test $testProject @filterArguments @artifactArguments `
    --configuration $Configuration `
    --no-build `
    --logger "trx;LogFileName=tests.trx" `
    --results-directory (Join-Path $root "TestResults/tests")

if ($LASTEXITCODE -ne 0) {
    throw "Test suite failed with exit code $LASTEXITCODE."
}

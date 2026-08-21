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
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$testProject = Join-Path $root "LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj"

if (-not $NoBuild) {
    & dotnet build $testProject --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Test project build failed with exit code $LASTEXITCODE."
    }
}

& dotnet test $testProject `
    --configuration $Configuration `
    --no-build `
    --logger "trx;LogFileName=tests.trx" `
    --results-directory (Join-Path $root "TestResults/tests")

if ($LASTEXITCODE -ne 0) {
    throw "Test suite failed with exit code $LASTEXITCODE."
}

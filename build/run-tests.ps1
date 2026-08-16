<#
.SYNOPSIS
    Local entry point for the LL backend test suites.

.DESCRIPTION
    Always runs the fast correctness suite.

    The exhaustive balance suite (Category=BalanceFull) costs minutes and only produces new
    information once the equipment stat budget moves, so locally it runs only when
    EquipmentStatBudgetCatalog.BalanceVersion differs from the version recorded in
    .artifacts/balance-suite.version. After a successful balance run the stamp is rewritten, so
    the next run skips the suite again until the equipment version changes.

    In CI the balance suite is selected by workflow test filters instead, and only for release
    branches. See .github/workflows/backend-gate.yml and .github/workflows/LL-backend.yml.

.EXAMPLE
    ./build/run-tests.ps1
    Runs the fast suite, plus the balance suite when the equipment version changed.

.EXAMPLE
    ./build/run-tests.ps1 -IncludeBalance
    Forces the balance suite regardless of the recorded version.

.EXAMPLE
    ./build/run-tests.ps1 -SkipBalance
    Runs the fast suite only and leaves the stamp untouched.
#>
param(
    [switch]$IncludeBalance,
    [switch]$SkipBalance,
    [switch]$NoBuild,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ($IncludeBalance -and $SkipBalance) {
    throw "-IncludeBalance and -SkipBalance are mutually exclusive."
}

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$testProject = Join-Path $root "LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj"
$catalogPath = Join-Path $root "LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentStatBudgetCatalog.cs"
$stampPath = Join-Path $root ".artifacts/balance-suite.version"

function Get-EquipmentBalanceVersion {
    if (-not (Test-Path -LiteralPath $catalogPath)) {
        throw "Could not find the equipment stat budget catalog at '$catalogPath'."
    }

    $content = Get-Content -Raw -LiteralPath $catalogPath
    $match = [regex]::Match($content, 'public\s+const\s+int\s+BalanceVersion\s*=\s*(?<version>\d+)\s*;')
    if (-not $match.Success) {
        throw "Could not read EquipmentStatBudgetCatalog.BalanceVersion from '$catalogPath'."
    }

    return [int]$match.Groups["version"].Value
}

function Get-RecordedBalanceVersion {
    if (-not (Test-Path -LiteralPath $stampPath)) {
        return $null
    }

    $recorded = (Get-Content -Raw -LiteralPath $stampPath).Trim()
    $parsed = 0
    if ([int]::TryParse($recorded, [ref]$parsed)) {
        return $parsed
    }

    return $null
}

# Writes dotnet's output straight to the console; callers read $LASTEXITCODE afterwards.
function Invoke-TestSuite {
    param(
        [Parameter(Mandatory = $true)][string]$Filter,
        [Parameter(Mandatory = $true)][string]$ResultsName
    )

    $arguments = @(
        "test", $testProject,
        "--configuration", $Configuration,
        "--no-build",
        "--filter", $Filter,
        "--logger", "trx;LogFileName=$ResultsName.trx",
        "--results-directory", (Join-Path $root "TestResults/$ResultsName")
    )

    & dotnet @arguments
}

$equipmentVersion = Get-EquipmentBalanceVersion
$recordedVersion = Get-RecordedBalanceVersion

if ($SkipBalance) {
    $runBalance = $false
    $reason = "-SkipBalance was supplied."
}
elseif ($IncludeBalance) {
    $runBalance = $true
    $reason = "-IncludeBalance was supplied."
}
elseif ($null -eq $recordedVersion) {
    $runBalance = $true
    $reason = "no local balance run has been recorded yet."
}
elseif ($recordedVersion -ne $equipmentVersion) {
    $runBalance = $true
    $reason = "equipment balance version moved from v$recordedVersion to v$equipmentVersion."
}
else {
    $runBalance = $false
    $reason = "equipment balance version v$equipmentVersion is unchanged since the last balance run."
}

if ($null -eq $recordedVersion) {
    $recordedLabel = "(none)"
}
else {
    $recordedLabel = "v$recordedVersion"
}

if ($runBalance) {
    $decisionLabel = "run"
    $overrideValue = "1"
}
else {
    $decisionLabel = "skip"
    $overrideValue = "0"
}

Write-Host "Equipment balance version : v$equipmentVersion"
Write-Host "Recorded balance version  : $recordedLabel"
Write-Host "Balance suite             : $decisionLabel - $reason"
Write-Host ""

# Keep the in-test gate (BalanceSuiteGate) aligned with the decision made here.
$env:LL_RUN_BALANCE = $overrideValue

if (-not $NoBuild) {
    & dotnet build $testProject --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Test project build failed with exit code $LASTEXITCODE."
    }
}

Invoke-TestSuite -Filter "Category!=BalanceFull" -ResultsName "fast"
if ($LASTEXITCODE -ne 0) {
    throw "Fast correctness suite failed with exit code $LASTEXITCODE."
}

if (-not $runBalance) {
    Write-Host ""
    Write-Host "Balance suite skipped. Use -IncludeBalance to run it anyway."
    return
}

Invoke-TestSuite -Filter "Category=BalanceFull" -ResultsName "balance"
if ($LASTEXITCODE -ne 0) {
    throw "Balance suite failed with exit code $LASTEXITCODE. The balance stamp was left unchanged."
}

$stampDirectory = Split-Path -Parent $stampPath
if (-not (Test-Path -LiteralPath $stampDirectory)) {
    New-Item -ItemType Directory -Path $stampDirectory -Force | Out-Null
}

Set-Content -LiteralPath $stampPath -Value $equipmentVersion -NoNewline
Write-Host ""
Write-Host "Balance suite passed. Recorded equipment balance version v$equipmentVersion in '$stampPath'."

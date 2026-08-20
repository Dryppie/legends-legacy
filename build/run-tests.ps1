<#
.SYNOPSIS
    Local entry point for the LL backend test suites.

.DESCRIPTION
    Always runs the fast correctness suite.

    The exhaustive balance suite (Category=BalanceFull) costs minutes and only produces new
    information once a balance input moves, so locally it runs only when the composite balance
    identity differs from the identity recorded in .artifacts/balance-suite.version. The identity
    covers equipment, combat, reference-control, raid, cooperative-roster, Tower-analyzer,
    ability-data, raid-boss-data, and Tower-floor-data versions. After a successful balance run
    the stamp is rewritten.

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

.EXAMPLE
    ./build/run-tests.ps1 -IncludeBalance -BalanceTestFilter "FullyQualifiedName~Canonical_equipment_pacing_smoke"
    Runs the fast suite and only the matching exhaustive-balance diagnostics. Filtered runs never
    rewrite the composite balance stamp.
#>
param(
    [switch]$IncludeBalance,
    [switch]$SkipBalance,
    [switch]$NoBuild,
    [string]$BalanceTestFilter,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if ($IncludeBalance -and $SkipBalance) {
    throw "-IncludeBalance and -SkipBalance are mutually exclusive."
}
if (-not [string]::IsNullOrWhiteSpace($BalanceTestFilter) -and -not $IncludeBalance) {
    throw "-BalanceTestFilter requires -IncludeBalance."
}

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($root)) {
    throw "Unable to resolve the repository root."
}

$root = $root.Trim()
$testProject = Join-Path $root "LL/tests/EssenceSystem.Tests/EssenceSystem.Tests.csproj"
$stampPath = Join-Path $root ".artifacts/balance-suite.version"

function Get-IntegerConstant {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ConstantName
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Could not find balance input at '$Path'."
    }

    $content = Get-Content -Raw -LiteralPath $Path
    $pattern = 'public\s+const\s+int\s+' + [regex]::Escape($ConstantName) + '\s*=\s*(?<version>\d+)\s*;'
    $match = [regex]::Match($content, $pattern)
    if (-not $match.Success) {
        throw "Could not read integer constant '$ConstantName' from '$Path'."
    }

    return [int]$match.Groups["version"].Value
}

function Get-BalanceIdentity {
    $equipmentPath = Join-Path $root "LL/src/Core/Domain/Models/Professions/Crafting/V2/EquipmentStatBudgetCatalog.cs"
    $powerPath = Join-Path $root "LL/src/Core/Application/Interfaces/Services/LL/PowerRatings/IPowerRatingService.cs"
    $referencePath = Join-Path $root "LL/src/Infrastructure/Service/Services.LL/Balance/EquipmentCombatPacingAnalyzer.cs"
    $raidPath = Join-Path $root "LL/src/Core/Domain/Models/Raids/RaidDefinitions.cs"
    $rosterPath = Join-Path $root "LL/src/Infrastructure/Service/Services.LL/PowerRatings/CanonicalCooperativeRosterCatalog.cs"
    $towerPath = Join-Path $root "LL/src/Infrastructure/Service/Services.LL/WorldTower/WorldTowerBalanceAnalyzer.cs"
    $abilitiesPath = Join-Path $root "LL/src/API/API.LL/Data/combat/abilities.json"
    $raidBossesPath = Join-Path $root "LL/src/API/API.LL/Data/raids/raid-bosses.json"
    $towerFloorsPath = Join-Path $root "LL/src/API/API.LL/Data/world-tower/tower-floors.json"

    $equipmentVersion = Get-IntegerConstant -Path $equipmentPath -ConstantName "BalanceVersion"
    $combatVersion = Get-IntegerConstant -Path $powerPath -ConstantName "CombatRulesVersion"
    $referenceVersion = Get-IntegerConstant -Path $referencePath -ConstantName "ReferenceControlVersion"
    $raidVersion = Get-IntegerConstant -Path $raidPath -ConstantName "Version"
    $rosterVersion = Get-IntegerConstant -Path $rosterPath -ConstantName "Version"
    $towerVersion = Get-IntegerConstant -Path $towerPath -ConstantName "BalanceVersion"
    $abilitiesHash = (Get-FileHash -LiteralPath $abilitiesPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $raidBossesHash = (Get-FileHash -LiteralPath $raidBossesPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $towerFloorsHash = (Get-FileHash -LiteralPath $towerFloorsPath -Algorithm SHA256).Hash.ToLowerInvariant()

    return "equipment=$equipmentVersion|combat=$combatVersion|reference=$referenceVersion|raid=$raidVersion|roster=$rosterVersion|tower=$towerVersion|abilities=$abilitiesHash|raidBosses=$raidBossesHash|towerFloors=$towerFloorsHash"
}

function Get-RecordedBalanceIdentity {
    if (-not (Test-Path -LiteralPath $stampPath)) {
        return $null
    }

    $recorded = (Get-Content -Raw -LiteralPath $stampPath).Trim()
    return $(if ([string]::IsNullOrWhiteSpace($recorded)) { $null } else { $recorded })
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

$balanceIdentity = Get-BalanceIdentity
$recordedIdentity = Get-RecordedBalanceIdentity

if ($SkipBalance) {
    $runBalance = $false
    $reason = "-SkipBalance was supplied."
}
elseif ($IncludeBalance) {
    $runBalance = $true
    $reason = "-IncludeBalance was supplied."
}
elseif ($null -eq $recordedIdentity) {
    $runBalance = $true
    $reason = "no local balance run has been recorded yet."
}
elseif ($recordedIdentity -cne $balanceIdentity) {
    $runBalance = $true
    $reason = "one or more balance inputs changed since the last successful run."
}
else {
    $runBalance = $false
    $reason = "the composite balance identity is unchanged since the last successful run."
}

if ($null -eq $recordedIdentity) {
    $recordedLabel = "(none)"
}
else {
    $recordedLabel = $recordedIdentity
}

if ($runBalance) {
    $decisionLabel = "run"
    $overrideValue = "1"
}
else {
    $decisionLabel = "skip"
    $overrideValue = "0"
}

Write-Host "Current balance identity  : $balanceIdentity"
Write-Host "Recorded balance identity : $recordedLabel"
Write-Host "Balance suite              : $decisionLabel - $reason"
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

$resolvedBalanceFilter = "Category=BalanceFull"
if (-not [string]::IsNullOrWhiteSpace($BalanceTestFilter)) {
    $resolvedBalanceFilter += "&$($BalanceTestFilter.Trim())"
}

Invoke-TestSuite -Filter $resolvedBalanceFilter -ResultsName "balance"
if ($LASTEXITCODE -ne 0) {
    throw "Balance suite failed with exit code $LASTEXITCODE. The balance stamp was left unchanged."
}

if (-not [string]::IsNullOrWhiteSpace($BalanceTestFilter)) {
    Write-Host ""
    Write-Host "Filtered balance diagnostics passed. The balance stamp was left unchanged."
    return
}

$stampDirectory = Split-Path -Parent $stampPath
if (-not (Test-Path -LiteralPath $stampDirectory)) {
    New-Item -ItemType Directory -Path $stampDirectory -Force | Out-Null
}

Set-Content -LiteralPath $stampPath -Value $balanceIdentity -NoNewline
Write-Host ""
Write-Host "Balance suite passed. Recorded the composite balance identity in '$stampPath'."

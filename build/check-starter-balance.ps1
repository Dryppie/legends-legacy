<#
.SYNOPSIS
    Compare the two accepted starter references with one captured local build.
.DESCRIPTION
    Build and test the backend first. This script retains the compiled harness,
    captures nonsecret combat inputs, reuses the accepted trial schedules, and
    evaluates existing goals. It never changes or promotes a baseline.
.EXAMPLE
    ./build/check-starter-balance.ps1 -OutputDirectory TestResults/balance/starter-regression-001
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$BloodGroveBaseline = 'TestResults/balance/baselines/blood-grove-starter-v1.json',
    [string]$CrystalCreekBaseline = 'TestResults/balance/baselines/crystal-creek-starter-v1.json',
    [string]$ContentRoot = 'LL/src/API/API.LL',
    [string]$HarnessDirectory = 'LL/tools/BalanceHarness/bin/Release/net10.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
$content = [IO.Path]::GetFullPath($ContentRoot, $root)
$build = [IO.Path]::GetFullPath($HarnessDirectory, $root)
if (Test-Path -LiteralPath $output) { throw "Output already exists: $output" }
if (-not (Test-Path -LiteralPath (Join-Path $build 'BalanceHarness.dll'))) { throw 'Build and test the backend before running this check.' }

$catalogFiles = @(
    'combat-styles/combat-styles.v1.json', 'combat/abilities.json', 'combat/creature-abilities.json',
    'combat/statuses.json', 'combat/summons.json', 'equipment/equipment-named.v1.json',
    'equipment/equipment-sets.v1.json', 'equipment/equipment-starters.v1.json',
    'equipment/equipment-styles.v1.json', 'essences/essences.json', 'items/items.json',
    'progression/region-combat-balance.json', 'world/creature-essence-loot-tables.json',
    'world/creatures.json', 'world/regions.json'
)
$threatKeys = @(
    'Enabled', 'AttentionExponent', 'MinimumAttentionWeight', 'MaximumAttentionWeight',
    'ThreatHalfLifeSeconds', 'BasicAttackThreatValue', 'ProtectiveSelfThreatPerSecond',
    'ProtectiveAllyThreatPerSecond', 'RetaliationThreatPerSecond', 'SupportAllyThreatPerSecond',
    'HardControlThreatPerSecond', 'SoftControlThreatPerSecond', 'DamageThreatPerSecond',
    'SelfSustainThreatPerSecond', 'UtilityThreatPerSecond', 'MarkThreatBonus',
    'CoverBudgetMaxHealthFraction', 'DefaultSummonThreatMultiplier'
)
function Get-Hash([string]$Path) { (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Write-NewText([string]$Path, [string]$Value) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Value)
        $stream.Write($bytes, 0, $bytes.Length)
    } finally { $stream.Dispose() }
}
function Write-NewJson([string]$Path, $Value) {
    Write-NewText $Path ($Value | ConvertTo-Json -Depth 100)
}
function Read-Settings {
    $settings = Get-Content -LiteralPath (Join-Path $content 'appsettings.json') -Raw | ConvertFrom-Json
    $threat = [ordered]@{}
    foreach ($property in $settings.Combat.ThreatAndTanking.PSObject.Properties) {
        if ($property.Name -in $threatKeys) { $threat[$property.Name] = $property.Value }
        else { throw "Unrecognized threat setting; review the capture allowlist: $($property.Name)" }
    }
    [ordered]@{ Combat = [ordered]@{
        ThreatAndTanking = $threat
        IdleProgression = @{ EncounterCadenceSeconds = $settings.Combat.IdleProgression.EncounterCadenceSeconds }
    } }
}
$checkpoints = @(
    @{ Id = 'blood-grove'; Baseline = $BloodGroveBaseline; Goals = 'idle-blood-grove-starter-goals.json';
        GoalsHash = '2615bd0260c2f02865c3c77bb5914807f660721f4822874aa5a8757a78fd535b';
        SuiteId = 'idle-blood-grove-starter-v1'; Cells = 2; Samples = 10000; Seed = 918091 },
    @{ Id = 'crystal-creek'; Baseline = $CrystalCreekBaseline; Goals = 'idle-crystal-creek-starter-goals.json';
        GoalsHash = 'e3112d21b47fcbbdcd860b140b8e250c83ce1608e45604e6e979a10280ab70f0';
        SuiteId = 'idle-crystal-creek-starter-v1'; Cells = 16; Samples = 2000; Seed = 818092 }
)
$frozen = [ordered]@{}
$sourceHashes = [ordered]@{}
$summaries = [Collections.Generic.List[object]]::new()
$null = New-Item -ItemType Directory -Path $output
$tool = Join-Path $output 'executable/BalanceHarness.dll'
function Invoke-Harness([string[]]$Arguments, [int[]]$AllowedExits = @(0)) {
    & dotnet $tool @Arguments | Out-Host
    if ($LASTEXITCODE -notin $AllowedExits) { throw "Harness $($Arguments[0]) failed with exit code $LASTEXITCODE." }
}
function Verify-Frozen {
    foreach ($file in $frozen.Keys) {
        if ((Get-Hash $file) -ne $frozen[$file]) { throw "Frozen evidence changed: $file" }
    }
}
try {
    Copy-Item -LiteralPath $build -Destination (Join-Path $output 'executable') -Recurse
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $output 'executable') -File -Recurse) {
        $frozen[$file.FullName] = Get-Hash $file.FullName
    }
    $null = New-Item -ItemType Directory -Path (Join-Path $output 'fixtures')
    foreach ($file in $catalogFiles) {
        $source = Join-Path $content "Data/$file"
        $target = Join-Path $output "content/Data/$file"
        $sourceHashes[$source] = Get-Hash $source
        $null = New-Item -ItemType Directory -Path (Split-Path $target) -Force
        Copy-Item -LiteralPath $source -Destination $target
        $frozen[$target] = Get-Hash $target
        if ($frozen[$target] -ne $sourceHashes[$source]) { throw "Content changed during capture: $file" }
    }
    $settings = Read-Settings
    $settingsText = $settings | ConvertTo-Json -Depth 100 -Compress
    $settingsPath = Join-Path $output 'content/appsettings.json'
    Write-NewJson $settingsPath $settings
    $frozen[$settingsPath] = Get-Hash $settingsPath
    foreach ($checkpoint in $checkpoints) {
        $baselineFile = [IO.Path]::GetFullPath($checkpoint.Baseline, $root)
        $baseline = Get-Content -LiteralPath $baselineFile -Raw | ConvertFrom-Json
        $baselineRun = [IO.Path]::GetFullPath($baseline.runDirectory, (Split-Path $baselineFile))
        $savedText = Get-Content -LiteralPath (Join-Path $baselineRun 'suite-input.json') -Raw
        $saved = $savedText | ConvertFrom-Json
        if ($saved.definition.id -ne $checkpoint.SuiteId -or $saved.masterSeed -ne $checkpoint.Seed -or
            $saved.cells.Count -ne $checkpoint.Cells -or $saved.definition.samplesPerCell -ne $checkpoint.Samples -or
            @($saved.cells | Where-Object { $_.trials.Count -ne $checkpoint.Samples }).Count -ne 0) {
            throw "Accepted schedule changed: $($checkpoint.Id). Declare a separate protocol."
        }
        $suitePath = Join-Path $output "fixtures/$($checkpoint.Id).json"
        # PowerShell's JSON round-trip converts DateTimeOffset text to local time.
        # Preserve the exact saved definition, including timestamp offset and numeric spelling.
        $savedDocument = [Text.Json.JsonDocument]::Parse($savedText)
        try { $definitionText = $savedDocument.RootElement.GetProperty('definition').GetRawText() }
        finally { $savedDocument.Dispose() }
        Write-NewText $suitePath $definitionText
        if ([IO.File]::ReadAllText($suitePath) -cne $definitionText) { throw 'Saved fixture copy changed.' }
        $goalsSource = Join-Path $root "LL/tools/BalanceHarness/Fixtures/$($checkpoint.Goals)"
        $goalsPath = Join-Path $output "fixtures/$($checkpoint.Goals)"
        $sourceHashes[$goalsSource] = Get-Hash $goalsSource
        Copy-Item -LiteralPath $goalsSource -Destination $goalsPath
        foreach ($file in @($baselineFile, $suitePath, $goalsPath)) { $frozen[$file] = Get-Hash $file }
        $checkpoint.Baseline = $baselineFile
        $checkpoint.BaselineRun = $baselineRun
        $checkpoint.BaselineArtifactHash = $baseline.artifactHash
        $checkpoint.Suite = $suitePath
        $checkpoint.Goals = $goalsPath
        # Read and validate each complete accepted archive and approved policy before any new fight.
        $baselineEvaluation = Join-Path $output "$($checkpoint.Id)-baseline-evaluation"
        Invoke-Harness @('evaluate', '--run', $baselineRun, '--goals', $goalsPath, '--output', $baselineEvaluation)
        $review = Get-Content -LiteralPath (Join-Path $baselineEvaluation 'evaluation.json') -Raw | ConvertFrom-Json
        if ($review.goalsHash -ne $checkpoint.GoalsHash -or $review.runArtifactHash -ne $baseline.artifactHash -or
            $review.gateStatus -ne 'Pass' -or $review.checks.Count -ne 2) { throw 'Accepted evidence or reviewed policy differs.' }
    }
    $planPath = Join-Path $output 'plan.json'
    Write-NewJson $planPath ([ordered]@{
        Id = 'starter-regression-v1'; PlannedBattles = 52000; Checkpoints = $checkpoints
        FrozenFiles = $frozen; SourceHashes = $sourceHashes
        StoppingRule = 'One repetition of each accepted schedule. No pooling, sample extension, tuning or baseline promotion.'
        ReplaySelection = 'Trial index 0 in the four primary cells.'
    })
    $frozen[$planPath] = Get-Hash $planPath
    foreach ($checkpoint in $checkpoints) {
        Verify-Frozen
        $directory = Join-Path $output $checkpoint.Id
        $run = Join-Path $directory 'candidate'
        Write-Host "Checking $($checkpoint.Id): $($checkpoint.Cells * $checkpoint.Samples) battles."
        Invoke-Harness @('suite', '--content-root', (Join-Path $output 'content'), '--suite', $checkpoint.Suite,
            '--samples', [string]$checkpoint.Samples, '--seed', [string]$checkpoint.Seed, '--output', $run)
        Verify-Frozen
        $manifest = Get-Content -LiteralPath (Join-Path $run 'manifest.json') -Raw | ConvertFrom-Json
        foreach ($file in $catalogFiles) {
            if ($manifest.contentHashes.$file -ne $frozen[(Join-Path $output "content/Data/$file")]) { throw 'Archived content differs.' }
        }
        foreach ($assembly in $manifest.execution.assemblyHashes.PSObject.Properties) {
            if ($assembly.Value -ne $frozen[(Join-Path $output "executable/$($assembly.Name).dll")]) { throw 'Archived execution differs.' }
        }
        Invoke-Harness @('compare', '--baseline', $checkpoint.Baseline, '--run', $run, '--output', (Join-Path $directory 'comparison'))
        Invoke-Harness @('evaluate', '--run', $run, '--goals', $checkpoint.Goals, '--output', (Join-Path $directory 'evaluation')) @(0, 1, 3)
        $comparison = Get-Content -LiteralPath (Join-Path $directory 'comparison/comparison.json') -Raw | ConvertFrom-Json
        $evaluation = Get-Content -LiteralPath (Join-Path $directory 'evaluation/evaluation.json') -Raw | ConvertFrom-Json
        if ($comparison.status -ne 'Complete' -or $comparison.cells.Count -ne $checkpoint.Cells -or
            $comparison.baselineArtifactHash -ne $checkpoint.BaselineArtifactHash -or
            $evaluation.goalsHash -ne $checkpoint.GoalsHash) { throw 'Incomplete or incompatible comparison/policy.' }
        $inputRecord = Get-Content -LiteralPath (Join-Path $run 'suite-input.json') -Raw | ConvertFrom-Json
        $primaryIds = @($evaluation.checks | ForEach-Object cellId | Sort-Object -Unique)
        $null = New-Item -ItemType Directory -Path (Join-Path $directory 'replays')
        foreach ($cell in $inputRecord.cells | Where-Object { $_.id -in $primaryIds }) {
            $battle = $cell.trials[0].battleId
            & dotnet $tool replay --run $run --battle $battle --detailed > (Join-Path $directory "replays/$battle.json")
            if ($LASTEXITCODE -ne 0) { throw 'Detailed replay failed.' }
        }
        $summaries.Add([ordered]@{
            Id = $checkpoint.Id; ValidBattles = $checkpoint.Cells * $checkpoint.Samples
            GateStatus = $evaluation.gateStatus; Checks = $evaluation.checks
            GameplayChanges = ($comparison.cells | Measure-Object gameplayChanges -Sum).Sum
            OutcomeChanges = ($comparison.cells | Measure-Object outcomeChanges -Sum).Sum
            Replays = $primaryIds.Count; ArtifactHash = $evaluation.runArtifactHash
        })
    }
    Verify-Frozen
    $drift = @($sourceHashes.Keys | Where-Object { (Get-Hash $_) -ne $sourceHashes[$_] })
    $settingsDrift = (Read-Settings | ConvertTo-Json -Depth 100 -Compress) -cne $settingsText
    $gate = if (@($summaries | Where-Object GateStatus -EQ 'Fail').Count) { 'Fail' }
        elseif (@($summaries | Where-Object GateStatus -NE 'Pass').Count) { 'Inconclusive' } else { 'Pass' }
    Write-NewJson (Join-Path $output 'results.json') ([ordered]@{
        Status = 'Complete'; GateStatus = $gate; ValidBattles = 52000; Checkpoints = $summaries.ToArray()
        SourceDriftAfterCapture = $drift; SettingsDriftAfterCapture = $settingsDrift; BaselinesPromoted = $false
    })
    $lines = @('# Starter regression check', '', "Complete: 52,000 regression battles; existing policy: **$gate**.", '')
    foreach ($summary in $summaries) {
        $lines += "- $($summary.Id): $($summary.GateStatus); $($summary.GameplayChanges) changed gameplay records; $($summary.OutcomeChanges) changed outcomes; $($summary.Replays) matched replays."
    }
    $lines += @('', "Post-capture source changes: $($drift.Count); selected settings changed: $settingsDrift.",
        'Accepted baselines remain unchanged. Repeated schedules are not pooled samples. This result applies to the retained executable and captured content.')
    Set-Content -LiteralPath (Join-Path $output 'summary.md') -Value $lines -Encoding utf8
    Write-Host "Starter regression complete: $gate. Evidence: $output"
    if ($gate -eq 'Fail') { exit 1 }
    if ($gate -eq 'Inconclusive') { exit 3 }
} catch {
    Write-NewJson (Join-Path $output 'failure.json') @{ Status = 'Invalid'; Error = $_.Exception.Message }
    throw
}

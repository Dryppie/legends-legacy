<#
.SYNOPSIS
    Run the fixed Blood Grove Essence-training / Forge investigation offline.
.DESCRIPTION
    Crosses Essence levels 1/10 with equipment ranks 0/1/5 on both predeclared
    seed sets. Retains generated recipes, archived runs, detailed replays and
    per-cell evidence. Resource acquisition and elapsed progression are not simulated.
.EXAMPLE
    ./build/investigate-blood-grove.ps1 -OutputDirectory TestResults/balance/blood-grove-001
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [ValidateRange(1, 100)][int]$SamplesPerCell = 100,
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) { throw "Choose a new output directory: $output already exists." }
$null = New-Item -ItemType Directory -Path $output
$toolProject = Join-Path $root 'LL/tools/BalanceHarness/BalanceHarness.csproj'
$toolDll = Join-Path $root "LL/tools/BalanceHarness/bin/$Configuration/net10.0/BalanceHarness.dll"
$contentRoot = Join-Path $root 'LL/src/API/API.LL'
$sourceFile = Join-Path $root 'LL/tools/BalanceHarness/Fixtures/idle-first-hunt.json'
$source = Get-Content -Raw -LiteralPath $sourceFile | ConvertFrom-Json -AsHashtable
$stages = @($source.stages | Where-Object id -EQ 'blood-grove')
if ($source.id -ne 'idle-first-hunt-v1' -or $stages.Count -ne 1 -or
    $stages[0].builds.Count -ne 6 -or $stages[0].encounters.Count -ne 2) {
    throw 'The First Hunt source cohort changed; review this investigation matrix.'
}
$variants = @(foreach ($rank in @(0, 1, 5)) {
    foreach ($level in @(1, 10)) {
        [ordered]@{ id = "e$level-r$rank"; essenceLevel = $level; equipmentRank = $rank }
    }
})
$seedSets = @([ordered]@{ id = 'discovery'; seed = 1337 }, [ordered]@{ id = 'confirmation'; seed = 7331 })
$plan = [ordered]@{
    schemaVersion = 1
    purpose = 'Controlled progression probes, advisory only; no baseline or goal approval.'
    sourceFixtureHash = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash.ToLowerInvariant()
    samplesPerCell = $SamplesPerCell
    cellsPerVariant = 12
    plannedBattles = 12 * $SamplesPerCell * $variants.Count * $seedSets.Count
    variants = $variants
    seedSets = $seedSets
    stoppingRule = 'Run every cell and variant on both seed sets; do not extend the budget based on results.'
}
$plan | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath (Join-Path $output 'plan.json') -Encoding utf8
# These nonsecret economic sources are outside the combat bundle allowlist.
foreach ($relative in @('equipment/equipment-upgrades.v1.json', 'progression/character-experience.json', 'progression/area-experience.json')) {
    Copy-Item -LiteralPath (Join-Path $contentRoot "Data/$relative") -Destination (Join-Path $output ([IO.Path]::GetFileName($relative)))
}
$rows = [Collections.Generic.List[object]]::new()
$training = [Collections.Generic.List[object]]::new()
$summary = [Collections.Generic.List[string]]::new()
$summary.Add('# Blood Grove progression investigation')
$summary.Add('')
$summary.Add("Predeclared: $($plan.plannedBattles) battles; $SamplesPerCell per cell on each of two seed sets. Recipe and seed details: plan.json.")
$summary.Add('')
$summary.Add('Rank applies to both the weapon and Medium Mail. Level 10 is unascended and unevolved. These are conditional mechanical probes, not simulated or guaranteed player budgets. The two seed sets are reported separately. Clear-rate intervals are per-cell 95% Wilson intervals, without simultaneous coverage across the matrix. Gained/lost wins are descriptive paired counts against level 1 / rank 0 within the same seed set; they are not regression-policy comparisons.')
$summary.Add('')
$summary.Add('| Seed set | Variant | Cell | Wins / valid | Clear rate, 95% interval | Gained / lost wins | Mean non-win seconds |')
$summary.Add('| --- | --- | --- | --- | --- | --- | --- |')

function Invoke-Harness {
    param([string[]]$Arguments)
    & dotnet $toolDll @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Harness $($Arguments[0]) failed: $LASTEXITCODE. Retained output: $output" }
}
function Read-Observations([string]$Run) {
    $records = @{}
    Get-Content -LiteralPath (Join-Path $Run 'battles.jsonl') | ForEach-Object {
        $row = $_ | ConvertFrom-Json
        if ($records.ContainsKey($row.battleId)) { throw 'Duplicate battle evidence.' }
        $records.Add($row.battleId, $row)
    }
    return $records
}
function Number($Value) {
    if ($null -eq $Value) { return 'unavailable' }
    return ([double]$Value).ToString('0.0', [Globalization.CultureInfo]::InvariantCulture)
}

try {
    if (-not $NoBuild) {
        & dotnet build $toolProject --configuration $Configuration | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Harness build failed.' }
    }
    if (-not (Test-Path -LiteralPath $toolDll)) { throw 'Build the harness before using -NoBuild.' }
    foreach ($variant in $variants) {
        $suite = $source | ConvertTo-Json -Depth 100 | ConvertFrom-Json -AsHashtable
        $stage = $suite.stages | Where-Object id -EQ 'blood-grove'
        $suite.id = "blood-grove-$($variant.id)-v1"
        $suite.description = "Blood Grove controlled probe: Essence level $($variant.essenceLevel), both items rank $($variant.equipmentRank). No acquisition feasibility claim."
        $suite.samplesPerCell = $SamplesPerCell
        $suite.stages = @($stage)
        $stage.assumptions = @(
            'Character level 5; actual First Hunt Essence crossed with mace/wand; one fixed possible Medium Mail Armor Chest outcome.'
            "Both common, standard, tier-1 items have rank $($variant.equipmentRank), baseline rolls and no active style."
            "The single First Hunt Essence has level $($variant.essenceLevel), Ascension tier 0 and is unevolved."
            'No regional drops, other equipment, buffs or persistent bonuses; training/Forge costs are external budget assumptions, not simulated rewards.'
            'Keep original stage, build and encounter IDs to preserve paired seeds; changed recipes cannot be compared as regression baselines.'
        )
        foreach ($build in $stage.builds) { $build.rank = $variant.equipmentRank }
        $stage.essenceLevels = [ordered]@{}
        foreach ($id in @($stage.builds.essenceIds | Sort-Object -Unique)) { $stage.essenceLevels[$id] = $variant.essenceLevel }
        $suite | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $output "$($variant.id).json") -Encoding utf8
    }
    $discoverySeeds = @{}
    foreach ($set in $seedSets) {
        $baselineRows = $null
        $untrainedRuns = @{}
        foreach ($variant in $variants) {
            $run = Join-Path $output "$($set.id)-$($variant.id)"
            Invoke-Harness -Arguments @('suite', '--output', $run, '--suite', (Join-Path $output "$($variant.id).json"),
                '--content-root', $contentRoot, '--seed', "$($set.seed)")
            $score = Get-Content -Raw -LiteralPath (Join-Path $run 'scorecard.json') | ConvertFrom-Json
            if ($score.status -ne 'Complete' -or $score.valid -ne 12 * $SamplesPerCell -or $score.cells.Count -ne 12) {
                throw 'Incomplete investigation run.'
            }
            $observations = Read-Observations $run
            if ($observations.Count -ne $score.valid) { throw 'Missing battle observations.' }
            if ($null -eq $baselineRows) { $baselineRows = $observations }
            foreach ($cell in $score.cells) {
                $cellRows = @($observations.Values | Where-Object cellId -EQ $cell.cellId)
                if ($cellRows.Count -ne $SamplesPerCell) { throw 'Incomplete cell evidence.' }
                $gained = 0; $lost = 0
                foreach ($row in $cellRows) {
                    $before = $baselineRows[$row.battleId]
                    if ($null -eq $before -or $before.seed -ne $row.seed -or $before.index -ne $row.index -or $row.status -ne 'Completed') {
                        throw 'Unpaired or invalid battle evidence.'
                    }
                    if ($before.outcome -ne 'Victory' -and $row.outcome -eq 'Victory') { $gained++ }
                    if ($before.outcome -eq 'Victory' -and $row.outcome -ne 'Victory') { $lost++ }
                }
                if ($variant.id -eq 'e1-r0') {
                    if ($set.id -eq 'discovery') { $discoverySeeds[$cell.cellId] = @($cellRows.seed) }
                    elseif (@($cellRows | Where-Object { $_.seed -in $discoverySeeds[$cell.cellId] }).Count -gt 0) {
                        throw 'Discovery and confirmation seeds overlap.'
                    }
                }
                $rows.Add([ordered]@{ seedSet = $set.id; variant = $variant.id; cell = $cell; gainedWins = $gained; lostWins = $lost })
                $rate = "$(Number (100 * $cell.clearRate.rate))% [$(Number (100 * $cell.clearRate.lower)), $(Number (100 * $cell.clearRate.upper))]"
                $summary.Add("| $($set.id) | $($variant.id) | $($cell.cellId) | $($cell.wins) / $($cell.valid) | $rate | $gained / $lost | $(Number $cell.nonWinDurationSeconds.mean) |")
            }
            if ($variant.essenceLevel -eq 1) { $untrainedRuns[$variant.equipmentRank] = $run }
            else {
                $changed = 0
                foreach ($battleId in $observations.Keys) {
                    $name = "battles/$battleId.json"
                    $before = Get-Content -Raw -LiteralPath (Join-Path $untrainedRuns[$variant.equipmentRank] $name) | ConvertFrom-Json
                    $after = Get-Content -Raw -LiteralPath (Join-Path $run $name) | ConvertFrom-Json
                    if (($before.summary | ConvertTo-Json -Depth 100 -Compress) -cne ($after.summary | ConvertTo-Json -Depth 100 -Compress)) { $changed++ }
                }
                $training.Add([ordered]@{ seedSet = $set.id; rank = $variant.equipmentRank; pairedBattles = $observations.Count; changedCombatSummaries = $changed })
            }
            $battle = ($observations.Keys | Sort-Object | Select-Object -First 1)
            & dotnet $toolDll replay --run $run --battle $battle --detailed > (Join-Path $output "$($set.id)-$($variant.id)-replay.json")
            if ($LASTEXITCODE -ne 0) { throw 'Detailed replay failed.' }
        }
    }
    [ordered]@{ plan = $plan; cells = $rows.ToArray(); trainingPairs = $training.ToArray() } |
        ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding utf8
    $summary.Add('')
    foreach ($pair in $training) {
        $summary.Add("Training level 1 -> 10, $($pair.seedSet), rank $($pair.rank): $($pair.changedCombatSummaries) changed combat summaries / $($pair.pairedBattles) paired battles.")
    }
    $summary.Add('')
    $summary.Add('Completed the fixed matrix and one detailed replay per run. No goal evaluation, target approval or gameplay baseline promotion was performed.')
}
catch {
    $summary.Add('')
    $summary.Add('Investigation failed; retained partial evidence is not a completed matrix.')
    throw
}
finally {
    $summary -join [Environment]::NewLine | Set-Content -LiteralPath (Join-Path $output 'summary.md') -Encoding utf8
    Write-Host "Investigation summary: $(Join-Path $output 'summary.md')"
}

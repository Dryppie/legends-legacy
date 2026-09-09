#requires -Version 7.4
<#
.SYNOPSIS
    Verify a restored starter package independently of the game checkout.
.DESCRIPTION
    Checks every file before running the retained executables. Full verification
    reevaluates four saved suites and replays two fixed primary battles per suite.
    IntegrityOnly verifies file hashes without requiring the original runtime/OS.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackageDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$DotnetPath = 'dotnet',
    [switch]$IntegrityOnly
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not ('LegendsLegacy.Tools.BalanceEvidenceArchive' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'BalanceEvidenceArchive.cs')
}
$root = [IO.Path]::GetFullPath($PackageDirectory, $PWD.Path)
$output = [IO.Path]::GetFullPath($OutputDirectory, $PWD.Path)
if ($output.Equals($root, [StringComparison]::OrdinalIgnoreCase) -or
    $output.StartsWith($root.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Verification output must be outside the immutable package.'
}
if (Test-Path -LiteralPath $output) { throw 'Verification output already exists.' }
Write-Host 'Verifying the complete restored file inventory before executing anything.'
$index = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Verify($root)
$contract = $index.Contract.GetRawText() | ConvertFrom-Json
if ($contract.schemaVersion -ne 1 -or $contract.id -ne 'starter-baselines-v1' -or $contract.cases.Count -ne 4) {
    throw 'Unsupported starter package contract.'
}
function Resolve([string]$relative) { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Resolve($root, $relative) }
foreach ($anchor in $contract.anchors.PSObject.Properties) {
    if ([LegendsLegacy.Tools.BalanceEvidenceArchive]::Hash((Resolve $anchor.Name)) -ne $anchor.Value) {
        throw "Reviewed anchor differs: $($anchor.Name)"
    }
}
$null = New-Item -ItemType Directory -Path $output
$cases = [Collections.Generic.List[object]]::new()
try {
    if (-not $IntegrityOnly) {
        foreach ($case in $contract.cases) {
            $run = Resolve $case.run
            $tool = Resolve $case.engine
            $manifest = Get-Content -LiteralPath (Join-Path $run 'manifest.json') -Raw | ConvertFrom-Json
            foreach ($assembly in $manifest.execution.assemblyHashes.PSObject.Properties) {
                $assemblyPath = Join-Path (Split-Path $tool) "$($assembly.Name).dll"
                if ([LegendsLegacy.Tools.BalanceEvidenceArchive]::Hash($assemblyPath) -ne $assembly.Value) {
                    throw "Replay executable differs for $($case.id)."
                }
            }
            $directory = Join-Path $output $case.id
            $null = New-Item -ItemType Directory -Path $directory
            Write-Host "Checking restored $($case.id)."
            & $DotnetPath $tool evaluate --run $run --goals (Resolve $case.goals) --output (Join-Path $directory 'evaluation') *> (Join-Path $directory 'evaluation.log')
            if ($LASTEXITCODE -ne 0) { throw "Restored evaluation failed for $($case.id)." }
            $evaluation = Get-Content -LiteralPath (Join-Path $directory 'evaluation/evaluation.json') -Raw | ConvertFrom-Json
            if ($evaluation.gateStatus -ne 'Pass' -or $evaluation.goalsHash -ne $case.goalsHash -or
                $evaluation.runArtifactHash -ne $case.artifactHash -or $evaluation.checks.Count -ne 2) {
                throw "Restored policy/artifact differs for $($case.id)."
            }
            & $DotnetPath $tool compare --baseline (Resolve $case.baseline) --run $run --output (Join-Path $directory 'comparison') *> (Join-Path $directory 'comparison.log')
            if ($LASTEXITCODE -ne 0) { throw "Restored comparison failed for $($case.id)." }
            $comparison = Get-Content -LiteralPath (Join-Path $directory 'comparison/comparison.json') -Raw | ConvertFrom-Json
            if ($comparison.status -ne 'Complete' -or $comparison.cells.Count -ne $case.cells -or
                @($comparison.cells | Where-Object { $_.gameplayChanges -ne 0 -or $_.outcomeChanges -ne 0 }).Count) {
                throw "Restored comparison differs from the recorded unchanged result for $($case.id)."
            }
            $scorecard = Get-Content -LiteralPath (Join-Path $run 'scorecard.json') -Raw | ConvertFrom-Json
            if ($scorecard.status -ne 'Complete' -or $scorecard.valid -ne $case.battles -or $scorecard.planned -ne $case.battles) {
                throw 'Restored battle count differs.'
            }
            $saved = Get-Content -LiteralPath (Join-Path $run 'suite-input.json') -Raw | ConvertFrom-Json
            $primary = @($evaluation.checks | ForEach-Object cellId | Sort-Object -Unique)
            $replays = 0
            foreach ($cell in $saved.cells | Where-Object { $_.id -in $primary }) {
                $battle = $cell.trials[0].battleId
                if (-not (Test-Path -LiteralPath (Join-Path $run "battles/$battle.json"))) { throw 'Saved replay result is missing.' }
                & $DotnetPath $tool replay --run $run --battle $battle --detailed 1> (Join-Path $directory "$battle.json") 2> (Join-Path $directory "$battle.log")
                if ($LASTEXITCODE -ne 0) { throw "Restored replay failed for $($case.id); inspect its log for runtime/platform or result mismatch." }
                $replays++
            }
            if ($replays -ne 2) { throw 'Expected two fixed primary replays.' }
            $cases.Add([ordered]@{ Id=$case.id; GateStatus='Pass'; ArtifactHash=$case.artifactHash; ValidSavedBattles=$case.battles; ComparedCells=$case.cells; MatchedReplays=$replays })
        }
    }
    [ordered]@{
        Status='Complete'; Mode=$(if ($IntegrityOnly) { 'IntegrityOnly' } else { 'EvaluationAndReplay' })
        PackageId=$contract.id; VerifiedPayloadFiles=$index.Files.Count
        Cases=$cases.ToArray(); StatisticalSamplesAdded=0; BaselinesPromoted=$false
        Note='All input paths resolve under the restored package. Historical absolute paths inside old reports are provenance only.'
    } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding utf8
    Write-Host "Package verification complete. Results: $output"
} catch {
    @{ Status='Invalid'; Error=$_.Exception.Message } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'failure.json') -Encoding utf8
    throw
}

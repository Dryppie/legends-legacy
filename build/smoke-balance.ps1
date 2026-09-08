<#
.SYNOPSIS
    Verify the offline idle balance workflow using two identical runs.
.DESCRIPTION
    Creates a disposable same-revision reference, checks repeatability and replay,
    and evaluates the shipped draft goals. This does not promote a gameplay baseline.
    All outputs are retained in a new directory; no API or database is used.
.EXAMPLE
    ./build/smoke-balance.ps1 -OutputDirectory TestResults/balance/smoke-001
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,
    [ValidateRange(1, 100)]
    [int]$SamplesPerCell = 3,
    [int]$Seed = 1337,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$SuitePath = 'LL/tools/BalanceHarness/Fixtures/idle-reference.json',
    [string]$GoalsPath = 'LL/tools/BalanceHarness/Fixtures/idle-goals.json',
    [switch]$NoBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputDirectory, $root)
if (Test-Path -LiteralPath $output) {
    throw "Smoke output already exists: $output. Choose a new directory."
}
$null = New-Item -ItemType Directory -Path $output
$timer = [Diagnostics.Stopwatch]::StartNew()
$summary = [Collections.Generic.List[string]]::new()
$summary.Add('# Balance harness workflow smoke')
$summary.Add('')
$summary.Add("Samples per cell: $SamplesPerCell; master seed: $Seed; configuration: $Configuration.")
$summary.Add('')
$summary.Add('The reference is disposable evidence from this revision, used only to verify repeatability. It is not a reviewed gameplay baseline or a comparison with the PR base branch.')
$summary.Add('Draft balance findings are advisory. Execution, evidence, repeatability and replay errors fail this check. Small-sample inconclusive findings are expected; sample minimums are preserved.')
$summary.Add('')

$toolProject = Join-Path $root 'LL/tools/BalanceHarness/BalanceHarness.csproj'
$toolDll = Join-Path $root "LL/tools/BalanceHarness/bin/$Configuration/net10.0/BalanceHarness.dll"
$suiteFile = [IO.Path]::GetFullPath($SuitePath, $root)
$goalsFile = [IO.Path]::GetFullPath($GoalsPath, $root)
$summary.Add("Suite: $([IO.Path]::GetFileName($suiteFile)); goals: $([IO.Path]::GetFileName($goalsFile)).")
$contentRoot = Join-Path $root 'LL/src/API/API.LL'

function Invoke-Harness {
    param([string[]]$Arguments)
    & dotnet $toolDll @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "BalanceHarness $($Arguments[0]) failed with exit code $LASTEXITCODE. See retained output under $output."
    }
}

try {
    if (-not $NoBuild) {
        & dotnet build $toolProject --configuration $Configuration | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Harness build failed with exit code $LASTEXITCODE." }
    }
    if (-not (Test-Path -LiteralPath $toolDll)) { throw 'Build the harness before using -NoBuild.' }
    $goals = Get-Content -Raw -LiteralPath $goalsFile | ConvertFrom-Json
    if (@($goals.goals | Where-Object enforcement -NE 'Draft').Count -gt 0) {
        throw 'This advisory smoke workflow requires draft goals. Review its policy explicitly before introducing enforced goals; enforcement is never silently downgraded.'
    }

    $reference = Join-Path $output 'reference'
    $candidate = Join-Path $output 'candidate'
    $baseline = Join-Path $output 'repeatability-baseline.json'
    $comparisonDirectory = Join-Path $output 'comparison'
    $evaluationDirectory = Join-Path $output 'evaluation'
    foreach ($run in @($reference, $candidate)) {
        Invoke-Harness -Arguments @('suite', '--output', $run, '--suite', $suiteFile,
            '--content-root', $contentRoot, '--seed', "$Seed", '--samples', "$SamplesPerCell")
    }
    Invoke-Harness -Arguments @('baseline', 'accept', '--run', $reference, '--output', $baseline,
        '--reason', 'Disposable same-revision workflow validation reference only. No gameplay target approval or baseline promotion.')
    Invoke-Harness -Arguments @('compare', '--baseline', $baseline, '--run', $candidate, '--output', $comparisonDirectory)
    $comparison = Get-Content -Raw -LiteralPath (Join-Path $comparisonDirectory 'comparison.json') | ConvertFrom-Json
    if ($comparison.status -ne 'Complete' -or $comparison.cells.Count -eq 0 -or $comparison.evidenceChanges.Count -ne 0 -or
        @($comparison.cells | Where-Object { $_.status -ne 'Compared' -or $_.gameplayChanges -ne 0 -or $_.outcomeChanges -ne 0 }).Count -ne 0) {
        throw 'Identical runs did not produce identical complete evidence and gameplay. Inspect comparison/comparison.md.'
    }
    $summary.Add("Repeatability: $($comparison.cells.Count) cells compared; zero changed gameplay records.")

    $inputRecord = Get-Content -Raw -LiteralPath (Join-Path $candidate 'suite-input.json') | ConvertFrom-Json
    # Prefer a group encounter so the multi-enemy cohort exercises group replay too.
    $replayCell = $inputRecord.cells | Sort-Object -Property {
        if ($_.input.scenario.PSObject.Properties.Name -contains 'additionalCreatureIds') {
            @($_.input.scenario.additionalCreatureIds).Count
        } else { 0 }
    } -Descending -Stable | Select-Object -First 1
    $battleId = $replayCell.trials[0].battleId
    & dotnet $toolDll replay --run $candidate --battle $battleId --detailed > (Join-Path $output 'replay.json')
    if ($LASTEXITCODE -ne 0) { throw "Detailed replay failed with exit code $LASTEXITCODE." }
    $summary.Add("Detailed replay matched: $battleId.")

    Invoke-Harness -Arguments @('evaluate', '--run', $candidate, '--goals', $goalsFile,
        '--baseline', $baseline, '--output', $evaluationDirectory)
    $evaluation = Get-Content -Raw -LiteralPath (Join-Path $evaluationDirectory 'evaluation.json') | ConvertFrom-Json
    if ($evaluation.exitCode -ne 0 -or $evaluation.gateStatus -ne 'Advisory' -or $evaluation.issues.Count -ne 0 -or
        @($evaluation.checks | Where-Object outcome -EQ 'Invalid').Count -ne 0) {
        throw 'Goal evaluation did not produce valid advisory evidence. Inspect evaluation/evaluation.md.'
    }
    $counts = foreach ($outcome in @('Pass', 'Fail', 'Inconclusive', 'Invalid')) {
        "$outcome=$(@($evaluation.checks | Where-Object outcome -EQ $outcome).Count)"
    }
    $summary.Add("Draft assessment: $($evaluation.assessment); enforcement: $($evaluation.gateStatus); $($counts -join ', ').")
    $summary.Add('')
    $summary.Add('**Workflow verification passed. This does not certify gameplay balance.**')
}
catch {
    $summary.Add('')
    $summary.Add('**Workflow verification failed.** See the command log and any retained failure/comparison/evaluation artifacts.')
    throw
}
finally {
    $timer.Stop()
    $summary.Add('')
    $summary.Add("Elapsed seconds: $([Math]::Round($timer.Elapsed.TotalSeconds, 2)). This is an execution measurement, not a controlled performance benchmark.")
    $summary.Add('Artifacts: reference/ and candidate/ bundles, repeatability-baseline.json, comparison/, evaluation/ and replay.json when their steps complete.')
    $summaryText = $summary -join [Environment]::NewLine
    Set-Content -LiteralPath (Join-Path $output 'summary.md') -Value $summaryText -Encoding utf8
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_STEP_SUMMARY)) {
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $summaryText -Encoding utf8
    }
    Write-Host "Smoke summary: $(Join-Path $output 'summary.md')"
}

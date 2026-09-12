<#
.SYNOPSIS
    Classify changed paths for the conditional balance workflow.
.DESCRIPTION
    Uses PowerShell wildcard patterns from balance-paths.json (* includes /).
    PRs compare from the merge base; pushes compare before/after snapshots.
    Missing history conservatively selects both checks. Renames include both paths.
#>
param(
    [string]$Base,
    [string]$Head = 'HEAD',
    [switch]$PullRequest,
    [switch]$Force,
    [AllowEmptyCollection()][string[]]$ChangedPaths,
    [string]$RulesPath = (Join-Path $PSScriptRoot 'balance-paths.json')
)
$ErrorActionPreference = 'Stop'
$rules = Get-Content -Raw -LiteralPath $RulesPath | ConvertFrom-Json
$unknown = $false
if (-not $Force -and -not $PSBoundParameters.ContainsKey('ChangedPaths')) {
    if ([string]::IsNullOrWhiteSpace($Base) -or $Base -match '^0+$') {
        $unknown = $true
    } else {
        $range = if ($PullRequest) { "$Base...$Head" } else { "$Base..$Head" }
        $ChangedPaths = @(& git -c core.quotepath=false diff --name-only --no-renames $range --)
        if ($LASTEXITCODE -ne 0) { $unknown = $true }
    }
}
function Matches([string]$Path, $Patterns) {
    foreach ($pattern in $Patterns) {
        if ($Path -like $pattern) { return $true }
    }
    return $false
}
$harness = [bool]($Force -or $unknown)
$smoke = $harness
$matched = @()
foreach ($path in $ChangedPaths) {
    if (Matches $path $rules.ignore) { continue }
    $gameplay = Matches $path $rules.gameplay
    $tooling = Matches $path $rules.harness
    $workflow = Matches $path $rules.workflow
    if ($gameplay -or $tooling -or $workflow) {
        $harness = $true
        $matched += $path
    }
    if ($gameplay -or $workflow -or (Matches $path $rules.smoke)) { $smoke = $true }
}
$result = [pscustomobject]@{
    harness = $harness
    smoke = $smoke
    reason = if ($Force) { 'Manual override' } elseif ($unknown) { 'Unknown base; run conservatively' } else { 'Changed-path policy' }
    matchedPaths = $matched
}
if ($env:GITHUB_OUTPUT) {
    "harness=$($harness.ToString().ToLowerInvariant())" | Add-Content $env:GITHUB_OUTPUT
    "smoke=$($smoke.ToString().ToLowerInvariant())" | Add-Content $env:GITHUB_OUTPUT
}
if ($env:GITHUB_STEP_SUMMARY) {
    @("Balance checks: harness=$harness; smoke=$smoke. $($result.reason).", '', ($matched -join "`n")) | Add-Content $env:GITHUB_STEP_SUMMARY
}
$result

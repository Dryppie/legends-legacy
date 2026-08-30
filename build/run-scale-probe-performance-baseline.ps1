[CmdletBinding()]
param(
    [string] $OutputRoot,

    [ValidateRange(1, 10)]
    [int] $Repetitions = 1
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputRoot))
{
    $OutputRoot = Join-Path $repositoryRoot 'balance-output\schema23-performance-baseline'
}

$balanceScript = Join-Path $PSScriptRoot 'run-balance.ps1'
$baselineArguments = @(
    '--output', $OutputRoot
    '--seed', '8471'
    '--build-count', '5'
    '--optimizer-population', '6'
    '--optimizer-generations', '1'
    '--optimizer-elites', '2'
    '--optimizer-random', '0.17'
    '--optimizer-retained', '3'
    '--representative-count', '10'
    '--tower-simulations', '1'
    '--calibration-iterations', '1'
    '--encounter-candidate-simulations', '1'
    '--encounter-retained', '2'
    '--elite-search-only'
    '--elite-restarts', '2'
    '--elite-population', '4'
    '--elite-generations', '1'
    '--elite-max-generations', '1'
    '--elite-elites', '2'
    '--elite-finalists', '1'
    '--elite-local-swap-depth', '1'
    '--elite-two-swap-limit', '1'
    '--elite-restart-refinement', '1'
    '--elite-restart-seeds', '1'
    '--elite-restart-two-swap-limit', '1'
    '--elite-finalist-refinement', '0'
    '--elite-holdout-seeds', '2'
    '--elite-simulations', '1'
    '--elite-party-genomes', '1'
    '--validation-seeds', '2'
    '--validation-simulations', '1'
    '--validation-probe-simulations', '1'
    '--meta-simulator-battles', '1'
    '--capability-seeds', '1'
    '--party-family-samples', '3'
    '--party-family-simulations', '1'
    '--scale-probes'
    '--scale-probe-parties', '3'
    '--scale-probe-simulations', '5'
    '--scale-probe-max-ms-per-trial', '15'
    '--scale-probe-max-allocated-mb-per-trial', '10'
    '--scale-probe-min-ticks-per-second', '30000'
    '--scale-probe-max-peak-memory-mb', '192'
)

for ($run = 1; $run -le $Repetitions; $run++)
{
    Write-Host "Running scale-probe performance baseline $run/$Repetitions..."
    & $balanceScript @baselineArguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "Scale-probe performance baseline failed with exit code $LASTEXITCODE."
    }
}


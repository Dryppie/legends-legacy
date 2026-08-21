#Requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [string]$OutputDirectory,

    [string]$BaselinePath,

    [string[]]$EncounterId,

    [string[]]$GearEnvelopeId,

    [string[]]$BuildFamilyId,

    [string[]]$PartyCompositionId,

    [string[]]$EssenceEnvelopeId,

    [string[]]$CohortId,

    [string[]]$StaggerProfileId,

    [ValidateRange(0, 1000)]
    [int]$Samples = 0,

    [switch]$StaggerOnly,

    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $repositoryRoot 'LL\tools\BalanceCalibration\BalanceCalibration.csproj'
$arguments = [Collections.Generic.List[string]]::new()

if (![string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $arguments.Add('--output')
    $arguments.Add([IO.Path]::GetFullPath($OutputDirectory))
}
if (![string]::IsNullOrWhiteSpace($BaselinePath)) {
    $arguments.Add('--baseline')
    $arguments.Add([IO.Path]::GetFullPath($BaselinePath))
}
foreach ($id in $EncounterId) {
    $arguments.Add('--encounter')
    $arguments.Add($id)
}
foreach ($id in $GearEnvelopeId) {
    $arguments.Add('--gear')
    $arguments.Add($id)
}
foreach ($id in $BuildFamilyId) {
    $arguments.Add('--build')
    $arguments.Add($id)
}
foreach ($id in $PartyCompositionId) {
    $arguments.Add('--composition')
    $arguments.Add($id)
}
foreach ($id in $EssenceEnvelopeId) {
    $arguments.Add('--essence')
    $arguments.Add($id)
}
foreach ($id in $CohortId) {
    $arguments.Add('--cohort')
    $arguments.Add($id)
}
foreach ($id in $StaggerProfileId) {
    $arguments.Add('--stagger-profile')
    $arguments.Add($id)
}
if ($StaggerOnly) {
    $arguments.Add('--stagger-only')
}
if ($Samples -gt 0) {
    $arguments.Add('--samples')
    $arguments.Add($Samples.ToString([Globalization.CultureInfo]::InvariantCulture))
}

if (!$NoBuild) {
    & dotnet build $project --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Encounter calibration tool build failed with exit code $LASTEXITCODE."
    }
}

& dotnet run `
    --project $project `
    --configuration $Configuration `
    --no-build `
    --no-restore `
    -- `
    @arguments

if ($LASTEXITCODE -ne 0) {
    throw "Encounter calibration failed with exit code $LASTEXITCODE."
}

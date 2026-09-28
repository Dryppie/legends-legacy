<#
.SYNOPSIS
Creates an editable equipment balance candidate without changing the live release.
.EXAMPLE
./build/new-equipment-balance.ps1 -Version 4 -SourceVersion 3
#>
param(
    [Parameter(Mandatory)][ValidateRange(3, 2147483647)][int]$Version,
    [ValidateRange(3, 2147483647)][int]$SourceVersion = 3,
    [string]$EquipmentDirectory = (Join-Path $PSScriptRoot '../LL/src/API/API.LL/Data/equipment')
)
$ErrorActionPreference = 'Stop'
$equipmentRoot = (Resolve-Path -LiteralPath $EquipmentDirectory).Path
$registryPath = Join-Path $equipmentRoot 'equipment-releases.json'
$registry = Get-Content -LiteralPath $registryPath -Raw | ConvertFrom-Json -AsHashtable
if ($Version -le $SourceVersion -or $registry.ContainsKey([string]$Version)) {
    throw 'Choose an unused release number newer than the source.'
}
$source = $registry[[string]$SourceVersion]
if ($null -eq $source) { throw 'The source release is not registered.' }
$candidate = [ordered]@{}
$contents = [ordered]@{}
foreach ($kind in @('starters', 'styles', 'sets', 'named')) {
    $sourceFile = [string]$source[$kind]
    if ([string]::IsNullOrWhiteSpace($sourceFile) -or [IO.Path]::GetFileName($sourceFile) -ne $sourceFile) {
        throw 'Release content must be a filename within the equipment directory.'
    }
    $candidate[$kind] = "equipment-$kind.v$Version.json"
    $targetPath = Join-Path $equipmentRoot $candidate[$kind]
    if (Test-Path -LiteralPath $targetPath) { throw "Candidate file already exists: $targetPath" }
    $contents[$kind] = [IO.File]::ReadAllText((Join-Path $equipmentRoot $sourceFile))
}
$starter = $contents['starters'] | ConvertFrom-Json -AsHashtable
if ($starter.balanceVersion -ne $SourceVersion -or $null -eq $starter.balance.attributeCosts) {
    throw 'The source must have matching version metadata and explicit attribute costs.'
}
$starter.balanceVersion = $Version
$contents['starters'] = ($starter | ConvertTo-Json -Depth 100) + [Environment]::NewLine
foreach ($kind in $candidate.Keys) {
    [IO.File]::WriteAllText((Join-Path $equipmentRoot $candidate[$kind]), $contents[$kind])
}
$registry[[string]$Version] = $candidate
[IO.File]::WriteAllText($registryPath, ($registry | ConvertTo-Json -Depth 100) + [Environment]::NewLine)
Write-Output "Created equipment release $Version from $SourceVersion. Edit its prices, weights and shares, then compare and rehearse it. Live configuration was not changed."

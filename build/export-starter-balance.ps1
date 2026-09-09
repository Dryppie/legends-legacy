#requires -Version 7.4
<#
.SYNOPSIS
    Package the accepted starter evidence without changing its manifests or runs.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ArchivePath,
    [string]$SourceRoot = (Split-Path -Parent $PSScriptRoot)
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not ('LegendsLegacy.Tools.BalanceEvidenceArchive' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'BalanceEvidenceArchive.cs')
}
$root = [IO.Path]::GetFullPath($SourceRoot, $PWD.Path)
$archive = [IO.Path]::GetFullPath($ArchivePath, $PWD.Path)
if ([IO.Path]::GetExtension($archive) -ne '.zip') { throw 'Use a .zip archive path.' }
foreach ($path in @($archive, "$archive.sha256", "$archive.receipt.json")) {
    if (Test-Path -LiteralPath $path) { throw "Output already exists: $path" }
}
$contractText = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'starter-balance-package-v1.json') -Raw
$contract = $contractText | ConvertFrom-Json
function Resolve([string]$relative) { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Resolve($root, $relative) }
function Hash([string]$path) { [LegendsLegacy.Tools.BalanceEvidenceArchive]::Hash($path) }
foreach ($anchor in $contract.anchors.PSObject.Properties) {
    if ((Hash (Resolve $anchor.Name)) -ne $anchor.Value) { throw "Reviewed evidence changed: $($anchor.Name)" }
}
foreach ($relative in $contract.includes) {
    $path = Resolve $relative
    if (-not (Test-Path -LiteralPath $path)) { throw "Required evidence is missing: $relative" }
    if ($relative -like '*/retained-builds/*') {
        foreach ($manifestName in @('retention.json', 'correction-retention.json')) {
            $manifestPath = Join-Path $path $manifestName
            if (-not (Test-Path -LiteralPath $manifestPath)) { continue }
            $retention = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            foreach ($file in $retention.Files) {
                $retained = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Resolve($path, $file.Path.Replace('\', '/'))
                if ((Hash $retained) -ne $file.Hash) { throw "Retained executable/provenance changed: $relative/$($file.Path)" }
            }
        }
    }
}
# Captured appsettings files must contain only the reviewed combat settings.
$threatKeys = @('Enabled','AttentionExponent','MinimumAttentionWeight','MaximumAttentionWeight',
    'ThreatHalfLifeSeconds','BasicAttackThreatValue','ProtectiveSelfThreatPerSecond','ProtectiveAllyThreatPerSecond',
    'RetaliationThreatPerSecond','SupportAllyThreatPerSecond','HardControlThreatPerSecond','SoftControlThreatPerSecond',
    'DamageThreatPerSecond','SelfSustainThreatPerSecond','UtilityThreatPerSecond','MarkThreatBonus',
    'CoverBudgetMaxHealthFraction','DefaultSummonThreatMultiplier')
$settingsCount = 0
foreach ($relative in $contract.includes) {
    $path = Resolve $relative
    if (-not [IO.Directory]::Exists($path)) { continue }
    foreach ($file in [IO.Directory]::EnumerateFiles($path, 'appsettings*.json', [IO.SearchOption]::AllDirectories)) {
        $settings = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
        if (@($settings.PSObject.Properties.Name | Where-Object { $_ -ne 'Combat' }).Count -or
            @($settings.Combat.PSObject.Properties.Name | Where-Object { $_ -notin @('ThreatAndTanking','IdleProgression') }).Count -or
            @($settings.Combat.IdleProgression.PSObject.Properties.Name | Where-Object { $_ -ne 'EncounterCadenceSeconds' }).Count -or
            @($settings.Combat.ThreatAndTanking.PSObject.Properties.Name | Where-Object { $_ -notin $threatKeys }).Count) {
            throw 'Captured settings contain unreviewed fields; do not package application configuration.'
        }
        $settingsCount++
    }
}
$null = New-Item -ItemType Directory -Path (Split-Path $archive) -Force
Write-Host "Packaging $($contract.id); all reviewed anchors and retained manifests match."
$index = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Create($root, $archive, [string[]]$contract.includes, $contractText)
$hash = Hash $archive
"$hash  $([IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath "$archive.sha256" -Encoding ascii
[ordered]@{
    Id = $contract.id; CreatedAt = [DateTimeOffset]::UtcNow.ToString('o')
    Archive = [IO.Path]::GetFileName($archive); Sha256 = $hash
    ArchiveBytes = (Get-Item -LiteralPath $archive).Length
    PayloadFiles = $index.Files.Count; PayloadBytes = ($index.Files | Measure-Object Length -Sum).Sum
    ReviewedAnchors = $contract.anchors.PSObject.Properties.Name.Count; CapturedSettingsFiles = $settingsCount
    SourceFilesRechecked = $true; BaselinesPromoted = $false; StatisticalSamplesAdded = 0
    DurableCopyVerified = $false
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$archive.receipt.json" -Encoding utf8
Write-Host "Archive complete: $archive"
Write-Host "SHA-256: $hash"
Write-Host 'Retain the archive and checksum together. A local archive is not an off-device backup.'

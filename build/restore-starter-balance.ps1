#requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ArchivePath,
    [Parameter(Mandatory)][string]$ExpectedSha256,
    [Parameter(Mandatory)][string]$OutputDirectory
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (-not ('LegendsLegacy.Tools.BalanceEvidenceArchive' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot 'BalanceEvidenceArchive.cs')
}
$index = [LegendsLegacy.Tools.BalanceEvidenceArchive]::Restore(
    [IO.Path]::GetFullPath($ArchivePath, $PWD.Path), $ExpectedSha256, [IO.Path]::GetFullPath($OutputDirectory, $PWD.Path))
Write-Host "Restored $($index.Files.Count) verified payload files. No executable was run."
Write-Host 'Use verify-starter-balance-package.ps1 with a separate output directory to evaluate and replay the restored evidence.'

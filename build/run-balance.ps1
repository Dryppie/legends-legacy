[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $BalanceArguments = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'LL\tools\LegendsLegacy.Balance\LegendsLegacy.Balance.csproj'
$arguments = @(
    'run'
    '--project'
    $projectPath
    '--configuration'
    'Release'
    '--'
    '--full'
) + $BalanceArguments

& dotnet @arguments
if ($LASTEXITCODE -ne 0)
{
    throw "The automated balance pipeline failed with exit code $LASTEXITCODE."
}

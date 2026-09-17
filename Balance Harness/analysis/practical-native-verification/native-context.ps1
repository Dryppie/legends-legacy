param([Parameter(Mandatory)][string]$Runtime, [Parameter(Mandatory)][string]$ContentRoot,
    [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $Runtime 'BalanceHarness.dll'))
$execution = [BalanceHarness.ExecutionIdentity]::Current()
$flags = [System.Reflection.BindingFlags]'Static,NonPublic'
$settings = [BalanceHarness.TowerBundle].GetMethod('ReadSettings', $flags).Invoke($null, [object[]]@($ContentRoot))
[BalanceHarness.HarnessJson]::WriteNew[object]($Output, @{
    execution = $execution
    executionHash = [BalanceHarness.HarnessJson]::Hash[object]($execution)
    settings = $settings
    settingsHash = [BalanceHarness.HarnessJson]::Hash[object]($settings)
    newValues = 0
    newFights = 0
})

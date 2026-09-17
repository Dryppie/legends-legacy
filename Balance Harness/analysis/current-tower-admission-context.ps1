param([Parameter(Mandatory)][string]$Runtime, [Parameter(Mandatory)][string]$ContentRoot,
    [Parameter(Mandatory)][string]$Output, [string]$DesignRoot)
$ErrorActionPreference = 'Stop'
[void][System.Reflection.Assembly]::LoadFrom((Join-Path $Runtime 'BalanceHarness.dll'))
$execution = [BalanceHarness.ExecutionIdentity]::Current()
$executionHash = [BalanceHarness.HarnessJson]::Hash[object]($execution)
$flags = [System.Reflection.BindingFlags]'Static,NonPublic'
$settings = [BalanceHarness.TowerBundle].GetMethod('ReadSettings', $flags).Invoke($null, [object[]]@($ContentRoot))
$combat = [BalanceHarness.RunBundle].GetMethod('ReadCombatSettings', $flags).Invoke($null, [object[]]@($ContentRoot))
$result = @{
    execution = $execution; executionHash = $executionHash; settings = $settings
    settingsHash = [BalanceHarness.HarnessJson]::Hash[object]($settings)
    idleCadenceSeconds = $combat.Item2; nativePreparations = 0; fights = 0; newValues = 0
}
if ($DesignRoot) {
    # A managed no-op remains callable when the async loader continues on a worker thread.
    $noOpMethod = [System.Reflection.Emit.DynamicMethod]::new('AdmissionReadinessCheckpoint', [void], [Type[]]@())
    $noOpMethod.GetILGenerator().Emit([System.Reflection.Emit.OpCodes]::Ret)
    $check = $noOpMethod.CreateDelegate([Action])
    $adapter = [BalanceHarness.TowerCurrentFamilyAdmission]
    $request = [BalanceHarness.TowerCurrentAdmissionRequest]::new(
        [BalanceHarness.TowerCurrentFamilyAdmission]::Version, $DesignRoot, $ContentRoot, $executionHash, 1500, 2013265920)
    $summary = [BalanceHarness.HarnessJson]::Read[System.Text.Json.JsonElement]((Join-Path $DesignRoot 'inventory-summary.json'))
    $adapter.GetMethod('CheckDesign', $flags).Invoke($null, [object[]]@($DesignRoot, 'files.json', $check))
    $adapter.GetMethod('CheckRuntime', $flags).Invoke($null, [object[]]@($request, $summary, $check))
    $stop = [System.Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(110))
    try {
        $task = $adapter.GetMethod('Load', $flags).Invoke($null, [object[]]@($DesignRoot, $check, $stop.Token))
        $inventory = $task.GetAwaiter().GetResult()
        $controls = @()
        foreach ($cell in $inventory.Cells) {
            if ($cell.RequiredControlIds.Count -eq 0) { continue }
            $scenario = $adapter.GetMethod('Scenario', $flags).Invoke($null, [object[]]@($inventory.Template, $cell))
            $controls += @{ inputKey = $cell.InputKey; requiredControlIds = $cell.RequiredControlIds
                scenarioHash = [BalanceHarness.HarnessJson]::Hash[object]($scenario) }
        }
        # JIT resolves these entry paths without invoking CreateInput, PrepareAsync, Scan or Run.
        $compiled = @()
        $methods = @(
            [BalanceHarness.TowerBattleRunner].GetMethod('CreateInput'),
            [BalanceHarness.TowerBattleRunner].GetMethod('PrepareAsync'),
            $adapter.GetMethod('Run'), $adapter.GetMethod('Scan', $flags)
        )
        foreach ($method in $methods) {
            [System.Runtime.CompilerServices.RuntimeHelpers]::PrepareMethod($method.MethodHandle)
            $compiled += "$($method.DeclaringType.FullName).$($method.Name)"
            $async = @($method.GetCustomAttributes([System.Runtime.CompilerServices.AsyncStateMachineAttribute], $false))
            if ($async.Count) {
                $move = $async[0].StateMachineType.GetMethod('MoveNext', [System.Reflection.BindingFlags]'Instance,NonPublic')
                [System.Runtime.CompilerServices.RuntimeHelpers]::PrepareMethod($move.MethodHandle)
                $compiled += "$($move.DeclaringType.FullName).MoveNext"
            }
        }
        $result.inventory = @{ cells = $inventory.Cells.Count; occurrences = $inventory.Entries.Count
            forcedInputCells = @($inventory.Cells | Where-Object { $_.AnchorReasons.Count -gt 0 }).Count
            incompatibleOccurrences = @($inventory.Entries | Where-Object { $_.Classification -eq 'Incompatible' }).Count
            controls = $controls; nativeAdmission = 'Pending'; readyForFamilyFreeze = $false }
        $result.jitResolvedMethods = $compiled
        $result.status = 'RuntimeAndStaticContractCompatible'
    } finally { $stop.Dispose() }
}
[BalanceHarness.HarnessJson]::WriteNew[object]($Output, $result)

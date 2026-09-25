param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$RepositoryRoot
)
$ErrorActionPreference = 'Stop'
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $Package 'runtime/BalanceHarness.dll'))
$flags = [Reflection.BindingFlags]'Static,Instance,Public,NonPublic,DeclaredOnly'
$token = [Threading.CancellationToken]::None
function Put($Name, $Value) { [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package $Name), $Value) }
function Hash($Value) { [BalanceHarness.HarnessJson]::Hash[object]($Value) }
$traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
$trace = $traceType.GetConstructor($flags, $null, [Type[]]@([Action[bool]]), $null).Invoke(
    [object[]]@([Action[bool]]{ param($completed) throw 'Runtime qualification forbids combat.' }))
$guard = $trace.Activate()
try {
    $hashes = [Collections.Generic.List[string]]::new()
    $catalogues = [Collections.Generic.List[string]]::new()
    for ($number = 1; $number -le 12; $number++) {
        $directory = Join-Path $Package ('placement-preview/root-{0:D2}' -f $number)
        $request = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExportRequest]((Join-Path $directory 'request.json'))
        $expected = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExport]((Join-Path $directory 'batches.json'))
        $replay = [BalanceHarness.TowerProposalPolicies]::Export($request, $token)
        if ((Hash $replay) -ne (Hash $expected)) { throw "Placement export changed at root $number." }
        $arm = @($replay.Arms | Where-Object Name -eq 'benchmark-subgroup-loadout-placement-v6')
        if ($arm.Count -ne 1 -or $arm[0].LoadoutPlacementCatalogue.Recipes.Count -ne 238) { throw 'Incomplete placement catalogue.' }
        $hashes.Add((Hash $replay)); $catalogues.Add((Hash $arm[0].LoadoutPlacementCatalogue))
    }
    $request = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExportRequest]((Join-Path $Package 'placement-preview/root-01/request.json'))
    $saved = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExport]((Join-Path $Package 'placement-preview/root-01/batches.json'))
    $catalogue = @($saved.Arms | Where-Object Name -eq 'benchmark-subgroup-loadout-placement-v6')[0].LoadoutPlacementCatalogue
    $placement = $assembly.GetType('BalanceHarness.TowerLoadoutPlacement', $true)
    $scenarioMethod = $placement.GetMethod('Scenario', $flags)
    $probes = [BalanceHarness.HarnessJson]::Read[System.Text.Json.JsonElement]((Join-Path $Package 'probe-scenarios.json'))
    $ordinal = 568
    foreach ($recipe in $catalogue.Recipes) {
        $arguments = [object[]]::new(3)
        $arguments.SetValue([BalanceHarness.TowerBossDiscoveryDefinition]$request.Context.Scope, 0)
        $arguments.SetValue([string]$request.Context.BenchmarkReferenceId, 1)
        $arguments.SetValue([BalanceHarness.PartyChoice]$recipe.Party, 2)
        $scenario = $scenarioMethod.Invoke($null, $arguments)
        $row = $probes[$ordinal]; $ordinal++
        $probe = [System.Text.Json.JsonSerializer]::Deserialize[BalanceHarness.TowerScenario]($row.GetProperty('scenario').GetRawText(), [BalanceHarness.HarnessJson]::Options)
        # Probe seeds are the only change to the seed-free native catalogue scenario.
        $node = [System.Text.Json.Nodes.JsonNode]::Parse([System.Text.Json.JsonSerializer]::Serialize($probe, [BalanceHarness.HarnessJson]::Options))
        $node['seeds'] = [System.Text.Json.Nodes.JsonArray]::new()
        $seedFree = [System.Text.Json.JsonSerializer]::Deserialize[BalanceHarness.TowerScenario]($node.ToJsonString(), [BalanceHarness.HarnessJson]::Options)
        if ((Hash $scenario) -ne (Hash $seedFree)) { throw 'Preparation probe differs from native catalogue scenario.' }
    }
    $methods = [Collections.Generic.List[string]]::new()
    foreach ($type in @($placement, $assembly.GetType('BalanceHarness.TowerProposalComparison', $true))) {
        foreach ($method in $type.GetMethods($flags)) {
            if ($method.ContainsGenericParameters -or $method.IsAbstract -or $null -eq $method.GetMethodBody()) { continue }
            [Runtime.CompilerServices.RuntimeHelpers]::PrepareMethod($method.MethodHandle)
            $methods.Add("$($type.FullName).$($method.Name)")
        }
    }
    $old = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalComparisonPlan]((Join-Path $Package 'prospective-plan.json'))
    # Bind the producing execution only; retain the entire saved physical context.
    $node = [System.Text.Json.Nodes.JsonNode]::Parse([System.Text.Json.JsonSerializer]::Serialize($request.Context, [BalanceHarness.HarnessJson]::Options))
    $node['scope']['executionHash'] = [System.Text.Json.Nodes.JsonValue]::Create([string](Hash ([BalanceHarness.ExecutionIdentity]::Current())))
    $context = [System.Text.Json.JsonSerializer]::Deserialize[BalanceHarness.TowerProposalContext]($node.ToJsonString(), [BalanceHarness.HarnessJson]::Options)
    $plan = [BalanceHarness.TowerProposalComparison]::CreateLoadoutPlacementPlan($context, $old.Control)
    Put 'runtime-context.json' $context
    Put 'runtime-plan.json' $plan
    $providers = $assembly.GetType('BalanceHarness.TowerContentProviders', $true)
    if ($providers.GetProperty('SupportsAccounting', $flags).GetValue($null)) { throw 'Expected the unchanged captured provider ABI.' }
    $settings = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerSettings]((Join-Path $Package 'settings.json'))
    $rejected = $false
    $work = [Activator]::CreateInstance($assembly.GetType('BalanceHarness.TowerWorkAccounting', $true), $true)
    $accounting = $work.Activate()
    try {
        [void][BalanceHarness.OfflineContent]::new((Join-Path $Package 'content'), $settings.Threat)
    } catch {
        $providerFailure = $_.Exception
        while ($null -ne $providerFailure.InnerException) { $providerFailure = $providerFailure.InnerException }
        if ($providerFailure -isnot [NotSupportedException] -or -not $providerFailure.Message.StartsWith('Captured content providers do not support content-read accounting.')) { throw }
        $rejected = $true
    } finally { $accounting.Dispose() }
    if (-not $rejected -or $work.Snapshot().Count -ne 0) { throw 'Captured accounting must fail before provider IO.' }
    Put 'placement-qualification.json' @{ status='PlacementRuntimeReplayedNoAdmission'; roots=12; replayedArms=36
        placementRecipes=238; nativeScenarioChecks=238; exportHashes=$hashes.ToArray(); catalogueHashes=$catalogues.ToArray()
        jitResolvedMethods=$methods.ToArray(); capturedAccountingBlockedBeforeIO=$true; fights=0; newValues=0; scientificAdmission=$false }
} finally { $guard.Dispose() }

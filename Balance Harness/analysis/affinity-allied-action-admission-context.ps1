param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][ValidateSet('baseline','candidate','bind','retention')][string]$Mode
)
$ErrorActionPreference = 'Stop'
$runtime = Join-Path $Package $(if ($Mode -eq 'baseline') { 'baseline-runtime' } else { 'runtime' })
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $runtime 'BalanceHarness.dll'))
$flags = [Reflection.BindingFlags]'Static,Instance,Public,NonPublic,DeclaredOnly'
$token = [Threading.CancellationToken]::None
function Put($Name, $Value) {
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package $Name), $Value)
}
function Hash($Value) { [BalanceHarness.HarnessJson]::Hash[object]($Value) }

# This callback is active across materialization and preparation, including async work.
$traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
$trace = $traceType.GetConstructor($flags, $null, [Type[]]@([Action[bool]]), $null).Invoke(
    [object[]]@([Action[bool]]{ param($completed) throw 'Admission forbids combat.' }))
$guard = $trace.Activate()
try {
    if ($Mode -eq 'retention') {
        # Exercise the production copier against the exact admission manifest,
        # including producing symbols, without entering RunOwned or Reserve.
        $manifest = Join-Path $Package 'runtime.json'
        $output = Join-Path $Package 'retention-check'
        [void][IO.Directory]::CreateDirectory($output)
        $maximum = [long](64*1048576)
        $method = $assembly.GetType('BalanceHarness.TowerProposalStudy', $true).GetMethod('RetainRuntime', $flags)
        # Reflection does not unwrap command-output PSObjects in object arrays.
        # SetValue crosses a typed CLR boundary before MethodInfo.Invoke.
        $arguments = [object[]]::new(4)
        $arguments.SetValue([string]$manifest, 0)
        $arguments.SetValue([string]$output, 1)
        $arguments.SetValue([long]$maximum, 2)
        $arguments.SetValue([Threading.CancellationToken]$token, 3)
        $files = $method.Invoke($null, $arguments)
        $retained = Join-Path $output 'executable'
        $bytes = [long]0
        foreach ($name in $files.Keys) { $bytes += [IO.FileInfo]::new((Join-Path $retained $name)).Length }
        Put 'retention-files.json' $files
        Put 'retention-qualification.json' @{ status='RuntimeRetentionVerifiedNoReservation'
            runtimeManifestSha256=[BalanceHarness.HarnessJson]::FileHash($manifest)
            harnessSha256=$files['BalanceHarness.dll']; symbolsSha256=$files['BalanceHarness.pdb']
            maximumBytes=$maximum; retainedBytes=$bytes; runtimeFiles=$files.Count; fights=0; newValues=0 }
        exit 0
    }
    if ($Mode -eq 'bind') {
        $context = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalContext]((Join-Path $Package 'context.json'))
        $old = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalComparisonPlan]((Join-Path $Package 'prospective-plan.json'))
        $plan = [BalanceHarness.TowerProposalComparison]::CreateAlliedActionPlan($context, $old.Candidate)
        Put 'plan.json' $plan
        Put 'plan-binding.json' @{ planHash=(Hash $plan); originalPlanHash=(Hash $old); contextHash=(Hash $context)
            executionHash=$context.Scope.ExecutionHash; fights=0; newValues=0 }
        exit 0
    }

    $clock = [Diagnostics.Stopwatch]::StartNew()
    $settings = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerSettings]((Join-Path $Package 'settings.json'))
    $contentRoot = Join-Path $Package 'content'
    $runner = [BalanceHarness.TowerBattleRunner]::new($contentRoot, [BalanceHarness.OfflineContent]::new($contentRoot, $settings.Threat))
    $probes = [BalanceHarness.HarnessJson]::Read[System.Text.Json.JsonElement]((Join-Path $Package 'probe-scenarios.json'))
    $rows = [Collections.Generic.List[object]]::new()
    $materialization = [Collections.Generic.List[double]]::new()
    $preparation = [Collections.Generic.List[double]]::new()
    foreach ($row in $probes.EnumerateArray()) {
        $scenario = [System.Text.Json.JsonSerializer]::Deserialize[BalanceHarness.TowerScenario](
            $row.GetProperty('scenario').GetRawText(), [BalanceHarness.HarnessJson]::Options)
        foreach ($value in $row.GetProperty('seeds').EnumerateArray()) {
            $seed = $value.GetInt32()
            $watch = [Diagnostics.Stopwatch]::StartNew()
            $input = $runner.CreateInput($scenario, $seed, $settings.Threat, $settings.CheckpointIntervalTicks)
            $inputHash = Hash $input
            $materialization.Add($watch.Elapsed.TotalSeconds)
            $watch.Restart()
            $prepared = $runner.PrepareAsync($input, $token).GetAwaiter().GetResult()
            $participants = [BalanceHarness.IdleBattleRunner]::DescribeParticipants($prepared)
            $preparation.Add($watch.Elapsed.TotalSeconds)
            $rows.Add(@{ ordinal=$rows.Count+1; seed=$seed; scenarioHash=(Hash $scenario)
                inputHash=$inputHash; participantsHash=(Hash $participants) })
        }
    }
    Put ($Mode+'-projections.json') $rows.ToArray()
    $details = @{ mode=$Mode; execution=[BalanceHarness.ExecutionIdentity]::Current()
        executionHash=(Hash ([BalanceHarness.ExecutionIdentity]::Current())); settingsHash=(Hash $settings)
        materializations=$rows.Count; nativePreparations=$rows.Count; fights=0; newValues=0
        materializationSeconds=$materialization.ToArray(); preparationSeconds=$preparation.ToArray()
        measuredSeconds=$clock.Elapsed.TotalSeconds; timings=$trace.Snapshot() }
    if ($Mode -eq 'candidate') {
        # All source documents, including generated ones, must match the producing PDB.
        $pdbStream = [IO.File]::OpenRead((Join-Path $runtime 'BalanceHarness.pdb'))
        $dllStream = [IO.File]::OpenRead((Join-Path $runtime 'BalanceHarness.dll'))
        $provider = [Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
        $pe = [Reflection.PortableExecutable.PEReader]::new($dllStream)
        try {
            $reader = $provider.GetMetadataReader()
            $entries = @($pe.ReadDebugDirectory() | Where-Object { $_.Type.ToString() -eq 'CodeView' })
            if ($entries.Count -ne 1) { throw 'Expected one producing CodeView entry.' }
            $symbolGuid = [Guid]::new([byte[]]$reader.DebugMetadataHeader.Id[0..15])
            if ($symbolGuid -ne $pe.ReadCodeViewDebugDirectoryData($entries[0]).Guid) { throw 'Changed producing symbols.' }
            $sourceFiles = [Collections.Generic.SortedDictionary[string,string]]::new([StringComparer]::Ordinal)
            $prefix = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
            foreach ($handle in $reader.Documents) {
                $document = $reader.GetDocument($handle)
                $source = [IO.Path]::GetFullPath($reader.GetString($document.Name))
                if (-not $source.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Source outside repository.' }
                if ($reader.GetGuid($document.HashAlgorithm) -ne [Guid]'8829d00f-11b8-4213-878b-770e8597ac16') { throw 'Expected SHA-256 source checksum.' }
                $expected = [Convert]::ToHexStringLower($reader.GetBlobBytes($document.Hash))
                if ([BalanceHarness.HarnessJson]::FileHash($source) -ne $expected) { throw "Producing source changed: $source" }
                $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $source).Replace('\','/')
                $target = Join-Path (Join-Path $Package 'source') $relative
                [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
                [IO.File]::Copy($source, $target, $false)
                $sourceFiles.Add($relative, $expected)
            }
            Put 'compiled-source-files.json' $sourceFiles
            $details.producingSymbolGuid = $symbolGuid.ToString()
            $details.compiledSourceDocuments = $sourceFiles.Count
        } finally { $provider.Dispose(); $pe.Dispose(); $dllStream.Dispose(); $pdbStream.Dispose() }

        $queue = [Collections.Generic.Queue[Type]]::new()
        foreach ($name in @('TowerAlliedActionProtection','TowerAffinityCreation','TowerBenchmarkValidation','TowerAdaptiveRacingGenerator','TowerBatchRacing','TowerProposalStudy','TowerProposalPolicies','TowerProposalComparison','TowerProposalRacingNative',
            'TowerBattleRunner','TowerLoadoutArchive','TowerBossInventory','TowerBossPartyGenerator','TowerRefinementComparisonLaunch')) {
            $queue.Enqueue($assembly.GetType('BalanceHarness.'+$name, $true))
        }
        $methods = [Collections.Generic.List[string]]::new()
        while ($queue.Count -gt 0) {
            $type = $queue.Dequeue()
            foreach ($nested in $type.GetNestedTypes([Reflection.BindingFlags]'Public,NonPublic')) { $queue.Enqueue($nested) }
            foreach ($method in $type.GetMethods($flags)) {
                if ($method.ContainsGenericParameters -or $method.IsAbstract -or $null -eq $method.GetMethodBody()) { continue }
                [Runtime.CompilerServices.RuntimeHelpers]::PrepareMethod($method.MethodHandle)
                $methods.Add("$($type.FullName).$($method.Name)#$($method.MetadataToken)")
            }
        }
        $details.jitResolvedMethods = $methods.ToArray()
        $preview = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExportRequest]((Join-Path $Package 'preview-request.json'))
        $saved = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExport]((Join-Path $Package 'preview-batches.json'))
        $inventory = [BalanceHarness.TowerBossInventory]::Create($contentRoot, $settings.Threat)
        if ((Hash $inventory) -ne (Hash $preview.Context.DamageAffinityInventory)) { throw 'Captured typed inventory changed.' }
        $replayed = [BalanceHarness.TowerProposalPolicies]::Export($preview, $token)
        if ((Hash $replayed) -ne (Hash $saved)) { throw 'Historical first-wave export changed.' }
        $details.inventoryHash = Hash $inventory
        $details.replayedExportHash = Hash $replayed
        $details.replayedArms = $replayed.Arms.Count
        $preserving = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExportRequest]((Join-Path $Package 'preserving-request.json'))
        $preserved = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExport]((Join-Path $Package 'preserving-batches.json'))
        if ((Hash $inventory) -ne (Hash $preserving.Context.DamageAffinityInventory)) { throw 'Preserving typed inventory changed.' }
        $preservingReplay = [BalanceHarness.TowerProposalPolicies]::Export($preserving, $token)
        if ((Hash $preservingReplay) -ne (Hash $preserved)) { throw 'Historical preserving two-wave export changed.' }
        $details.preservingReplayedExportHash = Hash $preservingReplay
        $details.preservingReplayedArms = $preservingReplay.Arms.Count
        $alliedHashes = [Collections.Generic.List[string]]::new()
        $alliedArms = 0
        for ($number = 1; $number -le 12; $number++) {
            $directory = Join-Path $Package ('allied-preview/root-{0:D2}' -f $number)
            $request = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExportRequest]((Join-Path $directory 'request.json'))
            $expected = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerProposalExport]((Join-Path $directory 'batches.json'))
            if ((Hash $inventory) -ne (Hash $request.Context.DamageAffinityInventory)) { throw 'Allied-action typed inventory changed.' }
            $replay = [BalanceHarness.TowerProposalPolicies]::Export($request, $token)
            if ((Hash $replay) -ne (Hash $expected)) { throw "Historical allied-action two-wave export changed at root $number." }
            $alliedHashes.Add((Hash $replay)); $alliedArms += $replay.Arms.Count
        }
        $details.alliedReplayedRoots = $alliedHashes.Count
        $details.alliedReplayedArms = $alliedArms
        $details.alliedReplayedExportHashes = $alliedHashes.ToArray()

    }
    Put ($Mode+'-qualification.json') $details
} finally { $guard.Dispose() }

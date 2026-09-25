param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [switch]$Screening,
    [switch]$ThreeReferenceTie,
    [switch]$AdaptiveRacing
)
$ErrorActionPreference = 'Stop'
if (([int]$Screening.IsPresent + [int]$ThreeReferenceTie.IsPresent + [int]$AdaptiveRacing.IsPresent) -gt 1) { throw 'Choose one versioned compatibility fixture.' }
$runtime = Join-Path $Package 'runtime'
$assembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $runtime 'BalanceHarness.dll'))
$flags = [System.Reflection.BindingFlags]'Static,Instance,Public,NonPublic,DeclaredOnly'
$execution = [BalanceHarness.ExecutionIdentity]::Current()
$contentRoot = [string](Join-Path $Package 'content')
$settings = [BalanceHarness.TowerBundle].GetMethod('ReadSettings', [System.Reflection.BindingFlags]'Static,NonPublic').Invoke(
    $null, [object[]]@($contentRoot))

# Authenticate the source snapshot against the portable symbols of the tested DLL.
# The DLL itself is pinned by the preceding engineering verification receipt.
$pdbStream = [IO.File]::OpenRead((Join-Path $runtime 'BalanceHarness.pdb'))
$dllStream = [IO.File]::OpenRead((Join-Path $runtime 'BalanceHarness.dll'))
try {
    $provider = [System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
    $reader = $provider.GetMetadataReader()
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($dllStream)
    $codeView = @($pe.ReadDebugDirectory() | Where-Object { $_.Type.ToString() -eq 'CodeView' })
    if ($codeView.Count -ne 1) { throw 'Expected one producing CodeView identity.' }
    $codeViewData = $pe.ReadCodeViewDebugDirectoryData($codeView[0])
    $symbolGuid = [Guid]::new([byte[]]@($reader.DebugMetadataHeader.Id | Select-Object -First 16))
    if ($symbolGuid -ne $codeViewData.Guid) { throw 'Portable symbols do not belong to the tested harness.' }
    $sourceFiles = [Collections.Generic.SortedDictionary[string,string]]::new([StringComparer]::Ordinal)
    $repository = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    foreach ($handle in $reader.Documents) {
        $document = $reader.GetDocument($handle)
        if ($reader.GetGuid($document.HashAlgorithm) -ne [Guid]'8829d00f-11b8-4213-878b-770e8597ac16') { throw 'Unexpected symbol checksum algorithm.' }
        $path = [IO.Path]::GetFullPath($reader.GetString($document.Name))
        if (-not $path.StartsWith($repository, [StringComparison]::OrdinalIgnoreCase)) { throw "Symbol source outside repository: $path" }
        $expected = [Convert]::ToHexStringLower($reader.GetBlobBytes($document.Hash))
        $actual = [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($path)))
        if ($expected -ne $actual) { throw "Source differs from producing symbols: $path" }
        $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $path).Replace('\', '/')
        $target = Join-Path (Join-Path $Package 'source') $relative
        [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
        [IO.File]::Copy($path, $target, $false)
        $sourceFiles.Add($relative, $actual)
    }
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'compiled-source-files.json'), $sourceFiles)
} finally {
    if ($pe) { $pe.Dispose() }
    if ($provider) { $provider.Dispose() }
    $dllStream.Dispose(); $pdbStream.Dispose()
}

# Resolve comparison entry paths, closures and async state machines without calling
# their bodies. CreateInput, evaluation, reservation and combat are not invoked here.
$typeNames = @('TowerReferenceExplorationComparison', 'TowerReferenceExploration', 'TowerPracticalSearch', 'TowerBossImprovement', 'TowerSuppliedCompositionSearch',
    'TowerBossStudyPolicy', 'TowerBossDiscoveryRun', 'TowerBattleRunner', 'TowerLoadoutArchive')
if ($Screening) { $typeNames += 'TowerPracticalScreening' }
if ($ThreeReferenceTie) { $typeNames += 'TowerIncumbentTieComparison' }
if ($AdaptiveRacing) { $typeNames += @('TowerAdaptiveRacingComparison', 'TowerAdaptiveRacing', 'TowerAdaptiveRacingGenerator', 'TowerAdaptiveRacingNative', 'TowerBatchRacing') }
$queue = [Collections.Generic.Queue[Type]]::new()
foreach ($name in $typeNames) { $queue.Enqueue($assembly.GetType('BalanceHarness.' + $name, $true)) }
$compiled = [Collections.Generic.List[string]]::new()
while ($queue.Count -gt 0) {
    $type = $queue.Dequeue()
    foreach ($nested in $type.GetNestedTypes([System.Reflection.BindingFlags]'Public,NonPublic')) { $queue.Enqueue($nested) }
    foreach ($method in $type.GetMethods($flags)) {
        if ($method.ContainsGenericParameters -or $method.IsAbstract -or $null -eq $method.GetMethodBody()) { continue }
        [System.Runtime.CompilerServices.RuntimeHelpers]::PrepareMethod($method.MethodHandle)
        $compiled.Add("$($type.FullName).$($method.Name)#$($method.MetadataToken)")
    }
}
$context = @{
    status = 'CapturedRuntimeEntryPathsCompatible'
    execution = $execution; executionHash = [BalanceHarness.HarnessJson]::Hash[object]($execution)
    settings = $settings; settingsHash = [BalanceHarness.HarnessJson]::Hash[object]($settings)
    producingSymbolGuid = $symbolGuid.ToString(); compiledSourceDocuments = $sourceFiles.Count
    jitResolvedMethods = $compiled.ToArray(); nativePreparations = 0; fights = 0; newValues = 0
}
if ($Screening) {
    # Only stored engineering measurements enter these pure stage contracts. The
    # separate public Check prepares the real captured references without combat.
    $fixture = Join-Path $Package 'screening-fixture'
    $template = [BalanceHarness.TowerBossDiscovery]::Read((Join-Path $fixture 'template.json'))
    $values = [BalanceHarness.HarnessJson]::Read[BalanceHarness.ExplorationReservation]((Join-Path $fixture 'fixture-values.json'))
    $run = [BalanceHarness.HarnessJson]::Read[BalanceHarness.ExplorationArm]((Join-Path $fixture 'candidate.json'))
    $mechanics = [BalanceHarness.HarnessJson]::Read[BalanceHarness.BossGenerationMechanics]((Join-Path $fixture 'mechanics.json'))
    $version = 'tower-practical-fresh-screening-comparison-v1'
    if ($values.Version -ne $version -or $values.Selected.Count -ne 12588) { throw 'Changed screening fixture version or allocation.' }
    $comparison = [BalanceHarness.TowerReferenceExplorationComparison]
    $screeningType = [BalanceHarness.TowerPracticalScreening]
    $traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
    $constructor = $traceType.GetConstructor($flags, $null, [Type[]]@([Action[bool]]), $null)
    $trace = $constructor.Invoke([object[]]@([Action[bool]]{ param($completed) throw 'Screening compatibility cannot fight.' }))
    $guard = $trace.Activate()
    try {
        $definition = $comparison.GetMethod('Bind', $flags).Invoke($null, [object[]]@($template, $values.Selected, 0, $true, $version))
        [int[]]$panel = $comparison.GetMethod('ScreeningPanel', $flags).Invoke($null, [object[]]@($values.Selected, 0, $version))
        $freeze = $screeningType.GetMethod('Freeze', $flags).Invoke($null, [object[]]@($definition, $run.Discovery, $panel))
        $screen = $screeningType.GetMethod('Nominate', $flags).Invoke($null, [object[]]@($definition, $run.Discovery, $panel, $freeze, $run.Screening.Measurements))
        if ([BalanceHarness.HarnessJson]::Hash[object]($screen) -ne [BalanceHarness.HarnessJson]::Hash[object]($run.Screening)) {
            throw 'Captured-runtime screening differs from the tested fixture.'
        }
        $selected = [BalanceHarness.TowerBossStudyPolicy]::Select($definition, $mechanics, $screen.Nominees, $run.Selection)
        if ($selected.Count -ne 1 -or [BalanceHarness.HarnessJson]::Hash[object]($selected[0]) -ne [BalanceHarness.HarnessJson]::Hash[object]($run.Output.Finalist)) {
            throw 'Captured-runtime final selection differs from the tested fixture.'
        }
        $context.screeningFixture = @{
            status = 'StoredMeasurementsReconstructed'; version = $version
            candidateFileSha256 = [BalanceHarness.HarnessJson]::FileHash((Join-Path $fixture 'candidate.json'))
            discoveryFights = $freeze.AfterDiscoveryFights; screeningMembers = $freeze.Candidates.Count
            afterSearchFights = $screen.AfterSearchFights; nominees = $screen.Nominees.Count
            newFights = 0; newValues = 0
        }
    } finally { $guard.Dispose() }
}
if ($ThreeReferenceTie) {
    # Reconstruct both selectors from the same stored engineering measurements.
    # The later public Check is the only native reference-preparation operation.
    $fixture = Join-Path $Package 'selector-fixture'
    $template = [BalanceHarness.TowerBossDiscovery]::Read((Join-Path $fixture 'template.json'))
    $values = [BalanceHarness.HarnessJson]::Read[BalanceHarness.IncumbentTieReservation]((Join-Path $fixture 'fixture-values.json'))
    $search = [BalanceHarness.HarnessJson]::Read[BalanceHarness.IncumbentTieSearch]((Join-Path $fixture 'search.json'))
    $mechanics = [BalanceHarness.HarnessJson]::Read[BalanceHarness.BossGenerationMechanics]((Join-Path $fixture 'mechanics.json'))
    $version = 'tower-three-reference-tie-comparison-v1'
    if ($values.Version -ne $version -or $values.Selected.Count -ne 24984) { throw 'Changed selector fixture allocation.' }
    $comparison = [BalanceHarness.TowerIncumbentTieComparison]
    $traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
    $constructor = $traceType.GetConstructor($flags, $null, [Type[]]@([Action[bool]]), $null)
    $trace = $constructor.Invoke([object[]]@([Action[bool]]{ param($completed) throw 'Selector compatibility cannot fight.' }))
    $guard = $trace.Activate()
    try {
        $baseline = $comparison.GetMethod('Bind', $flags).Invoke($null, [object[]]@($template, $values.Selected, 0, $false, $version))
        $candidate = $comparison.GetMethod('Bind', $flags).Invoke($null, [object[]]@($template, $values.Selected, 0, $true, $version))
        $rebuilt = $comparison.GetMethod('Select', $flags).Invoke($null, [object[]]@(
            0, $baseline, $candidate, $mechanics, $search.Discovery, $search.Selection, $version))
        if ([BalanceHarness.HarnessJson]::Hash[object]($rebuilt) -ne [BalanceHarness.HarnessJson]::Hash[object]($search)) {
            throw 'Captured-runtime selector reconstruction differs from the tested fixture.'
        }
        $context.selectorFixture = @{
            status = 'StoredMeasurementsReconstructed'; version = $version
            searchFileSha256 = [BalanceHarness.HarnessJson]::FileHash((Join-Path $fixture 'search.json'))
            nominees = $search.Discovery.DiscoveryShortlist.Count; selectionMeasurements = $search.Selection.Count
            baselineSelector = $rebuilt.Baseline.Selector; candidateSelector = $rebuilt.Candidate.Selector
            baselineParty = $rebuilt.Baseline.Finalist.Party.Id; candidateParty = $rebuilt.Candidate.Finalist.Party.Id
            newFights = 0; newValues = 0
        }
    } finally { $guard.Dispose() }
}
if ($AdaptiveRacing) {
    # Rebuild the tested first root entirely from stored observations. This covers
    # proposal generation and all five panels against the captured dependencies.
    $fixture = Join-Path $Package 'adaptive-fixture'
    $plan = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerAdaptiveRacingPlan]((Join-Path $fixture 'plan.json'))
    $saved = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerAdaptiveRacingReport]((Join-Path $fixture 'report.json'))
    $traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
    $constructor = $traceType.GetConstructor($flags, $null, [Type[]]@([Action[bool]]), $null)
    $trace = $constructor.Invoke([object[]]@([Action[bool]]{ param($completed) throw 'Adaptive compatibility cannot fight.' }))
    $guard = $trace.Activate()
    try {
        $rebuilt = [BalanceHarness.TowerAdaptiveRacing]::ReconstructAsync($plan, $saved, [Threading.CancellationToken]::None).GetAwaiter().GetResult()
        if ($rebuilt.Evaluation.Status -ne 'Complete' -or $rebuilt.Evaluation.ChargedEvaluations -ne 528 -or
            $rebuilt.Batches.Count -ne 2 -or $rebuilt.Evaluation.Panels.Count -ne 5 -or
            [BalanceHarness.HarnessJson]::Hash[object]($rebuilt) -ne [BalanceHarness.HarnessJson]::Hash[object]($saved)) {
            throw 'Captured-runtime adaptive reconstruction differs from the tested fixture.'
        }
        $context.adaptiveFixture = @{
            status = 'StoredMeasurementsReconstructed'; version = 'tower-adaptive-beam-racing-v1'
            planFileSha256 = [BalanceHarness.HarnessJson]::FileHash((Join-Path $fixture 'plan.json'))
            reportFileSha256 = [BalanceHarness.HarnessJson]::FileHash((Join-Path $fixture 'report.json'))
            storedEvaluations = 528; batches = 2; panels = 5
            newFights = 0; newValues = 0
        }
    } finally { $guard.Dispose() }
}
[BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'context.json'), $context)

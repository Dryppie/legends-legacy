param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$RepositoryRoot
)
$ErrorActionPreference = 'Stop'
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
        $relative = [IO.Path]::GetRelativePath($RepositoryRoot, $path).Replace('\', '/')
        $source = $path
        $actual = [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($source)))
        if ($expected -ne $actual) { throw "Source differs from producing symbols: $source" }
        $target = Join-Path (Join-Path $Package 'source') $relative
        [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target))
        [IO.File]::Copy($source, $target, $false)
        $sourceFiles.Add($relative, $actual)
    }
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'compiled-source-files.json'), $sourceFiles)
} finally {
    if ($pe) { $pe.Dispose() }
    if ($provider) { $provider.Dispose() }
    $dllStream.Dispose(); $pdbStream.Dispose()
}

# Resolve native entry points and their state machines without executing bodies.
$queue = [Collections.Generic.Queue[Type]]::new()
foreach ($name in @('TowerFixedFamilyConfirmation','TowerFixedFamilyPolicy','TowerBattleRunner','TowerLoadoutArchive',
    'TowerBalanceEvaluator','TowerRefinementComparisonLaunch','TowerCompleteReservation','TowerPracticalSearch')) {
    $queue.Enqueue($assembly.GetType('BalanceHarness.'+$name,$true))
}
$compiled = [Collections.Generic.List[string]]::new()
while ($queue.Count -gt 0) {
    $type = $queue.Dequeue()
    foreach ($nested in $type.GetNestedTypes([System.Reflection.BindingFlags]'Public,NonPublic')) { $queue.Enqueue($nested) }
    foreach ($method in $type.GetMethods($flags)) {
        if ($method.ContainsGenericParameters -or $method.IsAbstract -or $null -eq $method.GetMethodBody()) { continue }
        [Runtime.CompilerServices.RuntimeHelpers]::PrepareMethod($method.MethodHandle)
        $compiled.Add("$($type.FullName).$($method.Name)#$($method.MetadataToken)")
    }
}

$fixture = Join-Path $Package 'literal-fixture'
$study = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerFixedFamilyStudy]((Join-Path $fixture 'stored-study.json'))
$expected = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerRecognitionResult]((Join-Path $fixture 'stored-result.json'))
$binding = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerFixedFamilyPanel]((Join-Path $fixture 'stored-binding.json'))
$savedChunks = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerFixedFamilyChunk[]]((Join-Path $fixture 'stored-chunks.json'))
$confirmation = [BalanceHarness.TowerFixedFamilyConfirmation]
$traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace',$true)
$constructor = $traceType.GetConstructor($flags,$null,[Type[]]@([Action[bool]]),$null)
$trace = $constructor.Invoke([object[]]@([Action[bool]]{ param($completed) throw 'Admission compatibility cannot fight.' }))
$guard = $trace.Activate()
try {
    $result = $confirmation.GetMethod('AssessRecognition',$flags).Invoke($null,[object[]]@($study,[string](Join-Path $Package 'plan.json'),$expected.ArchiveHash))
    if ([BalanceHarness.HarnessJson]::Hash[object]($result) -ne [BalanceHarness.HarnessJson]::Hash[object]($expected)) { throw 'Stored endpoint changed.' }
    $chunks = $confirmation.GetMethod('Chunks',$flags).Invoke($null,[object[]]@($study.Freeze.Definition,$binding.Panel))
    if ([BalanceHarness.HarnessJson]::Hash[object]($chunks) -ne [BalanceHarness.HarnessJson]::Hash[object]($savedChunks)) { throw 'Stored transports changed.' }
    $bytes = [IO.File]::ReadAllBytes((Join-Path $fixture 'literal-entropy.bin'))
    $classified = $confirmation.GetMethod('Classify',$flags).Invoke($null,[object[]]@($bytes,$study.Freeze.Definition.ExcludedCombatSeeds,
        [BalanceHarness.HarnessJson]::Hash[object]($study.Freeze),$study.Version))
    if ([BalanceHarness.HarnessJson]::Hash[object]($classified) -ne [BalanceHarness.HarnessJson]::Hash[object]($binding)) { throw 'Stored literal classification changed.' }
    $native = [BalanceHarness.TowerBattleRunner]::new($contentRoot,[BalanceHarness.OfflineContent]::new($contentRoot,$settings.Threat))
    $inputs = [Collections.Generic.List[object]]::new()
    $parties = @{}
    foreach ($chunk in $chunks) {
        foreach ($seed in @($chunk.Scenario.Seeds[0],$chunk.Scenario.Seeds[$chunk.Scenario.Seeds.Count-1])) {
            $input = $native.CreateInput($chunk.Scenario,$seed,$settings.Threat,$settings.CheckpointIntervalTicks)
            $party = [BalanceHarness.HarnessJson]::Hash[object]($input.Party)
            if ($parties.ContainsKey($chunk.PartyId) -and $parties[$chunk.PartyId] -ne $party) { throw 'Physical party changed across transports.' }
            $parties[$chunk.PartyId] = $party
            if ($input.Rules.RandomSeed -ne $seed) { throw 'Input seed changed.' }
            $inputs.Add(@{teamOrdinal=$chunk.TeamOrdinal; sliceOrdinal=$chunk.SliceOrdinal; seed=$seed;
                inputHash=[BalanceHarness.HarnessJson]::Hash[object]($input); partyHash=$party})
        }
    }
    if ($inputs.Count -ne 216 -or $chunks.Count -ne 108 -or $parties.Count -ne 108) { throw 'Incomplete native transport coverage.' }
    [BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'input-projections.json'),$inputs.ToArray())
} finally { $guard.Dispose() }

[BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'context.json'),@{
    status='CapturedRecognitionRuntimeCompatible'; execution=$execution; executionHash=[BalanceHarness.HarnessJson]::Hash[object]($execution)
    settings=$settings; settingsHash=[BalanceHarness.HarnessJson]::Hash[object]($settings)
    producingSymbolGuid=$symbolGuid.ToString(); compiledSourceDocuments=$sourceFiles.Count; jitResolvedMethods=$compiled.ToArray()
    literalFixture=@{status='StoredEvidenceReconstructed'; version=$study.Version; teams=$result.Rates.Count; contrasts=$result.Contrasts.Count
        unmeasured=$result.Unmeasured.Count; strata=$result.Strata.Count; samples=256
        literalWords=$classified.Words.Count; literalPanel=$classified.Panel.Count; transports=$chunks.Count; inputProjections=$inputs.Count}
    nativePreparations=0; fights=0; newValues=0
})


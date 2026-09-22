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
$typeNames = @('TowerIncumbentTieComparison', 'TowerBossImprovement', 'TowerSuppliedCompositionSearch',
    'TowerBossStudyPolicy', 'TowerBossDiscoveryRun', 'TowerBattleRunner', 'TowerLoadoutArchive')
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
[BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Package 'context.json'), @{
    status = 'CapturedRuntimeEntryPathsCompatible'
    execution = $execution; executionHash = [BalanceHarness.HarnessJson]::Hash[object]($execution)
    settings = $settings; settingsHash = [BalanceHarness.HarnessJson]::Hash[object]($settings)
    producingSymbolGuid = $symbolGuid.ToString(); compiledSourceDocuments = $sourceFiles.Count
    jitResolvedMethods = $compiled.ToArray(); nativePreparations = 0; fights = 0; newValues = 0
})

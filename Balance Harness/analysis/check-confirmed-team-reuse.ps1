param([Parameter(Mandatory)][string]$Request, [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
$q = Get-Content -LiteralPath $Request -Raw | ConvertFrom-Json
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $q.runtimeRoot 'BalanceHarness.dll'))
$execution = [BalanceHarness.ExecutionIdentity]::Current()
if ([BalanceHarness.HarnessJson]::Hash[object]($execution) -ne $q.executionHash) {
    throw 'Target execution identity differs from the confirmed cohort.'
}
$flags = [Reflection.BindingFlags]'Static,NonPublic'
$settings = [BalanceHarness.TowerBundle].GetMethod('ReadSettings', $flags).Invoke($null, [object[]]@([string]$q.contentRoot))
if ([BalanceHarness.HarnessJson]::Hash[object]($settings) -ne $q.settingsHash) {
    throw 'Target effective settings differ from the confirmed cohort.'
}

# PowerShell loads the harness through reflection rather than the dotnet host,
# so load its declared ASP.NET shared framework at the captured runtime version.
$frameworkVersion = $execution.Runtime -replace '^\.NET ', ''
$frameworkLines = @(& dotnet --list-runtimes)
if ($LASTEXITCODE -ne 0) { throw 'Cannot locate the producing shared framework.' }
$frameworkMatch = @($frameworkLines | Where-Object { $_ -match ('^Microsoft\.AspNetCore\.App ' + [regex]::Escape($frameworkVersion) + ' \[(.+)\]$') })
if ($frameworkMatch.Count -ne 1) { throw 'Require the exact captured ASP.NET shared framework version.' }
[void]($frameworkMatch[0] -match '\[(.+)\]$')
$frameworkRoot = Join-Path $Matches[1] $frameworkVersion
$frameworkFiles = [Collections.Generic.SortedDictionary[string,string]]::new([StringComparer]::Ordinal)
foreach ($file in Get-ChildItem -LiteralPath $frameworkRoot -Filter '*.dll' -File) {
    try { $identity = [Reflection.AssemblyName]::GetAssemblyName($file.FullName) }
    catch {
        if ($_.Exception.InnerException -isnot [BadImageFormatException]) { throw }
        # Shared frameworks also contain native DLLs; retain their pins without
        # attempting to load them as managed assemblies.
        $frameworkFiles.Add($file.FullName, [BalanceHarness.HarnessJson]::FileHash($file.FullName))
        continue
    }
    $loaded = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $identity.Name })
    if ($loaded.Count) {
        if ($loaded.Count -ne 1 -or $loaded[0].FullName -ne $identity.FullName) { throw ('Conflicting framework assembly: ' + $identity.Name) }
        $frameworkFiles.Add($loaded[0].Location, [BalanceHarness.HarnessJson]::FileHash($loaded[0].Location))
    } else {
        [void][Reflection.Assembly]::LoadFrom($file.FullName)
        $frameworkFiles.Add($file.FullName, [BalanceHarness.HarnessJson]::FileHash($file.FullName))
    }
}
[BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path ([IO.Path]::GetDirectoryName($Output)) 'framework-files.json'), $frameworkFiles)

# The existing production trace rejects any accidental combat entry. The local
# sentinel only permits native preparation; exported recipes remain seed-free.
$traceType = $assembly.GetType('BalanceHarness.TowerPerformanceTrace', $true)
$trace = [Activator]::CreateInstance($traceType, [object[]]@([Action[bool]] { throw 'Reuse preparation cannot fight.' }))
$guard = $trace.Activate()
$stop = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(120))
try {
    $content = [BalanceHarness.OfflineContent]::new([string]$q.contentRoot, $settings.Threat)
    $runner = [BalanceHarness.TowerBattleRunner]::new([string]$q.contentRoot, $content)
    $prepared = [Collections.Generic.List[object]]::new()
    foreach ($team in $q.teams) {
        if ([BalanceHarness.HarnessJson]::FileHash([string]$team.scenarioPath) -ne $team.scenarioFileHash) {
            throw 'Changed seed-free scenario.'
        }
        $scenario = [BalanceHarness.HarnessJson]::Read[BalanceHarness.TowerScenario]([string]$team.scenarioPath)
        if ($scenario.Seeds.Count -ne 0) { throw 'Reuse scenarios must be seed-free.' }
        $copy = [BalanceHarness.TowerScenario]::new($scenario.SchemaVersion, $scenario.Id, $scenario.FloorNumber,
            $scenario.StartsAt, $scenario.PreparationState, $scenario.Assumptions, [int[]]@(0), $scenario.Party)
        $input = $runner.CreateInput($copy, 0, $settings.Threat, $settings.CheckpointIntervalTicks)
        $task = $runner.PrepareAsync($input, $stop.Token)
        $null = $task.GetAwaiter().GetResult()
        $prepared.Add(@{ partyId = $team.partyId; scenarioFileHash = $team.scenarioFileHash })
    }
    $stop.Token.ThrowIfCancellationRequested()
    [BalanceHarness.HarnessJson]::WriteNew[object]($Output, @{
        status = 'ContextMatchedRecipesPrepared'; executionHash = $q.executionHash; settingsHash = $q.settingsHash
        teams = $prepared.ToArray(); nativePreparations = $prepared.Count; fights = 0; newValues = 0
    })
} finally {
    $stop.Dispose()
    $guard.Dispose()
}

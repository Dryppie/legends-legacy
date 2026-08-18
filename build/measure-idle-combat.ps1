#Requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateRange(1, 10)]
    [int]$Runs = 3,

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',

    [ValidateSet('Counters', 'Cpu', 'Allocation')]
    [string]$Diagnostics = 'Counters',

    [string]$SnapshotPath = (Join-Path $PSScriptRoot '..\LL\legends_legacy_idle_benchmark.sql'),

    [string]$DatabaseName = 'legends_legacy_idle_benchmark',

    [string]$DatabaseHost = 'localhost',

    [ValidateRange(1, 65535)]
    [int]$DatabasePort = 5432,

    [string]$DatabaseUsername = 'postgres',

    [string]$DatabasePassword = $env:LL_BENCH_DB_PASSWORD,

    [string]$AdminEmail = 'admin@hotmail.com',

    [string]$AdminPassword = $env:LL_BENCH_ADMIN_PASSWORD,

    [string]$ExpectedFingerprint,

    [DateTimeOffset]$FixedUtcNow = [DateTimeOffset]'2026-08-18T12:00:00Z',

    [ValidateRange(1024, 65535)]
    [int]$ApiPort = 7051,

    [string]$PostgresBin = 'C:\Program Files\PostgreSQL\17\bin'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ExpectedDatabaseName = 'legends_legacy_idle_benchmark'
$AdminCharacterId = '11111111-1111-1111-1111-111111111111'
$RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$SnapshotPath = [IO.Path]::GetFullPath($SnapshotPath)
$ApiProject = Join-Path $RepositoryRoot 'LL\src\API\API.LL\API.LL.csproj'
$ApiProjectDirectory = Split-Path $ApiProject -Parent
$ApiExecutable = Join-Path $ApiProjectDirectory "bin\$Configuration\net10.0\API.LL.exe"
$ApiBaseUrl = "http://127.0.0.1:$ApiPort"
$FixedUtcNow = $FixedUtcNow.ToUniversalTime()
$FixedBoundary = $FixedUtcNow.AddHours(-24)

function Assert-LastExitCode([string]$Operation) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Operation failed with exit code $LASTEXITCODE."
    }
}

function Get-PostgresTool([string]$Name) {
    $path = Join-Path $PostgresBin "$Name.exe"
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "PostgreSQL tool not found: $path"
    }

    return $path
}

function Get-DotnetCountersPath {
    $userProfile = if (![string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        $env:USERPROFILE
    }
    else {
        [Environment]::GetFolderPath('UserProfile')
    }
    $packages = Join-Path $userProfile '.nuget\packages\dotnet-counters'
    $tool = Get-ChildItem -LiteralPath $packages -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object {
            Join-Path $_.FullName 'tools\net8.0\any\dotnet-counters.dll'
        } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($tool)) {
        throw 'dotnet-counters is not available in the local NuGet package cache.'
    }

    return $tool
}

function Get-DotnetTracePath {
    $userProfile = if (![string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
        $env:USERPROFILE
    }
    else {
        [Environment]::GetFolderPath('UserProfile')
    }
    $packages = Join-Path $userProfile '.nuget\packages\dotnet-trace'
    $tool = Get-ChildItem -LiteralPath $packages -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object {
            Join-Path $_.FullName 'tools\net8.0\any\dotnet-trace.dll'
        } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
        Select-Object -First 1

    if ([string]::IsNullOrWhiteSpace($tool)) {
        throw 'dotnet-trace is not available in the local NuGet package cache.'
    }

    return $tool
}

function Quote-ConnectionStringValue([string]$Value) {
    return '"' + $Value.Replace('"', '""') + '"'
}

function Restore-BenchmarkDatabase {
    param(
        [string]$Psql,
        [string]$DropDb,
        [string]$CreateDb,
        [string]$PgRestore
    )

    Write-Host "Restoring isolated benchmark database '$DatabaseName'..."
    $terminateSql = @"
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = '$DatabaseName'
  AND pid <> pg_backend_pid();
"@
    & $Psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUsername -d postgres -v ON_ERROR_STOP=1 -c $terminateSql | Out-Null
    Assert-LastExitCode 'Terminating benchmark database connections'

    & $DropDb -h $DatabaseHost -p $DatabasePort -U $DatabaseUsername --if-exists $DatabaseName
    Assert-LastExitCode 'Dropping the benchmark database'

    & $CreateDb -h $DatabaseHost -p $DatabasePort -U $DatabaseUsername $DatabaseName
    Assert-LastExitCode 'Creating the benchmark database'

    & $PgRestore `
        -h $DatabaseHost `
        -p $DatabasePort `
        -U $DatabaseUsername `
        -d $DatabaseName `
        --exit-on-error `
        --no-owner `
        --no-privileges `
        $SnapshotPath
    Assert-LastExitCode 'Restoring the benchmark snapshot'

    $fixedNowLiteral = $FixedUtcNow.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
    $boundaryLiteral = $FixedBoundary.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
    $prepareSql = @"
UPDATE public."CharacterActions"
SET "UpdatedAt" = '$fixedNowLiteral'::timestamptz,
    "NextResolutionAtUtc" = '$boundaryLiteral'::timestamptz,
    "BlockedUntilUtc" = NULL,
    "IsDeleted" = FALSE
WHERE "CharacterId" = '$AdminCharacterId'::uuid;
"@
    & $Psql -h $DatabaseHost -p $DatabasePort -U $DatabaseUsername -d $DatabaseName -v ON_ERROR_STOP=1 -c $prepareSql | Out-Null
    Assert-LastExitCode 'Preparing the fixed idle-combat boundary'

    $countSql = @"
SELECT COUNT(*)
FROM public."CharacterActions"
WHERE "CharacterId" = '$AdminCharacterId'::uuid;
"@
    $rowCount = & $Psql `
        -h $DatabaseHost `
        -p $DatabasePort `
        -U $DatabaseUsername `
        -d $DatabaseName `
        -v ON_ERROR_STOP=1 `
        -tA `
        -c $countSql
    Assert-LastExitCode 'Validating the benchmark character action'
    if (($rowCount | Select-Object -Last 1).Trim() -ne '1') {
        throw "The snapshot must contain exactly one action for benchmark character $AdminCharacterId."
    }
}

function Start-BenchmarkApi([string]$ConnectionString) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $ApiExecutable
    $startInfo.WorkingDirectory = $ApiProjectDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'
    $startInfo.Environment['ASPNETCORE_URLS'] = $ApiBaseUrl
    $startInfo.Environment['ConnectionStrings__LegendsLegacyDB'] = $ConnectionString
    $startInfo.Environment['Benchmarking__IdleCombat__Enabled'] = 'true'
    $startInfo.Environment['Benchmarking__IdleCombat__FixedUtcNow'] =
        $FixedUtcNow.ToString('O', [Globalization.CultureInfo]::InvariantCulture)
    $startInfo.Environment['FeatureManagement__SeedLocalGuestAccounts'] = 'false'
    $startInfo.Environment['Logging__LogLevel__Default'] = 'Warning'
    $startInfo.Environment['Logging__LogLevel__Microsoft'] = 'Warning'
    $startInfo.Environment['Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command'] = 'Warning'

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (!$process.Start()) {
        throw 'Failed to start the benchmark API.'
    }

    return $process
}

function Wait-BenchmarkApi([Diagnostics.Process]$Process) {
    $deadline = [DateTimeOffset]::UtcNow.AddMinutes(2)
    while ([DateTimeOffset]::UtcNow -lt $deadline) {
        if ($Process.HasExited) {
            throw "Benchmark API exited during startup with code $($Process.ExitCode)."
        }

        try {
            $response = Invoke-WebRequest -Uri "$ApiBaseUrl/healthz/live" -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    throw 'Benchmark API did not become ready within two minutes.'
}

function Start-CounterCollector(
    [string]$DotnetCounters,
    [int]$ProcessId,
    [string]$OutputPath) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add($DotnetCounters)
    $startInfo.ArgumentList.Add('collect')
    $startInfo.ArgumentList.Add('--process-id')
    $startInfo.ArgumentList.Add($ProcessId.ToString([Globalization.CultureInfo]::InvariantCulture))
    $startInfo.ArgumentList.Add('--counters')
    $startInfo.ArgumentList.Add('System.Runtime,LegendsLegacy.IdleCombat')
    $startInfo.ArgumentList.Add('--refresh-interval')
    $startInfo.ArgumentList.Add('1')
    $startInfo.ArgumentList.Add('--format')
    $startInfo.ArgumentList.Add('json')
    $startInfo.ArgumentList.Add('--output')
    $startInfo.ArgumentList.Add($OutputPath)
    $startInfo.ArgumentList.Add('--duration')
    $startInfo.ArgumentList.Add('00:00:30')

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (!$process.Start()) {
        throw 'Failed to start dotnet-counters.'
    }

    Start-Sleep -Seconds 2
    if ($process.HasExited) {
        $errorOutput = $process.StandardError.ReadToEnd()
        throw "dotnet-counters exited during startup: $errorOutput"
    }

    return $process
}

function Stop-CounterCollector([Diagnostics.Process]$Process) {
    if (!$Process.HasExited -and !$Process.WaitForExit(35000)) {
        $Process.Kill($true)
        throw 'dotnet-counters exceeded its configured collection duration.'
    }

    if ($Process.ExitCode -ne 0) {
        throw "dotnet-counters failed with exit code $($Process.ExitCode): $($Process.StandardError.ReadToEnd())"
    }
}

function Start-TraceCollector(
    [string]$DotnetTrace,
    [int]$ProcessId,
    [string]$OutputPath,
    [ValidateSet('Cpu', 'Allocation')]
    [string]$Mode) {
    $profile = if ($Mode -eq 'Cpu') {
        'dotnet-sampled-thread-time,dotnet-common'
    }
    else {
        'dotnet-sampled-thread-time,gc-verbose'
    }
    $bufferSize = if ($Mode -eq 'Allocation') { '1024' } else { '256' }

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.ArgumentList.Add($DotnetTrace)
    $startInfo.ArgumentList.Add('collect')
    $startInfo.ArgumentList.Add('--process-id')
    $startInfo.ArgumentList.Add($ProcessId.ToString([Globalization.CultureInfo]::InvariantCulture))
    $startInfo.ArgumentList.Add('--profile')
    $startInfo.ArgumentList.Add($profile)
    $startInfo.ArgumentList.Add('--buffersize')
    $startInfo.ArgumentList.Add($bufferSize)
    $startInfo.ArgumentList.Add('--output')
    $startInfo.ArgumentList.Add($OutputPath)
    $startInfo.ArgumentList.Add('--duration')
    $startInfo.ArgumentList.Add('00:00:00:30')

    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (!$process.Start()) {
        throw "Failed to start dotnet-trace for $Mode profiling."
    }

    Start-Sleep -Seconds 2
    if ($process.HasExited) {
        $errorOutput = $process.StandardError.ReadToEnd()
        throw "dotnet-trace exited during startup: $errorOutput"
    }

    return $process
}

function Stop-TraceCollector([Diagnostics.Process]$Process) {
    if (!$Process.HasExited -and !$Process.WaitForExit(45000)) {
        $Process.Kill($true)
        throw 'dotnet-trace exceeded its configured collection duration.'
    }

    if ($Process.ExitCode -ne 0) {
        throw "dotnet-trace failed with exit code $($Process.ExitCode): $($Process.StandardError.ReadToEnd())"
    }
}

function Get-CounterSummary([string]$Path, [double]$HttpDurationMs) {
    $document = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $p50 = $document.Events | Where-Object tags -eq 'Percentile=50'
    $resolve = $p50 |
        Where-Object name -eq 'idle_combat.resolve.duration (ms)' |
        Select-Object -Last 1
    if ($null -eq $resolve) {
        throw "No idle-combat resolve measurement was found in $Path."
    }

    $windowEnd = [DateTimeOffset]$resolve.timestamp
    $windowStart = $windowEnd.AddMilliseconds(-[double]$resolve.value)
    $runtime = $document.Events | Where-Object {
        $_.provider -eq 'System.Runtime' -and
        ([DateTimeOffset]$_.timestamp) -ge $windowStart -and
        ([DateTimeOffset]$_.timestamp) -le $windowEnd
    }
    $collections = $runtime | Where-Object name -eq 'dotnet.gc.collections ({collection} / 1 sec)'
    $workingSet = $runtime | Where-Object name -eq 'dotnet.process.memory.working_set (By)'

    return [ordered]@{
        HttpDurationMs = [Math]::Round($HttpDurationMs, 3)
        ResolveDurationMs = [double]$resolve.value
        Encounters = [int](($p50 | Where-Object name -eq 'idle_combat.resolve.encounters (encounters)' | Measure-Object value -Sum).Sum)
        Batches = [int](($p50 | Where-Object name -eq 'idle_combat.resolve.batches (batches)' | Measure-Object value -Sum).Sum)
        SimulationDurationMs = [double](($p50 | Where-Object name -eq 'idle_combat.simulation.duration (ms)' | Measure-Object value -Sum).Sum)
        SimulationAllocatedBytes = [long](($p50 | Where-Object name -eq 'idle_combat.simulation.allocated (By)' | Measure-Object value -Sum).Sum)
        RuntimeAllocatedBytes = [long](($runtime | Where-Object name -eq 'dotnet.gc.heap.total_allocated (By / 1 sec)' | Measure-Object value -Sum).Sum)
        CpuSeconds = [Math]::Round((($runtime | Where-Object name -eq 'dotnet.process.cpu.time (s / 1 sec)' | Measure-Object value -Sum).Sum), 6)
        GcPauseMilliseconds = [Math]::Round((($runtime | Where-Object name -eq 'dotnet.gc.pause.time (s / 1 sec)' | Measure-Object value -Sum).Sum) * 1000, 3)
        Gen0Collections = [int](($collections | Where-Object tags -eq 'gc.heap.generation=gen0' | Measure-Object value -Sum).Sum)
        Gen1Collections = [int](($collections | Where-Object tags -eq 'gc.heap.generation=gen1' | Measure-Object value -Sum).Sum)
        Gen2Collections = [int](($collections | Where-Object tags -eq 'gc.heap.generation=gen2' | Measure-Object value -Sum).Sum)
        WorkingSetMinimumBytes = [long](($workingSet | Measure-Object value -Minimum).Minimum)
        WorkingSetMaximumBytes = [long](($workingSet | Measure-Object value -Maximum).Maximum)
    }
}

function ConvertTo-CanonicalValue {
    param(
        [AllowNull()]
        [object]$Value,

        [Collections.Generic.HashSet[string]]$ExcludedProperties
    )

    if ($null -eq $Value) {
        return $null
    }

    if ($Value -is [Collections.IDictionary]) {
        $result = [ordered]@{}
        foreach ($key in @($Value.Keys | ForEach-Object { [string]$_ } | Sort-Object)) {
            if (!$ExcludedProperties.Contains($key)) {
                $result[$key] = ConvertTo-CanonicalValue -Value $Value[$key] -ExcludedProperties $ExcludedProperties
            }
        }

        return $result
    }

    if ($Value -is [Management.Automation.PSCustomObject]) {
        $result = [ordered]@{}
        foreach ($property in @($Value.PSObject.Properties | Sort-Object Name)) {
            if (!$ExcludedProperties.Contains($property.Name)) {
                $result[$property.Name] = ConvertTo-CanonicalValue `
                    -Value $property.Value `
                    -ExcludedProperties $ExcludedProperties
            }
        }

        return $result
    }

    if ($Value -is [Collections.IEnumerable] -and $Value -isnot [string]) {
        $items = [Collections.Generic.List[object]]::new()
        foreach ($item in $Value) {
            $items.Add((ConvertTo-CanonicalValue -Value $item -ExcludedProperties $ExcludedProperties))
        }

        return ,$items.ToArray()
    }

    return $Value
}

function Get-CanonicalFingerprint {
    param(
        [AllowNull()]
        [object]$Value,

        [string[]]$ExcludedProperties = @()
    )

    $excluded = [Collections.Generic.HashSet[string]]::new(
        $ExcludedProperties,
        [StringComparer]::OrdinalIgnoreCase)
    $canonical = ConvertTo-CanonicalValue -Value $Value -ExcludedProperties $excluded
    $json = $canonical | ConvertTo-Json -Depth 100 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()

    return [pscustomobject]@{
        Hash = $hash
        Json = $json
    }
}

function Get-DatabaseGameplayState([string]$Psql) {
    $stateSql = @"
WITH benchmark_character AS (
    SELECT "UserId" AS account_id
    FROM public."Entities"
    WHERE "Id" = '$AdminCharacterId'::uuid
),
inventory_state AS (
    SELECT jsonb_build_object(
        'inventoryItem', to_jsonb(inventory_item) - ARRAY['InventoryId', 'ItemInstanceId'],
        'item', to_jsonb(item_instance) - ARRAY['Id', 'AcquiredAtUtc', 'SourceItemInstanceId'],
        'modifiers', COALESCE((
            SELECT jsonb_agg(
                to_jsonb(modifier) - ARRAY['Id', 'ItemInstanceId']
                ORDER BY (to_jsonb(modifier) - ARRAY['Id', 'ItemInstanceId'])::text)
            FROM public."InstanceAttributeModifier" AS modifier
            WHERE modifier."ItemInstanceId" = item_instance."Id"
        ), '[]'::jsonb),
        'toolAffixes', COALESCE((
            SELECT jsonb_agg(
                to_jsonb(affix) - ARRAY['Id', 'EquipmentInstanceId']
                ORDER BY (to_jsonb(affix) - ARRAY['Id', 'EquipmentInstanceId'])::text)
            FROM public."ToolBonusModifier" AS affix
            WHERE affix."EquipmentInstanceId" = item_instance."Id"
        ), '[]'::jsonb)
    ) AS value
    FROM public."InventoryItems" AS inventory_item
    INNER JOIN public."ItemInstances" AS item_instance
        ON item_instance."Id" = inventory_item."ItemInstanceId"
    WHERE inventory_item."InventoryId" = '$AdminCharacterId'::uuid
),
loot_history_state AS (
    SELECT jsonb_set(
        to_jsonb(history) - ARRAY['Id', 'CharacterId', 'ReceivedAt'],
        '{ItemSnapshotJson}',
        history."ItemSnapshotJson" - ARRAY[
            'id', 'Id', 'itemInstanceId', 'ItemInstanceId',
            'acquiredAtUtc', 'AcquiredAtUtc', 'sourceItemInstanceId', 'SourceItemInstanceId'
        ]) AS value
    FROM public."LootHistoryEntries" AS history
    WHERE history."CharacterId" = '$AdminCharacterId'::uuid
),
economy_state AS (
    SELECT to_jsonb(entry) - ARRAY[
        'Id', 'ReferenceId', 'SourceItemInstanceId', 'DestinationItemInstanceId', 'OccurredAt'
    ] AS value
    FROM public."EconomyLedger" AS entry
    WHERE entry."SenderCharacterId" = '$AdminCharacterId'::uuid
       OR entry."RecipientCharacterId" = '$AdminCharacterId'::uuid
)
SELECT jsonb_build_object(
    'character', (
        SELECT to_jsonb(character) - ARRAY['Id', 'UserId', 'RowVersion']
        FROM public."Entities" AS character
        WHERE character."Id" = '$AdminCharacterId'::uuid
    ),
    'action', (
        SELECT to_jsonb(action) - ARRAY['CharacterId', 'UpdatedAt', 'RowVersion']
        FROM public."CharacterActions" AS action
        WHERE action."CharacterId" = '$AdminCharacterId'::uuid
    ),
    'inventory', COALESCE((
        SELECT jsonb_agg(value ORDER BY value::text)
        FROM inventory_state
    ), '[]'::jsonb),
    'professions', COALESCE((
        SELECT jsonb_agg(
            to_jsonb(profession) - 'CharacterId'
            ORDER BY (to_jsonb(profession) - 'CharacterId')::text)
        FROM public."Professions" AS profession
        WHERE profession."CharacterId" = '$AdminCharacterId'::uuid
    ), '[]'::jsonb),
    'creatureArchive', COALESCE((
        SELECT jsonb_agg(
            to_jsonb(entry) - ARRAY['Id', 'CharacterId']
            ORDER BY (to_jsonb(entry) - ARRAY['Id', 'CharacterId'])::text)
        FROM public."CharacterCreatureArchiveEntries" AS entry
        WHERE entry."CharacterId" = '$AdminCharacterId'::uuid
    ), '[]'::jsonb),
    'achievements', COALESCE((
        SELECT jsonb_agg(
            to_jsonb(progress) - ARRAY['Id', 'AccountId', 'CharacterId', 'CreatedAt', 'UpdatedAt']
            ORDER BY (to_jsonb(progress) - ARRAY['Id', 'AccountId', 'CharacterId', 'CreatedAt', 'UpdatedAt'])::text)
        FROM public."PlayerAchievementProgresses" AS progress
        CROSS JOIN benchmark_character
        WHERE progress."CharacterId" = '$AdminCharacterId'::uuid
           OR progress."AccountId" = benchmark_character.account_id
    ), '[]'::jsonb),
    'quests', COALESCE((
        SELECT jsonb_agg(
            to_jsonb(progress) - ARRAY['CharacterId', 'CreatedAt', 'UpdatedAt', 'RowVersion']
            ORDER BY (to_jsonb(progress) - ARRAY['CharacterId', 'CreatedAt', 'UpdatedAt', 'RowVersion'])::text)
        FROM public."CharacterQuestProgresses" AS progress
        WHERE progress."CharacterId" = '$AdminCharacterId'::uuid
    ), '[]'::jsonb),
    'questObjectives', COALESCE((
        SELECT jsonb_agg(
            to_jsonb(progress) - ARRAY['CharacterId', 'UpdatedAt']
            ORDER BY (to_jsonb(progress) - ARRAY['CharacterId', 'UpdatedAt'])::text)
        FROM public."CharacterQuestObjectiveProgresses" AS progress
        WHERE progress."CharacterId" = '$AdminCharacterId'::uuid
    ), '[]'::jsonb),
    'lootHistory', COALESCE((
        SELECT jsonb_agg(value ORDER BY value::text)
        FROM loot_history_state
    ), '[]'::jsonb),
    'economyLedger', COALESCE((
        SELECT jsonb_agg(value ORDER BY value::text)
        FROM economy_state
    ), '[]'::jsonb)
);
"@

    $json = & $Psql `
        -h $DatabaseHost `
        -p $DatabasePort `
        -U $DatabaseUsername `
        -d $DatabaseName `
        -v ON_ERROR_STOP=1 `
        -tA `
        -c $stateSql
    Assert-LastExitCode 'Reading the benchmark gameplay state'
    $json = ($json | Select-Object -Last 1).Trim()
    if ([string]::IsNullOrWhiteSpace($json)) {
        throw 'The benchmark gameplay-state query returned no JSON.'
    }

    return $json | ConvertFrom-Json -Depth 100 -AsHashtable
}

function Get-Median([double[]]$Values) {
    $sorted = @($Values | Sort-Object)
    $middle = [Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2 -eq 1) {
        return $sorted[$middle]
    }

    return ($sorted[$middle - 1] + $sorted[$middle]) / 2
}

if ($DatabaseName -cne $ExpectedDatabaseName) {
    throw "Refusing destructive restore: database name must be exactly '$ExpectedDatabaseName'."
}
if ($DatabaseHost -notin @('localhost', '127.0.0.1', '::1')) {
    throw 'Refusing destructive restore: the benchmark database host must be local.'
}
if ([string]::IsNullOrWhiteSpace($DatabasePassword)) {
    throw 'Set LL_BENCH_DB_PASSWORD or pass -DatabasePassword.'
}
if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    throw 'Set LL_BENCH_ADMIN_PASSWORD or pass -AdminPassword.'
}
if (![string]::IsNullOrWhiteSpace($ExpectedFingerprint) -and
    $ExpectedFingerprint -notmatch '^[0-9a-fA-F]{64}$') {
    throw 'ExpectedFingerprint must be a 64-character SHA-256 hash.'
}
if (!(Test-Path -LiteralPath $SnapshotPath -PathType Leaf)) {
    throw "Benchmark snapshot not found: $SnapshotPath"
}
$magic = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($SnapshotPath), 0, 5)
if ($magic -ne 'PGDMP') {
    throw 'The supplied benchmark snapshot must be a PostgreSQL custom archive (PGDMP).'
}

$psql = Get-PostgresTool 'psql'
$dropDb = Get-PostgresTool 'dropdb'
$createDb = Get-PostgresTool 'createdb'
$pgRestore = Get-PostgresTool 'pg_restore'
$dotnetCounters = if ($Diagnostics -eq 'Counters') { Get-DotnetCountersPath } else { $null }
$dotnetTrace = if ($Diagnostics -eq 'Counters') { $null } else { Get-DotnetTracePath }
$connectionString = @(
    "Host=$(Quote-ConnectionStringValue $DatabaseHost)"
    "Port=$DatabasePort"
    "Database=$(Quote-ConnectionStringValue $DatabaseName)"
    "Username=$(Quote-ConnectionStringValue $DatabaseUsername)"
    "Password=$(Quote-ConnectionStringValue $DatabasePassword)"
) -join ';'

Write-Host "Building benchmark API ($Configuration)..."
& dotnet build $ApiProject --configuration $Configuration --no-restore
Assert-LastExitCode 'Building the benchmark API'
if (!(Test-Path -LiteralPath $ApiExecutable -PathType Leaf)) {
    throw "Benchmark API executable not found after build: $ApiExecutable"
}

$runRoot = Join-Path $RepositoryRoot (
    'TestResults\idle-combat-benchmark\' +
    [DateTimeOffset]::UtcNow.ToString('yyyyMMdd-HHmmss', [Globalization.CultureInfo]::InvariantCulture))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$summaries = [Collections.Generic.List[object]]::new()
$referenceFingerprint = if ([string]::IsNullOrWhiteSpace($ExpectedFingerprint)) {
    $null
}
else {
    $ExpectedFingerprint.ToLowerInvariant()
}
$previousPgPassword = $env:PGPASSWORD
$env:PGPASSWORD = $DatabasePassword

try {
    for ($run = 1; $run -le $Runs; $run++) {
        Restore-BenchmarkDatabase -Psql $psql -DropDb $dropDb -CreateDb $createDb -PgRestore $pgRestore
        $api = $null
        $collector = $null
        try {
            Write-Host "Starting benchmark run $run of $Runs..."
            $api = Start-BenchmarkApi $connectionString
            Wait-BenchmarkApi $api

            $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
            $login = Invoke-RestMethod `
                -Method Post `
                -Uri "$ApiBaseUrl/api/v1/Auth/login" `
                -ContentType 'application/json' `
                -Body $loginBody
            if ([string]::IsNullOrWhiteSpace($login.accessToken)) {
                throw 'Benchmark login did not return an access token.'
            }

            $headers = @{ Authorization = "Bearer $($login.accessToken)" }
            $action = Invoke-RestMethod -Method Get -Uri "$ApiBaseUrl/api/v1/CharacterActions" -Headers $headers
            $actualBoundary = ([DateTimeOffset]$action.nextResolutionAtUtc).ToUniversalTime()
            if ($actualBoundary -ne $FixedBoundary) {
                throw "Unexpected benchmark boundary. Expected $FixedBoundary; got $actualBoundary."
            }

            $diagnosticPath = if ($Diagnostics -eq 'Counters') {
                Join-Path $runRoot ("run-{0:D2}.counters.json" -f $run)
            }
            else {
                Join-Path $runRoot ("run-{0:D2}.{1}.nettrace" -f $run, $Diagnostics.ToLowerInvariant())
            }
            $collector = if ($Diagnostics -eq 'Counters') {
                Start-CounterCollector `
                    -DotnetCounters $dotnetCounters `
                    -ProcessId $api.Id `
                    -OutputPath $diagnosticPath
            }
            else {
                Start-TraceCollector `
                    -DotnetTrace $dotnetTrace `
                    -ProcessId $api.Id `
                    -OutputPath $diagnosticPath `
                    -Mode $Diagnostics
            }
            $stopwatch = [Diagnostics.Stopwatch]::StartNew()
            $result = Invoke-RestMethod `
                -Method Post `
                -Uri "$ApiBaseUrl/api/v1/CharacterActions/Resolve" `
                -Headers $headers `
                -TimeoutSec 120
            $stopwatch.Stop()
            if ($Diagnostics -eq 'Counters') {
                Stop-CounterCollector $collector
            }
            else {
                Stop-TraceCollector $collector
            }
            $collector = $null

            if ($result.processedCount -ne 8641 -or $result.hasMoreDueWork) {
                throw "Unexpected resolve result: processed=$($result.processedCount), hasMore=$($result.hasMoreDueWork)."
            }

            $responseFingerprint = Get-CanonicalFingerprint `
                -Value $result `
                -ExcludedProperties @(
                    'Id',
                    'InventoryId',
                    'ItemInstanceId',
                    'SourceItemInstanceId',
                    'DestinationItemInstanceId',
                    'AcquiredAtUtc',
                    'Revision',
                    'UpdatedAt',
                    'RowVersion'
                )
            $databaseState = Get-DatabaseGameplayState -Psql $psql
            $databaseFingerprint = Get-CanonicalFingerprint -Value $databaseState
            $combinedFingerprint = Get-CanonicalFingerprint -Value ([ordered]@{
                Response = $responseFingerprint.Hash
                Database = $databaseFingerprint.Hash
            })

            $responseStatePath = Join-Path $runRoot ("run-{0:D2}.response.normalized.json" -f $run)
            $databaseStatePath = Join-Path $runRoot ("run-{0:D2}.database-state.normalized.json" -f $run)
            $responseFingerprint.Json | Set-Content -LiteralPath $responseStatePath -Encoding utf8
            $databaseFingerprint.Json | Set-Content -LiteralPath $databaseStatePath -Encoding utf8

            if ($null -eq $referenceFingerprint) {
                $referenceFingerprint = $combinedFingerprint.Hash
            }
            elseif ($combinedFingerprint.Hash -cne $referenceFingerprint) {
                throw @"
Correctness fingerprint mismatch on run $run.
Expected: $referenceFingerprint
Actual:   $($combinedFingerprint.Hash)
Compare the normalized artifacts in $runRoot.
"@
            }

            $summary = if ($Diagnostics -eq 'Counters') {
                Get-CounterSummary -Path $diagnosticPath -HttpDurationMs $stopwatch.Elapsed.TotalMilliseconds
            }
            else {
                [ordered]@{
                    HttpDurationMs = [Math]::Round($stopwatch.Elapsed.TotalMilliseconds, 3)
                    Diagnostics = $Diagnostics
                    TracePath = $diagnosticPath
                    ProcessedCount = [int]$result.processedCount
                }
            }
            $summary['Run'] = $run
            $summary['ResponseFingerprint'] = $responseFingerprint.Hash
            $summary['DatabaseFingerprint'] = $databaseFingerprint.Hash
            $summary['CorrectnessFingerprint'] = $combinedFingerprint.Hash
            $summaries.Add([pscustomobject]$summary)
            if ($Diagnostics -eq 'Counters') {
                Write-Host (
                    'Run {0}: HTTP {1:N0} ms, server {2:N0} ms, simulation {3:N0} ms, allocation {4:N3} GiB, fingerprint {5}' -f
                    $run,
                    $summary.HttpDurationMs,
                    $summary.ResolveDurationMs,
                    $summary.SimulationDurationMs,
                    ($summary.SimulationAllocatedBytes / 1GB),
                    $combinedFingerprint.Hash.Substring(0, 12))
            }
            else {
                Write-Host (
                    'Run {0}: {1} trace, HTTP {2:N0} ms, fingerprint {3}' -f
                    $run,
                    $Diagnostics,
                    $summary.HttpDurationMs,
                    $combinedFingerprint.Hash.Substring(0, 12))
            }
        }
        finally {
            if ($null -ne $collector -and !$collector.HasExited) {
                try {
                    if ($Diagnostics -eq 'Counters') {
                        Stop-CounterCollector $collector
                    }
                    else {
                        Stop-TraceCollector $collector
                    }
                }
                catch {
                    Write-Warning $_
                }
            }
            if ($null -ne $api -and !$api.HasExited) {
                $api.Kill($true)
                $api.WaitForExit()
            }
        }
    }
}
finally {
    $env:PGPASSWORD = $previousPgPassword
}

$median = if ($Diagnostics -eq 'Counters') {
    [ordered]@{
        HttpDurationMs = [Math]::Round((Get-Median @($summaries.HttpDurationMs)), 3)
        ResolveDurationMs = [Math]::Round((Get-Median @($summaries.ResolveDurationMs)), 3)
        SimulationDurationMs = [Math]::Round((Get-Median @($summaries.SimulationDurationMs)), 3)
        SimulationAllocatedBytes = [long](Get-Median @($summaries.SimulationAllocatedBytes))
        RuntimeAllocatedBytes = [long](Get-Median @($summaries.RuntimeAllocatedBytes))
        CpuSeconds = [Math]::Round((Get-Median @($summaries.CpuSeconds)), 6)
        GcPauseMilliseconds = [Math]::Round((Get-Median @($summaries.GcPauseMilliseconds)), 3)
        Gen0Collections = [Math]::Round((Get-Median @($summaries.Gen0Collections)), 1)
        Gen1Collections = [Math]::Round((Get-Median @($summaries.Gen1Collections)), 1)
        Gen2Collections = [Math]::Round((Get-Median @($summaries.Gen2Collections)), 1)
        WorkingSetMaximumBytes = [long](Get-Median @($summaries.WorkingSetMaximumBytes))
    }
}
else {
    [ordered]@{
        HttpDurationMs = [Math]::Round((Get-Median @($summaries.HttpDurationMs)), 3)
    }
}
$report = [ordered]@{
    CreatedAtUtc = [DateTimeOffset]::UtcNow
    SnapshotPath = $SnapshotPath
    DatabaseName = $DatabaseName
    FixedUtcNow = $FixedUtcNow
    FixedBoundary = $FixedBoundary
    Configuration = $Configuration
    Diagnostics = $Diagnostics
    CorrectnessFingerprint = $referenceFingerprint
    Runs = $summaries
    Median = $median
}
$reportPath = Join-Path $runRoot 'summary.json'
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $reportPath -Encoding utf8

Write-Host ''
if ($Diagnostics -eq 'Counters') {
    Write-Host "Benchmark complete. Median server resolve: $($median.ResolveDurationMs) ms"
}
else {
    Write-Host "$Diagnostics profile complete. HTTP duration: $($median.HttpDurationMs) ms"
}
Write-Host "Report: $reportPath"

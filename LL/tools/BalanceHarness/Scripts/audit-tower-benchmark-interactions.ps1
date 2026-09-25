<#
.SYNOPSIS
    Reproduce the captured benchmark's Poison/Viper scope review without running a battle.
.DESCRIPTION
    A narrow forensic audit, not a general interaction classifier or policy input.
    Loads only externally pinned captured assemblies. Calls condition, trigger and one
    periodic-damage component directly; never calls Run, prepares a team or reserves seeds.
    Run in a fresh pwsh process. Existing archives and generation metadata are read-only.
#>
param(
    [Parameter(Mandatory)][string]$Capture,
    [Parameter(Mandatory)][string]$CaptureManifestSha256,
    [Parameter(Mandatory)][string]$Preview,
    [Parameter(Mandatory)][string]$PreviewManifestSha256,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function FileHash([string]$Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function VerifyArchive([string]$Root, [string]$Pin) {
    if ($Pin -cnotmatch '^[0-9a-f]{64}$' -or (FileHash (Join-Path $Root 'files.json')) -cne $Pin) {
        throw 'External manifest pin mismatch.'
    }
    $files = Get-Content -Raw -LiteralPath (Join-Path $Root 'files.json') | ConvertFrom-Json -AsHashtable
    $actual = @(Get-ChildItem -LiteralPath $Root -Recurse -File | ForEach-Object {
        [IO.Path]::GetRelativePath($Root, $_.FullName).Replace('\', '/')
    } | Where-Object { $_ -cne 'files.json' } | Sort-Object)
    if (($actual -join "`n") -cne ((@($files.Keys | Sort-Object)) -join "`n")) { throw 'Archive inventory changed.' }
    foreach ($relative in $files.Keys) {
        if ($relative -match '(^|/)\.\.(/|$)|\\' -or [IO.Path]::IsPathRooted($relative)) { throw 'Unsafe manifest path.' }
        $path = Join-Path $Root $relative
        $item = Get-Item -LiteralPath $path
        while ($item.FullName -ne $Root) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Archive links are unsupported.' }
            $item = Get-Item -LiteralPath (Split-Path -Parent $item.FullName)
        }
        if ((FileHash $path) -cne $files[$relative]) { throw "Archive bytes changed: $relative" }
    }
    return $files
}
function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function NewWithDefaults([Reflection.ConstructorInfo]$Constructor, [object[]]$Leading) {
    $parameters = $Constructor.GetParameters()
    $values = [object[]]::new($parameters.Count)
    for ($i = 0; $i -lt $parameters.Count; $i++) {
        if ($i -lt $Leading.Count) { $values[$i] = $Leading[$i] }
        elseif ($parameters[$i].HasDefaultValue) { $values[$i] = $parameters[$i].DefaultValue }
        else { throw "Missing constructor argument: $($parameters[$i].Name)" }
    }
    $Constructor.Invoke($values)
}
function NewActor([string]$Id, [string]$Team) {
    $attributes = [Collections.Generic.Dictionary[Domain.Models.Attributes.AttributeType,single]]::new()
    $attributes.Add([Domain.Models.Attributes.AttributeType]::MaxHealth, 10000)
    $attributes.Add([Domain.Models.Attributes.AttributeType]::Power, 100)
    NewWithDefaults ([Services.LL.Combat.Engine.RuntimeCombatant].GetConstructors()[0]) @(
        $Id, $Id, [Services.LL.Combat.Engine.CombatTeam]::$Team, $attributes,
        [Services.LL.Combat.Engine.CompiledAbility[]]@())
}
function NewEngine {
    [Services.LL.Combat.Engine.FastCombatEngine]::new(
        [Collections.Generic.Dictionary[string,Services.LL.Combat.Engine.CompiledStatus]]::new(), $null)
}
function NewEvent($Source, $Target) {
    NewWithDefaults $eventConstructor @([Domain.Models.Combat.Abilities.AbilityTriggerEvent]::OnBasicAttack,
        $Source, $Target, 'basic_attack')
}
function Check([string]$Name, $Actual, $Expected) {
    Require ($Actual -ceq $Expected) "Component check failed: $Name (actual $Actual; expected $Expected)"
    $checks.Add([ordered]@{ name = $Name; actual = $Actual; expected = $Expected; passed = $true })
}

$Capture = [IO.Path]::GetFullPath($Capture).TrimEnd('\', '/')
$Preview = [IO.Path]::GetFullPath($Preview).TrimEnd('\', '/')
$Output = [IO.Path]::GetFullPath($Output).TrimEnd('\', '/')
Require (-not (Test-Path -LiteralPath $Output)) 'Output already exists; audit archives cannot be overwritten.'
foreach ($root in @($Capture, $Preview)) {
    Require (-not $Output.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) 'Output cannot be inside an input archive.'
    Require (-not ((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Archive root cannot be a link.'
}
$captureFiles = VerifyArchive $Capture $CaptureManifestSha256
$previewFiles = VerifyArchive $Preview $PreviewManifestSha256
$runtime = Join-Path $Capture 'runtime'
# pwsh does not activate the application's ASP.NET shared framework. Resolve the
# installed matching major/minor explicitly and retain every loaded helper's hash.
$runtimeConfig = Get-Content -Raw -LiteralPath (Join-Path $runtime 'BalanceHarness.runtimeconfig.json') | ConvertFrom-Json
$framework = @($runtimeConfig.runtimeOptions.frameworks | Where-Object name -CEQ 'Microsoft.AspNetCore.App')[0]
$wanted = [version]$framework.version
$dotnetRoot = Split-Path -Parent (Get-Command dotnet).Source
$frameworkRoot = Join-Path $dotnetRoot 'shared/Microsoft.AspNetCore.App'
$installed = @(Get-ChildItem -LiteralPath $frameworkRoot -Directory | Where-Object {
    $version = [version]$_.Name
    $version.Major -eq $wanted.Major -and $version.Minor -eq $wanted.Minor -and $version -ge $wanted
} | Sort-Object { [version]$_.Name } -Descending)
Require ($installed.Count -gt 0) 'Matching ASP.NET shared framework is unavailable.'
$frameworkHelpers = @('Primitives', 'FileProviders.Abstractions', 'FileProviders.Physical', 'FileSystemGlobbing',
    'Configuration.Abstractions', 'Configuration', 'Configuration.FileExtensions', 'Configuration.Json')
foreach ($helper in $frameworkHelpers) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $installed[0].FullName ('Microsoft.Extensions.' + $helper + '.dll')))
}
foreach ($name in @('Domain', 'Services.LL', 'BalanceHarness')) {
    Require (-not @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq $name }).Count) 'Use a fresh pwsh process; gameplay assemblies already loaded.'
    [void][Reflection.Assembly]::LoadFrom((Join-Path $runtime ($name + '.dll')))
}
$request = [BalanceHarness.HarnessJson]::Read[System.Text.Json.JsonElement]((Join-Path $Preview 'request.json'))
$context = $request.GetProperty('context')
$mechanics = [System.Text.Json.JsonSerializer]::Deserialize[BalanceHarness.BossGenerationMechanics](
    $context.GetProperty('mechanics').GetRawText(), [BalanceHarness.HarnessJson]::Options)
$scope = [System.Text.Json.JsonSerializer]::Deserialize[BalanceHarness.TowerBossDiscoveryDefinition](
    $context.GetProperty('scope').GetRawText(), [BalanceHarness.HarnessJson]::Options)
$benchmarkReferenceId = $context.GetProperty('benchmarkReferenceId').GetString()
$benchmark = @($scope.Starts | Where-Object ReferenceId -CEQ $benchmarkReferenceId)
Require ($benchmark.Count -eq 1) 'Benchmark reference is ambiguous.'
$party = $benchmark[0].Party
$contentRoot = [string](Join-Path $Capture 'content')
$settings = [BalanceHarness.TowerBundle].GetMethod('ReadSettings', [Reflection.BindingFlags]'Static,NonPublic').Invoke($null, @($contentRoot))
$inventory = [BalanceHarness.TowerBossInventory]::Create($contentRoot, $settings.Threat)
Require ([BalanceHarness.HarnessJson]::Hash[object]($inventory.SourceHashes) -ceq [BalanceHarness.HarnessJson]::Hash[object]($mechanics.SourceHashes)) 'Captured content differs from proposal mechanics.'
Require ([BalanceHarness.HarnessJson]::Hash[object]($inventory.EnablerConsumerPairs) -ceq [BalanceHarness.HarnessJson]::Hash[object]($mechanics.Interactions)) 'Captured inventory does not reproduce proposal interactions.'
Require ([BalanceHarness.HarnessJson]::Hash[object]($inventory.Essences) -ceq [BalanceHarness.HarnessJson]::Hash[object]($mechanics.Essences)) 'Captured inventory does not reproduce proposal essence roots.'

$nodes = @{}
foreach ($node in $inventory.Nodes) { $nodes.Add($node.Key, $node) }
$colocated = @($party.Builds.GetEnumerator() | Sort-Object Key | ForEach-Object {
    $owner = $_.Key; $build = $_.Value
    foreach ($pair in $mechanics.Interactions) {
        if ($build.Contains($pair.EnablerEssenceId) -and $build.Contains($pair.ConsumerEssenceId)) {
            [ordered]@{ owner = $owner; original = $pair; producer = $nodes[$pair.ProducerNodeKey]; consumer = $nodes[$pair.ConsumerNodeKey] }
        }
    }
})
Require ($colocated.Count -eq 3) 'This narrow audit expects the three captured Poison/Viper paths.'
$producerKeys = @(
    'Effect:Status:status.spider_queen.royal_venom/effect.status.spider_queen.royal_venom.poison',
    'Effect:Ability:ability.creature.venomous_spiderling.toxic_opportunity/effect.creature.venomous_spiderling.toxic_opportunity.poison',
    'Effect:Ability:ability.creature.venomous_spiderling.venom_web/effect.creature.venomous_spiderling.venom_web.poison')
foreach ($entry in $colocated) {
    Require ($entry.owner -eq 8 -and $entry.original.ConsumerEssenceId -ceq 'essence.viper' -and
        $entry.original.Mechanism -ceq 'StandardCondition:Poison' -and
        $entry.original.Compatibility -ceq 'recipient-and-trigger-scope-unverified' -and
        $producerKeys.Contains($entry.original.ProducerNodeKey)) 'Unrecognized interaction scope.'
    $effect = [System.Text.Json.JsonSerializer]::Deserialize[Domain.Models.Combat.Abilities.AbilityEffectSpec](
        $entry.producer.Definition.GetRawText(), [BalanceHarness.HarnessJson]::Options)
    $expectedTarget = if ($entry.original.ProducerNodeKey -eq $producerKeys[2]) { 'CurrentTarget' } else { 'EventTarget' }
    Require ($effect.Operation.ToString() -ceq 'ApplyCondition' -and $effect.Condition.ToString() -ceq 'Poison' -and
        $effect.Target.ToString() -ceq $expectedTarget) 'Producer condition or target changed.'
}
$consumerKey = 'Effect:Ability:ability.creature.viper.piercing_fangs/effect.creature.viper.piercing_fangs.poisoned'
Require (@($colocated | Where-Object { $_.original.ConsumerNodeKey -cne $consumerKey }).Count -eq 0) 'Unrecognized consumer.'
$consumer = [System.Text.Json.JsonSerializer]::Deserialize[Domain.Models.Combat.Abilities.AbilityEffectSpec](
    $nodes[$consumerKey].Definition.GetRawText(), [BalanceHarness.HarnessJson]::Options)
Require ($consumer.Conditions.Count -eq 1 -and $consumer.Conditions[0].Type.ToString() -ceq 'HasCondition' -and
    $consumer.Conditions[0].Subject.ToString() -ceq 'Target' -and $consumer.Conditions[0].Condition.ToString() -ceq 'Poison' -and
    $consumer.Target.ToString() -ceq 'CurrentTarget') 'Consumer predicate changed.'
$modifierKey = 'Effect:Ability:ability.creature.viper.potent_toxins/effect.creature.viper.potent_toxins.damage'
$modifier = [System.Text.Json.JsonSerializer]::Deserialize[Domain.Models.Combat.Abilities.AbilityEffectSpec](
    $nodes[$modifierKey].Definition.GetRawText(), [BalanceHarness.HarnessJson]::Options)
Require ($modifier.Operation.ToString() -ceq 'ModifyDamageDealt' -and $modifier.Target.ToString() -ceq 'Self' -and
    $modifier.DamageType.ToString() -ceq 'Poison' -and $modifier.BaseValue -eq 7 -and $modifier.Conditions.Count -eq 0) 'Owner modifier changed.'
Require (@($mechanics.Interactions | Where-Object { $_.ConsumerNodeKey -ceq $modifierKey -and $producerKeys.Contains($_.ProducerNodeKey) }).Count -eq 0) 'Modifier affinity is already present.'
$toxic = [System.Text.Json.JsonSerializer]::Deserialize[Domain.Models.Combat.Abilities.AbilityEffectSpec](
    $nodes[$producerKeys[1]].Definition.GetRawText(), [BalanceHarness.HarnessJson]::Options)
Require ($toxic.Conditions.Count -eq 1 -and $toxic.Conditions[0].Type.ToString() -ceq 'HasCondition' -and
    $toxic.Conditions[0].Subject.ToString() -ceq 'EventTarget' -and $toxic.Conditions[0].Condition.ToString() -ceq 'Slow') 'Toxic Opportunity predicate changed.'
$royalStatus = [System.Text.Json.JsonSerializer]::Deserialize[Domain.Models.Combat.Abilities.StatusSpec](
    $nodes['Status:status.spider_queen.royal_venom'].Definition.GetRawText(), [BalanceHarness.HarnessJson]::Options)
Require ($royalStatus.Triggers.Count -eq 1 -and $royalStatus.Triggers[0].Event.ToString() -ceq 'OnBasicAttack' -and
    $royalStatus.Triggers[0].Conditions.Count -eq 1 -and $royalStatus.Triggers[0].Conditions[0].Type.ToString() -ceq 'EventSourceIsSelf') 'Royal Venom trigger changed.'

# Direct component checks on the archived executable, not on a rebuilt/current engine.
$flags = [Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$engineType = [Services.LL.Combat.Engine.FastCombatEngine]
$conditionPass = $engineType.GetMethod('ConditionPass', $flags)
$triggerRelevant = $engineType.GetMethod('IsSourceScopedTriggerRelevant', $flags)
$periodic = $engineType.GetMethod('ResolvePeriodicCondition', $flags)
$eventConstructor = @($conditionPass.GetParameters()[2].ParameterType.GetConstructors($flags) | Where-Object { $_.GetParameters().Count -gt 1 })[0]
$compileCondition = [Services.LL.Combat.Engine.AbilityCompiler].GetMethod('CompileCondition', $flags)
$compiled = $compileCondition.Invoke($null, @($consumer.Conditions[0]))
$checks = [Collections.Generic.List[object]]::new()
$viper = NewActor 'viper' 'Friendly'; $ally = NewActor 'ally' 'Friendly'
$target = NewActor 'target' 'Hostile'; $other = NewActor 'other' 'Hostile'
$actors = [Services.LL.Combat.Engine.RuntimeCombatant[]]@($viper, $ally, $target, $other)
$engine = NewEngine
$combatEvent = NewEvent $viper $target
Check 'viper_gate_without_poison' ($conditionPass.Invoke($engine, @($compiled, $viper, $combatEvent, $actors, $target))) $false
foreach ($source in @($viper, $ally)) {
    $target.Conditions.Clear()
    $target.Conditions.Add((NewWithDefaults ([Services.LL.Combat.Engine.RuntimeCondition].GetConstructors()[0]) @(
        [Domain.Models.Combat.Abilities.StandardConditionType]::Poison, $source, $target, 1, 120, [single]100, [long]1, 'condition.poison')))
    Check ('viper_gate_poison_from_' + $source.Id) ($conditionPass.Invoke($engine, @($compiled, $viper, $combatEvent, $actors, $target))) $true
}
Check 'poison_on_another_enemy_does_not_enable_gate' ($conditionPass.Invoke($engine, @($compiled, $viper, $combatEvent, $actors, $other))) $false
$target.Conditions.Clear()
$target.Conditions.Add((NewWithDefaults ([Services.LL.Combat.Engine.RuntimeCondition].GetConstructors()[0]) @(
    [Domain.Models.Combat.Abilities.StandardConditionType]::Poison, $ally, $target, 0, 120, [single]100, [long]1, 'condition.poison')))
Check 'zero_poison_stacks_do_not_enable_gate' ($conditionPass.Invoke($engine, @($compiled, $viper, $combatEvent, $actors, $target))) $false
Check 'basic_attack_trigger_accepts_own_event' ($triggerRelevant.Invoke($null, @($viper, $combatEvent))) $true
Check 'basic_attack_trigger_rejects_ally_event' ($triggerRelevant.Invoke($null, @($ally, $combatEvent))) $false
$royalGuard = $compileCondition.Invoke($null, @($royalStatus.Triggers[0].Conditions[0]))
Check 'royal_venom_guard_accepts_own_attack' ($conditionPass.Invoke($engine, @($royalGuard, $viper, $combatEvent, $actors, $target))) $true
Check 'royal_venom_guard_rejects_ally_attack' ($conditionPass.Invoke($engine, @($royalGuard, $ally, $combatEvent, $actors, $target))) $false
$slow = $compileCondition.Invoke($null, @($toxic.Conditions[0]))
Check 'toxic_opportunity_requires_slow_on_event_target' ($conditionPass.Invoke($engine, @($slow, $viper, $combatEvent, $actors, $other))) $false
$target.Conditions.Add((NewWithDefaults ([Services.LL.Combat.Engine.RuntimeCondition].GetConstructors()[0]) @(
    [Domain.Models.Combat.Abilities.StandardConditionType]::Slow, $ally, $target, 1, 100, [single]100, [long]2, 'condition.slow')))
Check 'ally_slow_can_enable_toxic_opportunity' ($conditionPass.Invoke($engine, @($slow, $viper, $combatEvent, $actors, $other))) $true

# Fixed magnitude, no battle loop: a single Poison tick with modifier on source vs ally.
foreach ($placement in @('none', 'source', 'ally')) {
    $source = NewActor 'poison-source' 'Friendly'; $support = NewActor 'support' 'Friendly'; $victim = NewActor 'victim' 'Hostile'
    if ($placement -eq 'source') { $source.AdjustDamageDealt([Domain.Models.Damages.DamageType]::Poison, $modifier.BaseValue) }
    if ($placement -eq 'ally') { $support.AdjustDamageDealt([Domain.Models.Damages.DamageType]::Poison, $modifier.BaseValue) }
    $poison = NewWithDefaults ([Services.LL.Combat.Engine.RuntimeCondition].GetConstructors()[0]) @(
        [Domain.Models.Combat.Abilities.StandardConditionType]::Poison, $source, $victim, 100, 120, [single]100, [long]1, 'condition.poison')
    $before = $victim.Health
    [void]$periodic.Invoke((NewEngine), @($poison, [Services.LL.Combat.Engine.RuntimeCombatant[]]@($source, $support, $victim)))
    Check ('poison_tick_modifier_on_' + $placement) ([int]($before - $victim.Health)) $(if ($placement -eq 'source') { 107 } else { 100 })
}

$methods = @($conditionPass, $triggerRelevant, $periodic, $compileCondition,
    $engineType.GetMethod('ApplyDamage', $flags), $engineType.GetMethod('ResolveSubject', $flags),
    [Services.LL.Combat.Engine.RuntimeCombatant].GetMethod('HasCondition'),
    [Services.LL.Combat.Engine.RuntimeCombatant].GetMethod('GetDamageDealtPercent')) | ForEach-Object {
        [ordered]@{ type = $_.DeclaringType.FullName; signature = $_.ToString(); metadataToken = $_.MetadataToken
            ilSha256 = [Convert]::ToHexStringLower([Security.Cryptography.SHA256]::HashData($_.GetMethodBody().GetILAsByteArray())) }
    }
$loaded = @([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -in @('Domain', 'Services.LL', 'BalanceHarness') } | ForEach-Object {
    Require ($_.Location -ceq (Join-Path $runtime ($_.GetName().Name + '.dll'))) 'Unexpected gameplay assembly location.'
    [ordered]@{ name = $_.GetName().Name; sha256 = FileHash $_.Location; moduleVersionId = $_.ManifestModule.ModuleVersionId.ToString() }
})
$routeKeys = @('Ability:ability.creature.spider_queen.royal_venom', 'Status:status.spider_queen.royal_venom',
    'Ability:ability.creature.venomous_spiderling.toxic_opportunity', 'Ability:ability.creature.venomous_spiderling.venom_web',
    'Ability:ability.creature.viper.piercing_fangs', 'Ability:ability.creature.viper.potent_toxins')
$report = [ordered]@{
    version = 'tower-benchmark-interaction-audit-v1'; status = 'CompleteStructuralReviewOnly'
    captureManifestSha256 = $CaptureManifestSha256; previewManifestSha256 = $PreviewManifestSha256
    requestSha256 = $previewFiles['request.json']; scriptSha256 = FileHash $PSCommandPath
    benchmarkPartyId = $party.Id; rootSeed = $context.GetProperty('rootSeed').GetInt32()
    newFights = 0; nativePreparations = 0; newReservedValues = 0; componentChecks = $checks.ToArray()
    loadedAssemblies = $loaded; capturedMethods = @($methods); sourceHashes = $inventory.SourceHashes
    frameworkHelpers = @($frameworkHelpers | ForEach-Object {
        $file = Join-Path $installed[0].FullName ('Microsoft.Extensions.' + $_ + '.dll')
        [ordered]@{ name = [IO.Path]::GetFileName($file); frameworkVersion = $installed[0].Name; sha256 = FileHash $file }
    })
    colocatedEntries = $colocated; routeDefinitions = @($routeKeys | ForEach-Object { $nodes[$_] })
    existingPairResolution = 'SharedTargetPoisonPredicate; same owner is not required; successful application and timing remain conditional'
    separateMissingMechanism = [ordered]@{
        modifier = $nodes[$modifierKey]; owner = 8; producerKeys = $producerKeys
        scope = 'PoisonDamageSourceOwner'; structuralPairs = 2; producerPaths = 3
        presentInOriginalInteractionList = $false
        interpretation = 'Potent Toxins can amplify Poison credited to its owner; this is distinct from Piercing Fangs reading target Poison'
    }
    decision = 'DoNotRelabelOriginalPairs; use a separately versioned damage-source affinity before defining a preservation arm'
    limitations = @('Component checks establish runtime scope, not combat efficacy or realized uptime.',
        'Ward, target choice, death, cleanse, timing, cooldowns and damage rounding can prevent or erase gains.',
        'The 7 percent authored modifier is not a 7 percent party-damage or win-rate gain.',
        'No captured Services.LL source/PDB is available in this admission; findings use its pinned DLL and literal content.',
        'No generation metadata, policy, live seed history, production setting or scientific result is changed.')
}
[void](VerifyArchive $Capture $CaptureManifestSha256)
[void](VerifyArchive $Preview $PreviewManifestSha256)
# Publication is create-new, after all checks. A racing create also fails rather than overwriting.
[void](New-Item -ItemType Directory -Path $Output -ErrorAction Stop)
[BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Output 'audit.json'), $report)
[IO.File]::Copy($PSCommandPath, (Join-Path $Output 'audit.ps1'), $false)
$manifest = [Collections.Generic.SortedDictionary[string,string]]::new([StringComparer]::Ordinal)
foreach ($file in @('audit.json', 'audit.ps1')) { $manifest.Add($file, (FileHash (Join-Path $Output $file))) }
[BalanceHarness.HarnessJson]::WriteNew[object]((Join-Path $Output 'files.json'), $manifest)
[ordered]@{ status = $report.status; componentChecks = $checks.Count; newFights = 0; newReservedValues = 0
    manifestSha256 = FileHash (Join-Path $Output 'files.json') } | ConvertTo-Json

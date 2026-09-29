param(
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$ReportPin,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() }
function Require([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Close($actual, $expected, [string]$label) {
    Require ([math]::Abs([double]$actual - [double]$expected) -le 1e-8 * [math]::Max(1, [math]::Abs([double]$expected))) $label
}
Require (-not (Test-Path -LiteralPath $Output)) 'Use a new audit receipt path.'
Require ((Hash $Report) -eq $ReportPin.ToLowerInvariant()) 'Report pin mismatch.'
$data = Get-Content -LiteralPath $Report -Raw | ConvertFrom-Json -AsHashtable
Require ($data.version -eq 'tower-acquisition-study-v1') 'Unknown report.'
Require ($data.fights -eq 0 -and $data.reservedSeeds -eq 0 -and $data.measuredPlayerSamples -eq 0) 'Unexpected combat or measured-player claim.'
foreach ($source in $data.sourceHashes.GetEnumerator()) {
    Require ((Hash (Join-Path $repo $source.Key)) -eq $source.Value) "Source changed: $($source.Key)"
}
$production = Join-Path $repo 'LL/src/API/API.LL'
$pools = Get-Content (Join-Path $production 'Data/equipment/equipment-ordinary.v1.json') -Raw | ConvertFrom-Json -AsHashtable
$pool = @($pools | Where-Object region -eq 1)[0]
$assembly = Get-Content (Join-Path $production 'Data/dungeons/sigil-assembly.json') -Raw | ConvertFrom-Json -AsHashtable
$settings = Get-Content (Join-Path $production 'appsettings.json') -Raw | ConvertFrom-Json -AsHashtable
$prices = Get-Content (Join-Path $production 'Data/equipment/equipment-upgrades.v1.json') -Raw | ConvertFrom-Json -AsHashtable
$supplies = Get-Content (Join-Path $production 'Data/equipment/tower-equipment-supplies.v1.json') -Raw | ConvertFrom-Json -AsHashtable
$prophecies = Get-Content (Join-Path $production 'Data/prophecies/rewards.json') -Raw | ConvertFrom-Json -AsHashtable
$cadence = $settings.Combat.IdleProgression.EncounterCadenceSeconds
$checkedEfforts = 0
foreach ($economy in $data.economy) {
    $pace = $economy.assumptions
    $fragments = 0
    if ($null -ne $pace.dailyProphecyProfile) {
        $profile = @($prophecies.profiles | Where-Object id -eq $pace.dailyProphecyProfile)
        Require ($profile.Count -eq 1 -and $profile[0].scope -eq 'Daily') 'Invalid daily profile.'
        $fragments = $profile[0].flatReward.sigilFragments * $pace.dailyProphecyClaims
    }
    $efforts = @()
    foreach ($requirement in $economy.requirements) {
        $efforts += @{ value = $requirement.allRegionalSigils; split = 1 }
        $efforts += @{ value = $requirement.goblinOnly; split = $pool.sigils.Count }
    }
    foreach ($entry in @($economy.perOwnerLifetime) + @($economy.perOwnerPreselectedReferencePath)) {
        $efforts += @{ value = $entry.effort; split = 1 }
    }
    foreach ($entry in $efforts) {
        $effort = $entry.value
        $successes = $effort.successfulCharacterCompletions
        $attempts = $successes / $pace.dungeonSuccessProbability
        $failures = $attempts - $successes
        $sigilsPerHour = 3600 / $cadence * $pace.idleVictoryProbability * $pool.sigilDropChance / $entry.split
        Close $effort.expectedAttempts $attempts 'Attempt expectation differs.'
        Close $effort.expectedFailures $failures 'Failure expectation differs.'
        Close ($effort.expectedEntryItems.Values | Measure-Object -Sum).Sum $attempts 'Entry costs differ.'
        Close $effort.assemblyOnlyFragments ($attempts * $assembly.fragmentCost) 'Assembly demand differs.'
        Close $effort.randomSigilOnlyIdleHours ($attempts / $sigilsPerHour) 'Idle hours differ.'
        Close $effort.dungeonActiveHours (($successes * $pace.successfulRunMinutes + $failures * $pace.failedRunMinutes) / 60) 'Dungeon duration differs.'
        if ($fragments -gt 0) { Close $effort.fragmentOnlyRewardDays ($attempts * $assembly.fragmentCost / $fragments) 'Fragment budget days differ.' }
        else { Require ($null -eq $effort.fragmentOnlyRewardDays) 'Unfunded fragments cannot produce days.' }
        $combined = $sigilsPerHour * $pace.eligibleIdleHoursPerDay + $fragments / $assembly.fragmentCost
        if ($combined -gt 0) { Close $effort.combinedSupplyRateDays ($attempts / $combined) 'Combined rate budget differs.' }
        $checkedEfforts++
    }
}
function CheckInventory($items, $floorRows) {
    $byId = @{}
    $ownerOrdinals = @{}
    foreach ($item in $items) {
        $state = $item.data.state
        Require (-not $byId.ContainsKey($state.id)) 'Duplicate earned identity.'
        $byId[$state.id] = $item
        $ordinalKey = "$($item.ownerKey)/$($item.successfulCompletionOrdinal)"
        Require (-not $ownerOrdinals.ContainsKey($ordinalKey)) 'Duplicated personal completion.'
        $ownerOrdinals[$ordinalKey] = $true
        Require ($state.ownership.kind -eq 'BoundPersonal') 'Unbound supply gear.'
        $supply = @($supplies | Where-Object itemBaseId -eq $item.supplyId)[0]
        Require ($supply.targetFloor -le $item.earnedBeforeFloor -and $state.tier -eq $supply.tier) 'Supply gate or tier mismatch.'
        $position = ($supply.targetFloor - 1) % 10 + 1
        $band = if ($position -le 3) { @('Rare', 'Standard', 2) } elseif ($position -le 6) { @('Epic', 'Fine', 3) } elseif ($position -le 9) { @('Unique', 'Exceptional', 4) } else { @('Legendary', 'Masterpiece', 5) }
        Require ($item.data.rarity -eq $band[0] -and $state.quality -eq $band[1] -and $state.rank -eq $band[2]) 'Repeating band mismatch.'
        Require ($state.attributeRollMultiplier -eq 1 -and $null -eq $state.activeStyleId) 'Supply roll/style mismatch.'
    }
    foreach ($row in $floorRows) {
        Require ($row.members.Count -eq $row.partySize) 'Party size mismatch.'
        $newCount = 0
        $retainedCount = 0
        foreach ($member in $row.members) {
            $memberNew = 0
            foreach ($id in $member.equippedItemIds) {
                Require ($byId.ContainsKey($id)) 'Equipped item was not earned.'
                $item = $byId[$id]
                Require ($item.ownerKey -eq $member.ownerKey -and $item.data.state.ownership.ownerId -eq $member.ownerId) 'Bound donation detected.'
                Require ($item.earnedBeforeFloor -le $row.floor) 'Equipment borrowed from the future.'
                if ($item.earnedBeforeFloor -eq $row.floor) { $memberNew++ } else { $retainedCount++ }
            }
            Require ($memberNew -eq $member.newItems) 'Personal incremental bill differs.'
            $newCount += $memberNew
        }
        Require ($newCount -eq $row.newItems -and $retainedCount -eq $row.retainedItems) 'Incremental party bill differs.'
        Require (@($items | Where-Object earnedBeforeFloor -le $row.floor).Count -eq $row.cumulativeEarnedItems) 'Cumulative bill differs.'
    }
}
CheckInventory $data.earnedInventory $data.floors
CheckInventory $data.preselectedReferencePath.inventory $data.preselectedReferencePath.floors
$f10 = @($data.floors | Where-Object floor -eq 10)[0]
$f11 = @($data.floors | Where-Object floor -eq 11)[0]
Require ($f11.newItems -eq 0) 'Unexpected floor-11 replacement bill.'
foreach ($member in $f11.members) {
    $previous = @($f10.members | Where-Object ownerKey -eq $member.ownerKey)[0]
    Require (($previous.equippedItemIds -join ',') -eq ($member.equippedItemIds -join ',')) 'Carried item identities differ.'
}
Require ($data.floor11AllNewcomers.inventory.Count -eq 70) 'Newcomers need their own full loadouts.'
foreach ($item in $data.floor11AllNewcomers.inventory) { Require ($item.supplyId -eq 'item.tower_supply.v1.floor_10') 'Newcomer old-region source missing.' }
foreach ($row in $data.dismantling.repeatFarm) {
    $tierPrices = @($prices.tiers | Where-Object tier -eq $row.tier)[0]
    $invested = ($tierPrices.rankPartCosts | Select-Object -First $row.band.rank | Measure-Object -Sum).Sum
    $perSlot = $tierPrices.baseDismantleParts + [math]::Floor($invested * $prices.dismantleRankRecovery)
    Close $row.oneSlotPartsPerChest $perSlot 'Dismantling parts differ.'
    Close $row.twoHandedPartsPerChest (2 * $perSlot) 'Two-handed dismantling differs.'
}
Require ($data.dismantling.creditedParts -eq 0 -and $data.dismantling.actuallyDismantled -eq 0) 'Retained equipment was spent.'
foreach ($reference in $data.references) {
    $archive = Join-Path $repo $reference.reference.archive
    Require ((Hash (Join-Path $archive 'files.json')) -eq $reference.reference.manifestPin) 'Historical manifest mismatch.'
    $manifest = Get-Content (Join-Path $archive 'files.json') -Raw | ConvertFrom-Json -AsHashtable
    Require ((Hash (Join-Path $archive 'cells.json')) -eq $manifest['cells.json']) 'Historical cells mismatch.'
    Require ($reference.exactCombatEquipmentDescriptors) 'Native equipment parity did not pass.'
    # Production descriptor/preparation parity is tested natively; this audit independently
    # checks accounting and consumed historical bytes, not a second combat implementation.
}
$receipt = [ordered]@{
    status = 'VerifiedAcquisitionAccounting'; report = [IO.Path]::GetFullPath($Report); reportPin = $ReportPin;
    verifierHash = Hash $PSCommandPath; checkedSourceHashes = $data.sourceHashes.Count;
    checkedEfforts = $checkedEfforts; earnedItems = $data.earnedInventory.Count;
    referencePathItems = $data.preselectedReferencePath.inventory.Count;
    fights = 0; reservedSeeds = 0; measuredPlayerSamples = 0;
    scope = 'Independent source/ledger/economy checks. Native production preparation and descriptor parity remain separate evidence. No pace acceptance, fresh combat or full historical-archive reconstruction.'
}
$bytes = [Text.Encoding]::UTF8.GetBytes(($receipt | ConvertTo-Json -Depth 5))
$stream = [IO.File]::Open([IO.Path]::GetFullPath($Output), [IO.FileMode]::CreateNew)
try { $stream.Write($bytes) } finally { $stream.Dispose() }
$receipt | ConvertTo-Json -Depth 5

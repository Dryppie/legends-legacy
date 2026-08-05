param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

function Read-Json([string]$RelativePath) {
    Get-Content -Raw -LiteralPath (Join-Path $RepositoryRoot $RelativePath) | ConvertFrom-Json
}

function Write-Json([string]$RelativePath, [object]$Value, [int]$Depth = 30) {
    ConvertTo-Json -InputObject $Value -Depth $Depth |
        Set-Content -LiteralPath (Join-Path $RepositoryRoot $RelativePath) -Encoding utf8
}

$profileDocument = Read-Json 'LL/src/API/API.LL/Data/combat/creature-abilities.json'
$creatureDocument = Read-Json 'LL/src/API/API.LL/Data/world/creatures.json'
$abilities = @(Read-Json 'LL/src/API/API.LL/Data/combat/abilities.json')
$creaturesByKey = @{}
foreach ($creature in $creatureDocument.creatures) { $creaturesByKey[$creature.imagePath] = $creature }

$essences = [System.Collections.Generic.List[object]]::new()
$lootOwners = [System.Collections.Generic.List[object]]::new()
$essenceItems = [System.Collections.Generic.List[object]]::new()

foreach ($profile in $profileDocument.creatures) {
    $slug = $profile.monsterId.Substring('monster.'.Length)
    $creature = $creaturesByKey[$slug]
    if ($null -eq $creature) { throw "Missing roster creature '$slug'." }

    $profileAbilities = @($profile.abilityIds | ForEach-Object {
        $abilityId = $_
        $abilities | Where-Object id -eq $abilityId | Select-Object -First 1
    })
    $passive = $profileAbilities | Where-Object kind -eq 'Passive' | Select-Object -First 1
    $actives = @($profileAbilities | Where-Object kind -eq 'Active')
    if ($null -eq $passive -or $actives.Count -eq 0) {
        throw "Creature '$slug' requires at least one active and one passive ability."
    }

    $variants = [System.Collections.Generic.List[object]]::new()
    for ($index = 0; $index -lt $actives.Count; $index++) {
        $active = $actives[$index]
        $variantSuffix = if ($index -eq 0) { $slug } else { "$slug`_$($active.id.Split('.')[-1])" }
        $essenceId = "essence.$variantSuffix"
        $displayName = if ($index -eq 0) {
            "$($creature.name) Essence"
        }
        else {
            "$($creature.name): $($active.name) Essence"
        }

        $essences.Add([pscustomobject][ordered]@{
            id = $essenceId
            sourceMonsterId = $profile.monsterId
            name = $displayName
            description = "The combat essence of $($creature.name), granting $($active.name) and $($passive.name)."
            nativeRegion = 1
            rarity = $(if ($slug -eq 'hobgoblin') { 'Rare' } else { 'Common' })
            tags = @()
            attributeBonuses = @()
            activeAbilityId = $active.id
            passiveAbilityId = $passive.id
            evolution = [pscustomobject][ordered]@{
                id = "evolution.$variantSuffix"
                name = "Awakened $($creature.name)"
                description = "The awakened form of $displayName."
                requiredAscensionTier = 2
                requiredCatalystItemId = ''
                addsTags = @()
                attributeModifierChanges = @()
                activeAbilityModifiers = @()
                passiveAbilityModifiers = @()
            }
        })

        $active | Add-Member -NotePropertyName owningEssenceId -NotePropertyValue $essenceId -Force
        if ($index -eq 0) {
            $passive | Add-Member -NotePropertyName owningEssenceId -NotePropertyValue $essenceId -Force
        }

        $variants.Add([pscustomobject][ordered]@{
            essenceDefinitionId = $essenceId
            activeAbilityId = $active.id
            weight = 1
        })

        $essenceItems.Add([pscustomobject][ordered]@{
            id = "item.$essenceId"
            name = "Unbound $displayName"
            description = "A tradable Essence that can be absorbed into the Soul Archive or dismantled into Soul Dust."
            itemType = 'Essence'
            rarity = $(if ($slug -eq 'hobgoblin') { 'Rare' } else { 'Common' })
            equipmentType = 'Head'
            attributeModifiers = @()
            toolBonuses = @()
            gatheringType = $null
        })
    }

    $lootOwners.Add([pscustomobject][ordered]@{
        id = $profile.monsterId
        essenceLootTable = [pscustomobject][ordered]@{
            baseDropChance = 0.0001
            passiveAbilityId = $passive.id
            variants = @($variants)
        }
    })
}

Write-Json 'LL/src/API/API.LL/Data/essences/essences.json' ([pscustomobject][ordered]@{ essences = @($essences) })
Write-Json 'LL/src/API/API.LL/Data/world/creature-essence-loot-tables.json' ([pscustomobject][ordered]@{ creatures = @($lootOwners) })
Write-Json 'LL/src/API/API.LL/Data/combat/abilities.json' $abilities

$items = @(Read-Json 'LL/src/API/API.LL/Data/items/items.json')
$items = @($items | Where-Object itemType -ne 'Essence') + @($essenceItems)
Write-Json 'LL/src/API/API.LL/Data/items/items.json' $items

$tutorial = Read-Json 'LL/src/API/API.LL/Data/tutorials/first-steps.v1.json'
$remainingSteps = @($tutorial.steps | Where-Object key -notin @('absorb_essence', 'equip_essence'))
($remainingSteps | Where-Object key -eq 'defeat_training_creature').nextStepKey = 'absorb_essence'
$essenceSteps = @(
    [pscustomobject][ordered]@{
        key = 'absorb_essence'; objective = "Absorb the Unbound Goblin Essence into your Soul Archive."
        requiredAmount = 1; actionLabel = 'Open Essences'; destinationRoute = '/game/character/essences'
        tourPageId = 'tutorial-essence-absorb'; guidePageId = 'tutorial-essence-absorb'
        trigger = [pscustomobject][ordered]@{ type = 'EssenceAbsorbed'; essenceDefinitionId = 'essence.goblin' }
        nextStepKey = 'equip_essence'
    },
    [pscustomobject][ordered]@{
        key = 'equip_essence'; objective = "Attune the Goblin Essence in your active Essence loadout."
        requiredAmount = 1; actionLabel = 'Open Loadout'; destinationRoute = '/game/character/essences'
        tourPageId = 'tutorial-essence-loadout'; guidePageId = 'tutorial-essence-loadout'
        trigger = [pscustomobject][ordered]@{ type = 'EssenceLoadoutChanged'; essenceDefinitionId = 'essence.goblin' }
        nextStepKey = 'craft_equipment'
    }
)
$tutorial.steps = @($remainingSteps[0]) + $essenceSteps + @($remainingSteps | Select-Object -Skip 1)
Write-Json 'LL/src/API/API.LL/Data/tutorials/first-steps.v1.json' $tutorial

Write-Output "Generated $($essences.Count) Essences and $($lootOwners.Count) roster loot tables."

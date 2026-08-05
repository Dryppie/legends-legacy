param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

function Read-JsonArray([string]$Path) {
    $parsed = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json
    $result = foreach ($item in @($parsed)) {
        if (($item.PSObject.Properties.Name -contains 'value') -and ($item.PSObject.Properties.Name -contains 'Count') -and ($item.PSObject.Properties.Name -notcontains 'id')) {
            @($item.value)
        }
        else {
            $item
        }
    }

    return @($result)
}

function New-Condition {
    param(
        [string]$Type,
        [string]$Subject = 'Target',
        [int]$Value = 0,
        [string]$Condition,
        [string]$StatusId,
        [string]$DamageType,
        [string]$AttackType
    )
    $result = [ordered]@{ type = $Type; subject = $Subject }
    if ($Value -ne 0) { $result.value = $Value }
    if ($Condition) { $result.condition = $Condition }
    if ($StatusId) { $result.statusId = $StatusId }
    if ($DamageType) { $result.damageType = $DamageType }
    if ($AttackType) { $result.attackType = $AttackType }
    [pscustomobject]$result
}

function New-Trigger {
    param(
        [string]$Event,
        [string[]]$EffectIds,
        [int]$Cooldown = 0,
        [int]$Delay = 0,
        [int]$Every = 1,
        [object[]]$Conditions = @()
    )
    $result = [ordered]@{ event = $Event; effectIds = @($EffectIds) }
    if ($Cooldown -gt 0) { $result.internalCooldownTicks = $Cooldown }
    if ($Delay -gt 0) { $result.initialDelayTicks = $Delay }
    if ($Every -gt 1) { $result.everyNthOccurrence = $Every }
    if ($Conditions.Count -gt 0) { $result.conditions = @($Conditions) }
    [pscustomobject]$result
}

function New-Effect {
    param(
        [string]$Id,
        [string]$Operation,
        [string]$Target = 'CurrentTarget',
        [int]$BaseValue = 0,
        [string]$ScalingAttribute,
        [double]$ScalingCoefficient = 0,
        [double]$MaximumScalingCoefficient = 0,
        [double]$EventMagnitudeCoefficient = 0,
        [string]$ScalingCondition,
        [double]$ConditionScalingCoefficient = 0,
        [string]$ScalingStatusId,
        [double]$StatusScalingCoefficient = 0,
        [string]$Attribute,
        [string]$Condition,
        [string]$AlternativeCondition,
        [string]$StatusId,
        [string]$SummonId,
        [int]$Duration = 0,
        [int]$Interval = 0,
        [int]$Uses = 0,
        [int]$Chance = 100,
        [string]$AttackType = 'None',
        [string]$DamageType = 'None',
        [string]$CritEligibility = 'Default',
        [double]$CritChanceBonus = 0,
        [double]$ArmorPenetrationBonus = 0,
        [double]$LifeStealPercentage = 0,
        [string[]]$Tags = @(),
        [object[]]$Conditions = @()
    )
    $result = [ordered]@{ id = $Id; operation = $Operation; target = $Target }
    if ($BaseValue -ne 0) { $result.baseValue = $BaseValue }
    if ($ScalingAttribute) { $result.scalingAttribute = $ScalingAttribute }
    if ($ScalingCoefficient -ne 0) { $result.scalingCoefficient = $ScalingCoefficient }
    if ($MaximumScalingCoefficient -ne 0) { $result.maximumScalingCoefficient = $MaximumScalingCoefficient }
    if ($EventMagnitudeCoefficient -ne 0) { $result.eventMagnitudeCoefficient = $EventMagnitudeCoefficient }
    if ($ScalingCondition) { $result.scalingCondition = $ScalingCondition }
    if ($ConditionScalingCoefficient -ne 0) { $result.conditionScalingCoefficient = $ConditionScalingCoefficient }
    if ($ScalingStatusId) { $result.scalingStatusId = $ScalingStatusId }
    if ($StatusScalingCoefficient -ne 0) { $result.statusScalingCoefficient = $StatusScalingCoefficient }
    if ($Attribute) { $result.attribute = $Attribute }
    if ($Condition) { $result.condition = $Condition }
    if ($AlternativeCondition) { $result.alternativeCondition = $AlternativeCondition }
    if ($StatusId) { $result.statusId = $StatusId }
    if ($SummonId) { $result.summonId = $SummonId }
    if ($Duration -gt 0) { $result.durationTicks = $Duration }
    if ($Interval -gt 0) { $result.intervalTicks = $Interval }
    if ($Uses -gt 0) { $result.uses = $Uses }
    if ($Chance -ne 100) { $result.chancePercent = $Chance }
    if ($AttackType -ne 'None') { $result.attackType = $AttackType }
    if ($DamageType -ne 'None') { $result.damageType = $DamageType }
    if ($CritEligibility -ne 'Default') { $result.critEligibility = $CritEligibility }
    if ($CritChanceBonus -ne 0) { $result.critChanceBonus = $CritChanceBonus }
    if ($ArmorPenetrationBonus -ne 0) { $result.armorPenetrationBonus = $ArmorPenetrationBonus }
    if ($LifeStealPercentage -ne 0) { $result.lifeStealPercentage = $LifeStealPercentage }
    if ($Tags.Count -gt 0) { $result.tags = @($Tags) }
    if ($Conditions.Count -gt 0) { $result.conditions = @($Conditions) }
    $result.procCoefficient = 1
    [pscustomobject]$result
}

function New-Ability {
    param(
        [string]$Creature,
        [string]$Slug,
        [string]$AbilitySlug,
        [string]$Kind,
        [string]$Name,
        [string]$Description,
        [object[]]$Effects,
        [object[]]$Triggers = @(),
        [int]$Cooldown = 100
    )
    $result = [ordered]@{
        id = "ability.creature.$Slug.$AbilitySlug"
        kind = $Kind
        name = $Name
        description = $Description
        cooldownTicks = $(if ($Kind -eq 'Active') { $Cooldown } else { 0 })
        tags = @('Effect.Ability', "Creature.$($Creature.Replace(' ', ''))")
    }
    if ($Triggers.Count -gt 0) { $result.triggers = @($Triggers) }
    $result.effects = @($Effects)
    [pscustomobject]$result
}

$abilities = [System.Collections.Generic.List[object]]::new()
$profiles = [System.Collections.Generic.List[object]]::new()

function Add-Creature {
    param([string]$Name, [string]$Slug, [object[]]$CreatureAbilities)
    foreach ($ability in $CreatureAbilities) { $abilities.Add($ability) }
    $profiles.Add([pscustomobject][ordered]@{
        monsterId = "monster.$Slug"
        abilityIds = @($CreatureAbilities.id)
    })
}

$selfBelow20 = @(New-Condition HealthBelowPercent Source 20)
$selfAbove50 = @(New-Condition HealthAbovePercent Source 50)
$selfAbove66 = @(New-Condition HealthAbovePercent Source 66)
$selfBelow33 = @(New-Condition HealthBelowPercent Source 33)
$eventTargetPoison = @(New-Condition HasCondition EventTarget 0 Poison)
$eventTargetSlow = @(New-Condition HasCondition EventTarget 0 Slow)

Add-Creature 'Vampire Bat' 'vampire_bat' @(
    (New-Ability 'Vampire Bat' 'vampire_bat' 'bloodthirsty_fangs' Active 'Bloodthirsty Fangs' 'Deal 110% Physical Damage to the target and heal for 50% of damage dealt.' @(
        (New-Effect 'effect.creature.vampire_bat.bloodthirsty_fangs.damage' Damage CurrentTarget 0 Power 1.10 -AttackType Melee -DamageType Physical -LifeStealPercentage 50)
    )),
    (New-Ability 'Vampire Bat' 'vampire_bat' 'erratic_flight' Passive 'Erratic Flight' 'Gain 5% Dodge while below 20% Health.' @(
        (New-Effect 'effect.creature.vampire_bat.erratic_flight.dodge' ModifyAttribute Self 5 -Attribute DodgeChance -Duration 1)
    ) @((New-Trigger OnInterval @('effect.creature.vampire_bat.erratic_flight.dodge') -Cooldown 1 -Conditions $selfBelow20)))
)

Add-Creature 'Raven' 'raven' @(
    (New-Ability 'Raven' 'raven' 'piercing_peck' Active 'Piercing Peck' 'Strike a random enemy for 150% Physical Damage.' @(
        (New-Effect 'effect.creature.raven.piercing_peck.damage' Damage RandomEnemy 0 Power 1.50 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Raven' 'raven' 'bad_omen' Passive 'Bad Omen' 'At combat start, apply Vulnerable(2) to all enemies.' @(
        (New-Effect 'effect.creature.raven.bad_omen.vulnerable' ApplyCondition AllEnemies 2 -Condition Vulnerable)
    ))
)

Add-Creature 'Venomous Snake' 'venomous_snake' @(
    (New-Ability 'Venomous Snake' 'venomous_snake' 'toxic_fangs' Active 'Toxic Fangs' 'Deal 75% Physical Damage and apply Poison(15).' @(
        (New-Effect 'effect.creature.venomous_snake.toxic_fangs.damage' Damage CurrentTarget 0 Power 0.75 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.venomous_snake.toxic_fangs.poison' ApplyCondition CurrentTarget 15 -Condition Poison)
    )),
    (New-Ability 'Venomous Snake' 'venomous_snake' 'venom_coated_strikes' Passive 'Venom-Coated Strikes' 'Basic attacks have a 12% chance to apply Poison(10).' @(
        (New-Effect 'effect.creature.venomous_snake.venom_coated_strikes.poison' ApplyCondition EventTarget 10 -Condition Poison -Chance 12)
    ) @((New-Trigger OnBasicAttack @('effect.creature.venomous_snake.venom_coated_strikes.poison'))))
)

Add-Creature 'Nightshade Blossom' 'nightshade_blossom' @(
    (New-Ability 'Nightshade Blossom' 'nightshade_blossom' 'withering_petals' Active 'Withering Petals' 'Deal 90% Magical Damage to a random enemy and apply Weaken.' @(
        (New-Effect 'effect.creature.nightshade_blossom.withering_petals.damage' Damage RandomEnemy 0 Power 0.90 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.nightshade_blossom.withering_petals.weaken' ApplyCondition RandomEnemy 1 -Condition Weaken)
    )),
    (New-Ability 'Nightshade Blossom' 'nightshade_blossom' 'midnight_bloom' Passive 'Midnight Bloom' 'Every 12 seconds, heal yourself for 50% Power.' @(
        (New-Effect 'effect.creature.nightshade_blossom.midnight_bloom.heal' Heal Self 0 Power 0.50)
    ) @((New-Trigger OnInterval @('effect.creature.nightshade_blossom.midnight_bloom.heal') -Cooldown 120 -Delay 120)))
)

Add-Creature 'Blood Zombie' 'blood_zombie' @(
    (New-Ability 'Blood Zombie' 'blood_zombie' 'rending_bite' Active 'Rending Bite' 'Deal 90% Physical Damage to a random enemy and apply Bleed(20).' @(
        (New-Effect 'effect.creature.blood_zombie.rending_bite.damage' Damage RandomEnemy 0 Power 0.90 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.blood_zombie.rending_bite.bleed' ApplyCondition RandomEnemy 20 -Condition Bleed)
    )),
    (New-Ability 'Blood Zombie' 'blood_zombie' 'clotted_flesh' Passive 'Clotted Flesh' 'Take 10% less Physical Damage while above 50% Health.' @(
        (New-Effect 'effect.creature.blood_zombie.clotted_flesh.defense' ModifyDamageTaken Self -10 -DamageType Physical -Duration 1)
    ) @((New-Trigger OnInterval @('effect.creature.blood_zombie.clotted_flesh.defense') -Cooldown 1 -Conditions $selfAbove50)))
)

Add-Creature 'Lumo Wisp' 'lumo_wisp' @(
    (New-Ability 'Lumo Wisp' 'lumo_wisp' 'soothing_glow' Active 'Soothing Glow' 'Heal the lowest-health ally for 80% Power.' @(
        (New-Effect 'effect.creature.lumo_wisp.soothing_glow.heal' Heal LowestHealthAlly 0 Power 0.80)
    )),
    (New-Ability 'Lumo Wisp' 'lumo_wisp' 'lumo_barrier' Passive 'Lumo Barrier' 'At combat start, gain Barrier equal to 9% Max Health.' @(
        (New-Effect 'effect.creature.lumo_wisp.lumo_barrier.barrier' GrantBarrier Self 0 MaxHealth 0.09)
    ))
)

Add-Creature 'Lumo Sentinel' 'lumo_sentinel' @(
    (New-Ability 'Lumo Sentinel' 'lumo_sentinel' 'targeting_beam' Active 'Targeting Beam' 'Deal 160% Magical Damage to a random enemy.' @(
        (New-Effect 'effect.creature.lumo_sentinel.targeting_beam.damage' Damage RandomEnemy 0 Power 1.60 -AttackType Ranged -DamageType Magical)
    )),
    (New-Ability 'Lumo Sentinel' 'lumo_sentinel' 'cracked_core' Passive 'Cracked Core' 'Gain 30% Magical Defense above 66% Health; lose 15% below 33% Health.' @(
        (New-Effect 'effect.creature.lumo_sentinel.cracked_core.high' ModifyAttribute Self 0 Resistance 0.30 -Attribute Resistance -Duration 1 -Conditions $selfAbove66),
        (New-Effect 'effect.creature.lumo_sentinel.cracked_core.low' ModifyAttribute Self 0 Resistance -0.15 -Attribute Resistance -Duration 1 -Conditions $selfBelow33)
    ) @((New-Trigger OnInterval @('effect.creature.lumo_sentinel.cracked_core.high','effect.creature.lumo_sentinel.cracked_core.low') -Cooldown 1)))
)

Add-Creature 'Goblin' 'goblin' @(
    (New-Ability 'Goblin' 'goblin' 'shiv_jab' Active 'Shiv Jab' 'Deal 100% Physical Damage to a random enemy and apply Bleed(10).' @(
        (New-Effect 'effect.creature.goblin.shiv_jab.damage' Damage RandomEnemy 0 Power 1.00 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.goblin.shiv_jab.bleed' ApplyCondition RandomEnemy 10 -Condition Bleed)
    )),
    (New-Ability 'Goblin' 'goblin' 'gobber_trapper' Passive 'Gobber Trapper' 'The first time you dodge, apply Bleed(70) to the attacker.' @(
        (New-Effect 'effect.creature.goblin.gobber_trapper.bleed' ApplyCondition EventTarget 70 -Condition Bleed -Uses 1)
    ) @((New-Trigger OnDodge @('effect.creature.goblin.gobber_trapper.bleed'))))
)

Add-Creature 'Goblin Archer' 'goblin_archer' @(
    (New-Ability 'Goblin Archer' 'goblin_archer' 'snipers_strike' Active "Sniper's Strike" 'Deal 100% ranged Physical Damage with +50% Critical Chance. Authored cost: 22 (resource unspecified).' @(
        (New-Effect 'effect.creature.goblin_archer.snipers_strike.damage' Damage RandomEnemy 0 Power 1.00 -AttackType Ranged -DamageType Physical -CritChanceBonus 50)
    ) -Cooldown 250),
    (New-Ability 'Goblin Archer' 'goblin_archer' 'poisoned_arrows' Passive 'Poisoned Arrows' 'Ranged attacks have a 10% chance to apply Poison(10).' @(
        (New-Effect 'effect.creature.goblin_archer.poisoned_arrows.poison' ApplyCondition EventTarget 10 -Condition Poison -Chance 10)
    ) @((New-Trigger OnRangedAttack @('effect.creature.goblin_archer.poisoned_arrows.poison'))))
)

Add-Creature 'Goblin Warrior' 'goblin_warrior' @(
    (New-Ability 'Goblin Warrior' 'goblin_warrior' 'raging_cleave' Active 'Raging Cleave' 'Deal 150% Physical Damage to two enemies.' @(
        (New-Effect 'effect.creature.goblin_warrior.raging_cleave.damage' Damage TwoEnemies 0 Power 1.50 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Goblin Warrior' 'goblin_warrior' 'relentless' Passive 'Relentless' 'Every third Basic Attack deals 40% increased damage.' @(
        (New-Effect 'effect.creature.goblin_warrior.relentless.damage' ModifyNextBasicAttackDamage Self 40)
    ) @((New-Trigger OnBasicAttack @('effect.creature.goblin_warrior.relentless.damage') -Every 3)))
)

Add-Creature 'Goblin Shaman' 'goblin_shaman' @(
    (New-Ability 'Goblin Shaman' 'goblin_shaman' 'weak_rejuvenation' Active 'Weak Rejuvenation' 'Heal the lowest-health ally for 35% Power every 4 seconds for 20 seconds.' @(
        (New-Effect 'effect.creature.goblin_shaman.weak_rejuvenation.heal' Heal LowestHealthAlly 0 Power 0.35 -Duration 200 -Interval 40)
    )),
    (New-Ability 'Goblin Shaman' 'goblin_shaman' 'spirit_link' Passive 'Spirit Link' 'After 15 seconds, apply Recovery(20) to all allies.' @(
        (New-Effect 'effect.creature.goblin_shaman.spirit_link.recovery' ApplyCondition AllAllies 20 -Condition Recovery)
    ) @((New-Trigger OnInterval @('effect.creature.goblin_shaman.spirit_link.recovery') -Cooldown 100000 -Delay 150)))
)

$hobgoblinPassive = New-Ability 'Hobgoblin' 'hobgoblin' 'threatening_presence' Passive 'Threatening Presence' 'At combat start gain 50 Threat and take 10% less damage from Vulnerable enemies.' @(
    (New-Effect 'effect.creature.hobgoblin.threatening_presence.threat' ModifyThreat Self 50),
    (New-Effect 'effect.creature.hobgoblin.threatening_presence.vulnerable' ModifyDamageTakenFromCondition Self -10 -Condition Vulnerable)
)
Add-Creature 'Hobgoblin' 'hobgoblin' @(
    (New-Ability 'Hobgoblin' 'hobgoblin' 'intimidating_slam' Active 'Intimidating Slam' 'Deal 180% Physical Damage and apply Vulnerable(2).' @(
        (New-Effect 'effect.creature.hobgoblin.intimidating_slam.damage' Damage CurrentTarget 0 Power 1.80 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.hobgoblin.intimidating_slam.vulnerable' ApplyCondition CurrentTarget 2 -Condition Vulnerable)
    )),
    (New-Ability 'Hobgoblin' 'hobgoblin' 'brutal_charge' Active 'Brutal Charge' 'Gain Unstoppable(3), deal 220% Physical Damage, and Stun(1) if the target is below 30% Health.' @(
        (New-Effect 'effect.creature.hobgoblin.brutal_charge.unstoppable' ApplyCondition Self 3 -Condition Unstoppable),
        (New-Effect 'effect.creature.hobgoblin.brutal_charge.damage' Damage CurrentTarget 0 Power 2.20 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.hobgoblin.brutal_charge.stun' ApplyCondition CurrentTarget 1 -Condition Stun -Conditions @((New-Condition HealthBelowPercent Target 30)))
    )),
    $hobgoblinPassive
)

Add-Creature 'Frost Imp' 'frost_imp' @(
    (New-Ability 'Frost Imp' 'frost_imp' 'ice_needle' Active 'Ice Needle' 'Deal 160% Magical Damage to a random enemy.' @(
        (New-Effect 'effect.creature.frost_imp.ice_needle.damage' Damage RandomEnemy 0 Power 1.60 -AttackType Ranged -DamageType Magical)
    )),
    (New-Ability 'Frost Imp' 'frost_imp' 'cold_touch' Passive 'Cold Touch' 'Basic Attacks apply Chill(1).' @(
        (New-Effect 'effect.creature.frost_imp.cold_touch.chill' ApplyCondition EventTarget 1 -Condition Chill)
    ) @((New-Trigger OnBasicAttack @('effect.creature.frost_imp.cold_touch.chill'))))
)

Add-Creature 'Crystal Wisp' 'crystal_wisp' @(
    (New-Ability 'Crystal Wisp' 'crystal_wisp' 'prismatic_shard' Active 'Prismatic Shard' 'Deal 50-250% Magical Damage to a random enemy.' @(
        (New-Effect 'effect.creature.crystal_wisp.prismatic_shard.damage' Damage RandomEnemy 0 Power 0.50 2.50 -AttackType Ranged -DamageType Magical)
    )),
    (New-Ability 'Crystal Wisp' 'crystal_wisp' 'captured_light' Passive 'Captured Light' 'Increase all healing received by 8%.' @(
        (New-Effect 'effect.creature.crystal_wisp.captured_light.healing' ModifyHealingReceived Self 8)
    ))
)

Add-Creature 'Blue Slime' 'blue_slime' @(
    (New-Ability 'Blue Slime' 'blue_slime' 'sweet_water' Active 'Sweet Water' 'Heal all allies for 70% Power over 6 seconds.' @(
        (New-Effect 'effect.creature.blue_slime.sweet_water.heal' Heal AllAllies 0 Power 0.233333 -Duration 60 -Interval 20)
    )),
    (New-Ability 'Blue Slime' 'blue_slime' 'protective_slime' Passive 'Protective Slime' 'At combat start, grant all allies Barrier equal to 7% of your Max Health.' @(
        (New-Effect 'effect.creature.blue_slime.protective_slime.barrier' GrantBarrier AllAllies 0 MaxHealth 0.07)
    ))
)

Add-Creature 'Transparent Slime' 'transparent_slime' @(
    (New-Ability 'Transparent Slime' 'transparent_slime' 'transparent_engulf' Active 'Transparent Engulf' 'Taunt a random enemy for 7 seconds.' @(
        (New-Effect 'effect.creature.transparent_slime.transparent_engulf.taunt' ApplyCondition RandomEnemy 7 -Condition Taunt)
    )),
    (New-Ability 'Transparent Slime' 'transparent_slime' 'reconstitute' Passive 'Reconstitute' 'The first time you fall below 90% Health, heal 15% Max Health.' @(
        (New-Effect 'effect.creature.transparent_slime.reconstitute.heal' Heal Self 0 MaxHealth 0.15 -Uses 1)
    ) @((New-Trigger OnHealthChanged @('effect.creature.transparent_slime.reconstitute.heal') -Conditions @((New-Condition HealthBelowPercent Source 90)))))
)

Add-Creature 'Moss Lizard' 'moss_lizard' @(
    (New-Ability 'Moss Lizard' 'moss_lizard' 'moss_camouflage' Active 'Moss Camouflage' 'Deal 170% Physical Damage and gain Stealth(3).' @(
        (New-Effect 'effect.creature.moss_lizard.moss_camouflage.damage' Damage CurrentTarget 0 Power 1.70 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.moss_lizard.moss_camouflage.stealth' ApplyCondition Self 3 -Condition Stealth)
    )),
    (New-Ability 'Moss Lizard' 'moss_lizard' 'lost_tail' Passive 'Lost Tail' 'The first time you fall below 40% Health, increase Health Regeneration rate by 250% for 14 seconds.' @(
        (New-Effect 'effect.creature.moss_lizard.lost_tail.regeneration' ModifyRegenerationRate Self 250 -Duration 140 -Uses 1)
    ) @((New-Trigger OnHealthChanged @('effect.creature.moss_lizard.lost_tail.regeneration') -Conditions @((New-Condition HealthBelowPercent Source 40)))))
)

Add-Creature 'Shadow Imp' 'shadow_imp' @(
    (New-Ability 'Shadow Imp' 'shadow_imp' 'shadow_image' Active 'Shadow Image' 'Summon a shadow illusion with exactly 1 Health.' @(
        (New-Effect 'effect.creature.shadow_imp.shadow_image.summon' Summon Self -SummonId creatureShadowImage)
    )),
    (New-Ability 'Shadow Imp' 'shadow_imp' 'shadowy_presence' Passive 'Shadowy Presence' 'Increase Dodge by 2%.' @(
        (New-Effect 'effect.creature.shadow_imp.shadowy_presence.dodge' ModifyAttribute Self 2 -Attribute DodgeChance)
    ))
)

Add-Creature 'Grave Hound' 'grave_hound' @(
    (New-Ability 'Grave Hound' 'grave_hound' 'gravebound_pounce' Active 'Gravebound Pounce' 'Deal 180% Physical Damage to the lowest-health enemy.' @(
        (New-Effect 'effect.creature.grave_hound.gravebound_pounce.damage' Damage LowestHealthEnemy 0 Power 1.80 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Grave Hound' 'grave_hound' 'graveyard_howl' Passive 'Graveyard Howl' 'At combat start, apply Slow to all enemies.' @(
        (New-Effect 'effect.creature.grave_hound.graveyard_howl.slow' ApplyCondition AllEnemies 1 -Condition Slow)
    ))
)

Add-Creature 'Lost Soul' 'lost_soul' @(
    (New-Ability 'Lost Soul' 'lost_soul' 'soul_drain' Active 'Soul Drain' 'Deal 110% Magical Damage and heal for 50% of damage dealt.' @(
        (New-Effect 'effect.creature.lost_soul.soul_drain.damage' Damage RandomEnemy 0 Power 1.10 -AttackType Ranged -DamageType Magical -LifeStealPercentage 50)
    )),
    (New-Ability 'Lost Soul' 'lost_soul' 'soul_lantern' Passive 'Soul Lantern' 'At combat start, apply Vulnerable(5) to a random enemy.' @(
        (New-Effect 'effect.creature.lost_soul.soul_lantern.vulnerable' ApplyCondition RandomEnemy 5 -Condition Vulnerable)
    ))
)

Add-Creature 'Grave Wisp' 'grave_wisp' @(
    (New-Ability 'Grave Wisp' 'grave_wisp' 'mourning_flash' Active 'Mourning Flash' 'Deal 120% Magical Damage to a random enemy and apply Slow.' @(
        (New-Effect 'effect.creature.grave_wisp.mourning_flash.damage' Damage RandomEnemy 0 Power 1.20 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.grave_wisp.mourning_flash.slow' ApplyCondition RandomEnemy 1 -Condition Slow)
    )),
    (New-Ability 'Grave Wisp' 'grave_wisp' 'pale_flame' Passive 'Pale Flame' 'Magical hits deal an additional 5% Shadow Damage.' @(
        (New-Effect 'effect.creature.grave_wisp.pale_flame.damage' Damage EventTarget 0 -EventMagnitudeCoefficient 0.05 -DamageType Shadow -Tags @('Damage.Secondary'))
    ) @((New-Trigger OnHit @('effect.creature.grave_wisp.pale_flame.damage') -Conditions @((New-Condition EventDamageTypeIs EventTarget -DamageType Magical)))))
)

Add-Creature 'Skeleton' 'skeleton' @(
    (New-Ability 'Skeleton' 'skeleton' 'bone_smash' Active 'Bone Smash' 'Deal 150% Physical Damage.' @(
        (New-Effect 'effect.creature.skeleton.bone_smash.damage' Damage CurrentTarget 0 Power 1.50 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Skeleton' 'skeleton' 'calcium' Passive 'Calcium' 'At combat start, gain Guard(4).' @(
        (New-Effect 'effect.creature.skeleton.calcium.guard' ApplyCondition Self 4 -Condition Guard)
    ))
)

Add-Creature 'Pixie' 'pixie' @(
    (New-Ability 'Pixie' 'pixie' 'pixie_burst' Active 'Pixie Burst' 'Deal 80% Magical Damage to all enemies.' @(
        (New-Effect 'effect.creature.pixie.pixie_burst.damage' Damage AllEnemies 0 Power 0.80 -AttackType Ranged -DamageType Magical)
    )),
    (New-Ability 'Pixie' 'pixie' 'resonant_chime' Passive 'Resonant Chime' 'Whenever you use an ability, gain 1% Critical Damage.' @(
        (New-Effect 'effect.creature.pixie.resonant_chime.crit_damage' ModifyAttribute Self 1 -Attribute CritDamage)
    ) @((New-Trigger OnAbilityUsed @('effect.creature.pixie.resonant_chime.crit_damage'))))
)

Add-Creature 'Wood Nymph' 'wood_nymph' @(
    (New-Ability 'Wood Nymph' 'wood_nymph' 'bramble_shield' Active 'Bramble Shield' 'Grant the highest-Max-Health ally Thorns(5) for 12 seconds and Barrier equal to 80% Power.' @(
        (New-Effect 'effect.creature.wood_nymph.bramble_shield.thorns' ApplyCondition HighestMaxHealthAlly 5 -Condition Thorns -Duration 120),
        (New-Effect 'effect.creature.wood_nymph.bramble_shield.barrier' GrantBarrier HighestMaxHealthAlly 0 Power 0.80)
    )),
    (New-Ability 'Wood Nymph' 'wood_nymph' 'natures_protection' Passive "Nature's Protection" 'At combat start, gain Renewal(15) and Ward(2).' @(
        (New-Effect 'effect.creature.wood_nymph.natures_protection.renewal' ApplyCondition Self 15 -Condition Renewal),
        (New-Effect 'effect.creature.wood_nymph.natures_protection.ward' ApplyCondition Self 2 -Condition Ward)
    ))
)

Add-Creature 'Rainbow Slime' 'rainbow_slime' @(
    (New-Ability 'Rainbow Slime' 'rainbow_slime' 'unstable_colors' Active 'Unstable Colors' 'Each ally has an 80% chance to gain Empower; otherwise they gain Weaken.' @(
        (New-Effect 'effect.creature.rainbow_slime.unstable_colors.condition' ApplyRandomCondition AllAllies 1 -Condition Empower -AlternativeCondition Weaken -Chance 80)
    )),
    (New-Ability 'Rainbow Slime' 'rainbow_slime' 'colorful_shield' Passive 'Colorful Shield' 'Whenever you use an ability, grant Barrier equal to 20% Power to the highest-Max-Health ally.' @(
        (New-Effect 'effect.creature.rainbow_slime.colorful_shield.barrier' GrantBarrier HighestMaxHealthAlly 0 Power 0.20)
    ) @((New-Trigger OnAbilityUsed @('effect.creature.rainbow_slime.colorful_shield.barrier'))))
)

Add-Creature 'Enchanted Fairy' 'enchanted_fairy' @(
    (New-Ability 'Enchanted Fairy' 'enchanted_fairy' 'faes_corrosion' Active "Fae's Corrosion" 'Deal 140% Magical Damage to a random enemy and apply Corrosion(8).' @(
        (New-Effect 'effect.creature.enchanted_fairy.faes_corrosion.damage' Damage RandomEnemy 0 Power 1.40 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.enchanted_fairy.faes_corrosion.corrosion' ApplyCondition RandomEnemy 8 -Condition Corrosion)
    )),
    (New-Ability 'Enchanted Fairy' 'enchanted_fairy' 'faes_charm' Passive "Fae's Charm" 'Every 20 seconds, each enemy has an 80% chance to receive Stun(3).' @(
        (New-Effect 'effect.creature.enchanted_fairy.faes_charm.stun' ApplyCondition AllEnemies 3 -Condition Stun -Chance 80)
    ) @((New-Trigger OnInterval @('effect.creature.enchanted_fairy.faes_charm.stun') -Cooldown 200 -Delay 200)))
)

Add-Creature 'Illusion Fox' 'illusion_fox' @(
    (New-Ability 'Illusion Fox' 'illusion_fox' 'a_doomed_illusion' Active 'A Doomed Illusion' 'Apply Doom(300) to a random enemy.' @(
        (New-Effect 'effect.creature.illusion_fox.a_doomed_illusion.doom' ApplyCondition RandomEnemy 300 -Condition Doom)
    )),
    (New-Ability 'Illusion Fox' 'illusion_fox' 'foxfire_wisp' Passive 'Foxfire Wisp' 'Every 5 seconds gain a Foxfire stack, up to 3. When attacked, consume one to deal 35% Magical Damage per stack.' @(
        (New-Effect 'effect.creature.illusion_fox.foxfire_wisp.stack' ApplyStatus Self 1 -StatusId status.foxfire_stack)
    ) @((New-Trigger OnInterval @('effect.creature.illusion_fox.foxfire_wisp.stack') -Cooldown 50 -Delay 50)))
)

Add-Creature 'Thornback Boar' 'thornback_boar' @(
    (New-Ability 'Thornback Boar' 'thornback_boar' 'thorned_rush' Active 'Thorned Rush' 'Deal 180% Physical Damage plus 2% per Thorns stack.' @(
        (New-Effect 'effect.creature.thornback_boar.thorned_rush.damage' Damage RandomEnemy 0 Power 1.80 -ScalingCondition Thorns -ConditionScalingCoefficient 0.02 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Thornback Boar' 'thornback_boar' 'bristling_hide' Passive 'Bristling Hide' 'At combat start, gain Thorns(10) for 30 seconds.' @(
        (New-Effect 'effect.creature.thornback_boar.bristling_hide.thorns' ApplyCondition Self 10 -Condition Thorns -Duration 300)
    ))
)

$hollowEffects = @()
foreach ($threshold in @(80,60,40,20)) {
    $hollowEffects += New-Effect "effect.creature.hollow_stag.hollow_core.$threshold" ModifyAttribute Self 1 -Attribute DamageReduction -Uses 1 -Conditions @((New-Condition HealthBelowPercent Source $threshold))
}
Add-Creature 'Hollow Stag' 'hollow_stag' @(
    (New-Ability 'Hollow Stag' 'hollow_stag' 'echoing_antlers' Active 'Echoing Antlers' 'Deal 150% Magical Damage and apply Weaken.' @(
        (New-Effect 'effect.creature.hollow_stag.echoing_antlers.damage' Damage CurrentTarget 0 Power 1.50 -AttackType Melee -DamageType Magical),
        (New-Effect 'effect.creature.hollow_stag.echoing_antlers.weaken' ApplyCondition CurrentTarget 1 -Condition Weaken)
    )),
    (New-Ability 'Hollow Stag' 'hollow_stag' 'hollow_core' Passive 'Hollow Core' 'For every 20% Health lost, gain 1% Damage Reduction for the rest of combat.' $hollowEffects @((New-Trigger OnHealthChanged @($hollowEffects.id))))
)

Add-Creature 'Treant Sapling' 'treant_sapling' @(
    (New-Ability 'Treant Sapling' 'treant_sapling' 'sprouting_surge' Active 'Sprouting Surge' 'Heal yourself for 210% Power.' @(
        (New-Effect 'effect.creature.treant_sapling.sprouting_surge.heal' Heal Self 0 Power 2.10)
    )),
    (New-Ability 'Treant Sapling' 'treant_sapling' 'nurturing_roots' Passive 'Nurturing Roots' 'Increase all healing received by 15%.' @(
        (New-Effect 'effect.creature.treant_sapling.nurturing_roots.healing' ModifyHealingReceived Self 15)
    ))
)

Add-Creature 'Glade Panther' 'glade_panther' @(
    (New-Ability 'Glade Panther' 'glade_panther' 'ambush_strike' Active 'Ambush Strike' 'Deal 175% Physical Damage with +20% Critical Chance.' @(
        (New-Effect 'effect.creature.glade_panther.ambush_strike.damage' Damage CurrentTarget 0 Power 1.75 -AttackType Melee -DamageType Physical -CritChanceBonus 20)
    )),
    (New-Ability 'Glade Panther' 'glade_panther' 'razor_claws' Passive 'Razor Claws' 'Increase Critical Damage by 22%.' @(
        (New-Effect 'effect.creature.glade_panther.razor_claws.crit_damage' ModifyAttribute Self 22 -Attribute CritDamage)
    ))
)

Add-Creature 'Forest Spirit' 'forest_spirit' @(
    (New-Ability 'Forest Spirit' 'forest_spirit' 'rejuvenation' Active 'Rejuvenation' 'Heal the lowest-health ally for 120% Power.' @(
        (New-Effect 'effect.creature.forest_spirit.rejuvenation.heal' Heal LowestHealthAlly 0 Power 1.20)
    )),
    (New-Ability 'Forest Spirit' 'forest_spirit' 'spirit_bloom' Passive 'Spirit Bloom' 'Every third healing event restores an additional 30% Power to its target.' @(
        (New-Effect 'effect.creature.forest_spirit.spirit_bloom.heal' Heal EventTarget 0 Power 0.30)
    ) @((New-Trigger OnHeal @('effect.creature.forest_spirit.spirit_bloom.heal') -Every 3)))
)

Add-Creature 'Rotroot Shambler' 'rotroot_shambler' @(
    (New-Ability 'Rotroot Shambler' 'rotroot_shambler' 'rotburst' Active 'Rotburst' 'Deal 140% Physical Damage and apply Decay(15).' @(
        (New-Effect 'effect.creature.rotroot_shambler.rotburst.damage' Damage CurrentTarget 0 Power 1.40 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.rotroot_shambler.rotburst.decay' ApplyCondition CurrentTarget 15 -Condition Decay)
    )),
    (New-Ability 'Rotroot Shambler' 'rotroot_shambler' 'decaying_husk' Passive 'Decaying Husk' 'The first time you fall below 50% Health, apply Poison(30) and Decay(20) to all enemies.' @(
        (New-Effect 'effect.creature.rotroot_shambler.decaying_husk.poison' ApplyCondition AllEnemies 30 -Condition Poison),
        (New-Effect 'effect.creature.rotroot_shambler.decaying_husk.decay' ApplyCondition AllEnemies 20 -Condition Decay)
    ) @((New-Trigger OnHealthChanged @('effect.creature.rotroot_shambler.decaying_husk.poison','effect.creature.rotroot_shambler.decaying_husk.decay') -Cooldown 100000 -Conditions @((New-Condition HealthBelowPercent Source 50)))))
)

Add-Creature 'Spider' 'spider' @(
    (New-Ability 'Spider' 'spider' 'skittering_strike' Active 'Skittering Strike' 'Deal 120% Physical Damage.' @(
        (New-Effect 'effect.creature.spider.skittering_strike.damage' Damage CurrentTarget 0 Power 1.20 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Spider' 'spider' 'spider_eyes' Passive 'Spider Eyes' 'Increase Critical Chance by 2%.' @(
        (New-Effect 'effect.creature.spider.spider_eyes.crit' ModifyAttribute Self 2 -Attribute CritChance)
    ))
)

Add-Creature 'Giant Spider' 'giant_spider' @(
    (New-Ability 'Giant Spider' 'giant_spider' 'spider_crash' Active 'Spider Crash' 'Deal 220% Physical Damage to the target and 80% Physical Damage to yourself.' @(
        (New-Effect 'effect.creature.giant_spider.spider_crash.target' Damage CurrentTarget 0 Power 2.20 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.giant_spider.spider_crash.self' Damage Self 0 Power 0.80 -DamageType Physical)
    )),
    (New-Ability 'Giant Spider' 'giant_spider' 'thickened_carapace' Passive 'Thickened Carapace' 'Increase Physical Armor by 15%.' @(
        (New-Effect 'effect.creature.giant_spider.thickened_carapace.armor' ModifyAttribute Self 0 Armor 0.15 -Attribute Armor)
    ))
)

Add-Creature 'Venomous Spiderling' 'venomous_spiderling' @(
    (New-Ability 'Venomous Spiderling' 'venomous_spiderling' 'venom_web' Active 'Venom Web' 'Apply Poison(20) and Slow.' @(
        (New-Effect 'effect.creature.venomous_spiderling.venom_web.poison' ApplyCondition CurrentTarget 20 -Condition Poison),
        (New-Effect 'effect.creature.venomous_spiderling.venom_web.slow' ApplyCondition CurrentTarget 1 -Condition Slow)
    )),
    (New-Ability 'Venomous Spiderling' 'venomous_spiderling' 'toxic_opportunity' Passive 'Toxic Opportunity' 'Basic Attacks against Slowed enemies apply Poison(4).' @(
        (New-Effect 'effect.creature.venomous_spiderling.toxic_opportunity.poison' ApplyCondition EventTarget 4 -Condition Poison -Conditions $eventTargetSlow)
    ) @((New-Trigger OnBasicAttack @('effect.creature.venomous_spiderling.toxic_opportunity.poison'))))
)

Add-Creature 'Blackjaw Spider' 'blackjaw_spider' @(
    (New-Ability 'Blackjaw Spider' 'blackjaw_spider' 'blackjaw_bite' Active 'Blackjaw Bite' 'Deal 160% Physical Damage, or 220% if the target is below 40% Health.' @(
        (New-Effect 'effect.creature.blackjaw_spider.blackjaw_bite.normal' Damage CurrentTarget 0 Power 1.60 -AttackType Melee -DamageType Physical -Conditions @((New-Condition HealthAbovePercent Target 40))),
        (New-Effect 'effect.creature.blackjaw_spider.blackjaw_bite.execute' Damage CurrentTarget 0 Power 2.20 -AttackType Melee -DamageType Physical -Conditions @((New-Condition HealthBelowPercent Target 40)))
    )),
    (New-Ability 'Blackjaw Spider' 'blackjaw_spider' 'powerful_mandibles' Passive 'Powerful Mandibles' 'Every eighth Basic Attack has 50% Armor Penetration.' @(
        (New-Effect 'effect.creature.blackjaw_spider.powerful_mandibles.penetration' ModifyNextBasicAttackArmorPenetration Self 50)
    ) @((New-Trigger OnBasicAttack @('effect.creature.blackjaw_spider.powerful_mandibles.penetration') -Every 8)))
)

Add-Creature 'Flame Imp' 'flame_imp' @(
    (New-Ability 'Flame Imp' 'flame_imp' 'firebomb_toss' Active 'Firebomb Toss' 'Deal 120% Magical Damage and apply Burn(20).' @(
        (New-Effect 'effect.creature.flame_imp.firebomb_toss.damage' Damage CurrentTarget 0 Power 1.20 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.flame_imp.firebomb_toss.burn' ApplyCondition CurrentTarget 20 -Condition Burn)
    )),
    (New-Ability 'Flame Imp' 'flame_imp' 'impish_flame' Passive 'Impish Flame' 'Whenever you apply Burn, there is a 20% chance to apply Taunt(15).' @(
        (New-Effect 'effect.creature.flame_imp.impish_flame.taunt' ApplyCondition EventTarget 15 -Condition Taunt -Chance 20)
    ) @((New-Trigger OnStatusApplied @('effect.creature.flame_imp.impish_flame.taunt') -Conditions @((New-Condition EventIdIs EventTarget -StatusId condition.burn)))))
)

Add-Creature 'Smolder Rat' 'smolder_rat' @(
    (New-Ability 'Smolder Rat' 'smolder_rat' 'scorching_tail' Active 'Scorching Tail' 'Deal 140% Physical Damage and 100% additional damage if the target is Burning.' @(
        (New-Effect 'effect.creature.smolder_rat.scorching_tail.damage' Damage CurrentTarget 0 Power 1.40 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.smolder_rat.scorching_tail.burning' Damage CurrentTarget 0 Power 1.00 -DamageType Physical -Tags @('Damage.Secondary') -Conditions @((New-Condition HasCondition Target 0 Burn)))
    )),
    (New-Ability 'Smolder Rat' 'smolder_rat' 'heat_resistance' Passive 'Heat Resistance' 'Take 15% less Burn Damage.' @(
        (New-Effect 'effect.creature.smolder_rat.heat_resistance.damage' ModifyDamageTaken Self -15 -DamageType Burn)
    ))
)

Add-Creature 'Cinder Beetle' 'cinder_beetle' @(
    (New-Ability 'Cinder Beetle' 'cinder_beetle' 'burning_mandibles' Active 'Burning Mandibles' 'Deal 90% Physical Damage and apply Burn(12) and Bleed(12).' @(
        (New-Effect 'effect.creature.cinder_beetle.burning_mandibles.damage' Damage CurrentTarget 0 Power 0.90 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.cinder_beetle.burning_mandibles.burn' ApplyCondition CurrentTarget 12 -Condition Burn),
        (New-Effect 'effect.creature.cinder_beetle.burning_mandibles.bleed' ApplyCondition CurrentTarget 12 -Condition Bleed)
    )),
    (New-Ability 'Cinder Beetle' 'cinder_beetle' 'molten_shell' Passive 'Molten Shell' 'When critically hit, deal 150% Magical Damage to the attacker. Can trigger four times.' @(
        (New-Effect 'effect.creature.cinder_beetle.molten_shell.damage' Damage EventTarget 0 Power 1.50 -DamageType Magical -Uses 4 -Tags @('Damage.Secondary'))
    ) @((New-Trigger OnDamaged @('effect.creature.cinder_beetle.molten_shell.damage') -Conditions @((New-Condition EventWasCritical EventTarget)))))
)

Add-Creature 'Red Slime' 'red_slime' @(
    (New-Ability 'Red Slime' 'red_slime' 'ignite_core' Active 'Ignite Core' 'For 10 seconds, deal 15% Magical Damage each second to everyone except yourself.' @(
        (New-Effect 'effect.creature.red_slime.ignite_core.damage' Damage EveryoneButSelf 0 Power 0.15 -Duration 100 -Interval 10 -DamageType Magical)
    )),
    (New-Ability 'Red Slime' 'red_slime' 'recharging_core' Passive 'Recharging Core' 'Every 12 seconds, gain 4% Critical Damage, up to 40%.' @(
        (New-Effect 'effect.creature.red_slime.recharging_core.crit_damage' ModifyAttribute Self 4 -Attribute CritDamage -Uses 10)
    ) @((New-Trigger OnInterval @('effect.creature.red_slime.recharging_core.crit_damage') -Cooldown 120 -Delay 120)))
)

Add-Creature 'Giant Worm' 'giant_worm' @(
    (New-Ability 'Giant Worm' 'giant_worm' 'drag_beneath' Active 'Drag Beneath' 'Deal 160% Physical Damage and apply Stun(2).' @(
        (New-Effect 'effect.creature.giant_worm.drag_beneath.damage' Damage CurrentTarget 0 Power 1.60 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.giant_worm.drag_beneath.stun' ApplyCondition CurrentTarget 2 -Condition Stun)
    )),
    (New-Ability 'Giant Worm' 'giant_worm' 'erupting_ambush' Passive 'Erupting Ambush' 'At combat start, deal 300% Physical Damage to the lowest-health enemy and apply Bleed(18).' @(
        (New-Effect 'effect.creature.giant_worm.erupting_ambush.damage' Damage LowestHealthEnemy 0 Power 3.00 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.giant_worm.erupting_ambush.bleed' ApplyCondition LowestHealthEnemy 18 -Condition Bleed)
    ))
)

Add-Creature 'Bog Mite' 'bog_mite' @(
    (New-Ability 'Bog Mite' 'bog_mite' 'infestation' Active 'Infestation' 'Deal 140% Physical Damage and apply Wound(9).' @(
        (New-Effect 'effect.creature.bog_mite.infestation.damage' Damage CurrentTarget 0 Power 1.40 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.bog_mite.infestation.wound' ApplyCondition CurrentTarget 9 -Condition Wound)
    )),
    (New-Ability 'Bog Mite' 'bog_mite' 'blood_feeders' Passive 'Blood Feeders' 'Heal for 5% of damage dealt to Poisoned targets.' @(
        (New-Effect 'effect.creature.bog_mite.blood_feeders.heal' Heal Self 0 -EventMagnitudeCoefficient 0.05 -Conditions $eventTargetPoison)
    ) @((New-Trigger OnHit @('effect.creature.bog_mite.blood_feeders.heal'))))
)

Add-Creature 'Green Slime' 'green_slime' @(
    (New-Ability 'Green Slime' 'green_slime' 'acid_splash' Active 'Acid Splash' 'Deal 60% Magical Damage and apply Poison(18) to all enemies.' @(
        (New-Effect 'effect.creature.green_slime.acid_splash.damage' Damage AllEnemies 0 Power 0.60 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.green_slime.acid_splash.poison' ApplyCondition AllEnemies 18 -Condition Poison)
    )),
    (New-Ability 'Green Slime' 'green_slime' 'corrosive_ooze' Passive 'Corrosive Ooze' 'When damaged by a Physical melee hit, apply Poison(3) to the attacker.' @(
        (New-Effect 'effect.creature.green_slime.corrosive_ooze.poison' ApplyCondition EventTarget 3 -Condition Poison)
    ) @((New-Trigger OnDamaged @('effect.creature.green_slime.corrosive_ooze.poison') -Conditions @((New-Condition EventDamageTypeIs EventTarget -DamageType Physical),(New-Condition EventAttackTypeIs EventTarget -AttackType Melee)))))
)

Add-Creature 'Large Rat' 'large_rat' @(
    (New-Ability 'Large Rat' 'large_rat' 'tail_wrap' Active 'Tail Wrap' 'Deal 240% Physical Damage.' @(
        (New-Effect 'effect.creature.large_rat.tail_wrap.damage' Damage CurrentTarget 0 Power 2.40 -AttackType Melee -DamageType Physical)
    )),
    (New-Ability 'Large Rat' 'large_rat' 'big' Passive 'Big' 'Increase Max Health by 10%.' @(
        (New-Effect 'effect.creature.large_rat.big.health' ModifyAttribute Self 0 MaxHealth 0.10 -Attribute MaxHealth)
    ))
)

Add-Creature 'Viper' 'viper' @(
    (New-Ability 'Viper' 'viper' 'piercing_fangs' Active 'Piercing Fangs' 'Deal 90% Physical Damage and another 90% if the target is Poisoned.' @(
        (New-Effect 'effect.creature.viper.piercing_fangs.damage' Damage CurrentTarget 0 Power 0.90 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.viper.piercing_fangs.poisoned' Damage CurrentTarget 0 Power 0.90 -DamageType Physical -Tags @('Damage.Secondary') -Conditions @((New-Condition HasCondition Target 0 Poison)))
    )),
    (New-Ability 'Viper' 'viper' 'potent_toxins' Passive 'Potent Toxins' 'Increase Poison Damage dealt by 7%.' @(
        (New-Effect 'effect.creature.viper.potent_toxins.damage' ModifyDamageDealt Self 7 -DamageType Poison)
    ))
)

Add-Creature 'Poisonous Rat' 'poisonous_rat' @(
    (New-Ability 'Poisonous Rat' 'poisonous_rat' 'toxic_bite' Active 'Toxic Bite' 'Apply Poison(20), with a 5% chance to also apply Toxic Blood.' @(
        (New-Effect 'effect.creature.poisonous_rat.toxic_bite.poison' ApplyCondition CurrentTarget 20 -Condition Poison),
        (New-Effect 'effect.creature.poisonous_rat.toxic_bite.toxic_blood' ApplyStatus CurrentTarget 1 -StatusId status.toxic_blood -Chance 5)
    )),
    (New-Ability 'Poisonous Rat' 'poisonous_rat' 'resistant_hide' Passive 'Resistant Hide' 'Take 12% less Poison Damage.' @(
        (New-Effect 'effect.creature.poisonous_rat.resistant_hide.damage' ModifyDamageTaken Self -12 -DamageType Poison)
    ))
)

Add-Creature 'Rotfly Toad' 'rotfly_toad' @(
    (New-Ability 'Rotfly Toad' 'rotfly_toad' 'putrid_belch' Active 'Putrid Belch' 'Deal 80% Magical Damage and apply Decay(14).' @(
        (New-Effect 'effect.creature.rotfly_toad.putrid_belch.damage' Damage CurrentTarget 0 Power 0.80 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.rotfly_toad.putrid_belch.decay' ApplyCondition CurrentTarget 14 -Condition Decay)
    )),
    (New-Ability 'Rotfly Toad' 'rotfly_toad' 'rotfly_host' Passive 'Rotfly Host' 'When an enemy with Decay dies, heal 8% Max Health.' @(
        (New-Effect 'effect.creature.rotfly_toad.rotfly_host.heal' Heal Self 0 MaxHealth 0.08 -Conditions @((New-Condition HasCondition EventTarget 0 Decay)))
    ) @((New-Trigger OnKill @('effect.creature.rotfly_toad.rotfly_host.heal'))))
)

Add-Creature 'Brown Slime' 'brown_slime' @(
    (New-Ability 'Brown Slime' 'brown_slime' 'absorb_impact' Active 'Absorb Impact' 'Gain Guard(5).' @(
        (New-Effect 'effect.creature.brown_slime.absorb_impact.guard' ApplyCondition Self 5 -Condition Guard)
    )),
    (New-Ability 'Brown Slime' 'brown_slime' 'layered_mud' Passive 'Layered Mud' 'When hit by a Basic Attack, gain 1 Armor for 6 seconds, up to 30 applications.' @(
        (New-Effect 'effect.creature.brown_slime.layered_mud.armor' ModifyAttribute Self 1 -Attribute Armor -Duration 60 -Uses 30)
    ) @((New-Trigger OnAttacked @('effect.creature.brown_slime.layered_mud.armor') -Conditions @((New-Condition EventIdIs EventTarget -StatusId basic_attack)))))
)

Add-Creature 'Cave Bat' 'cave_bat' @(
    (New-Ability 'Cave Bat' 'cave_bat' 'sonic_screech' Active 'Sonic Screech' 'Deal 135% Magical Damage and apply Slow.' @(
        (New-Effect 'effect.creature.cave_bat.sonic_screech.damage' Damage CurrentTarget 0 Power 1.35 -AttackType Ranged -DamageType Magical),
        (New-Effect 'effect.creature.cave_bat.sonic_screech.slow' ApplyCondition CurrentTarget 1 -Condition Slow)
    )),
    (New-Ability 'Cave Bat' 'cave_bat' 'echolocation' Passive 'Echolocation' 'Increase hit chance by 5%.' @(
        (New-Effect 'effect.creature.cave_bat.echolocation.precision' ModifyAttribute Self 5 -Attribute Precision)
    ))
)

Add-Creature 'Giant Bat' 'giant_bat' @(
    (New-Ability 'Giant Bat' 'giant_bat' 'echoing_screech' Active 'Echoing Screech' 'Deal 90% Magical Damage to all enemies.' @(
        (New-Effect 'effect.creature.giant_bat.echoing_screech.damage' Damage AllEnemies 0 Power 0.90 -AttackType Ranged -DamageType Magical)
    )),
    (New-Ability 'Giant Bat' 'giant_bat' 'resonant_cry' Passive 'Resonant Cry' 'Every fourth Basic Attack deals 30% Magical Damage to all enemies.' @(
        (New-Effect 'effect.creature.giant_bat.resonant_cry.damage' Damage AllEnemies 0 Power 0.30 -DamageType Magical -Tags @('Damage.Secondary'))
    ) @((New-Trigger OnBasicAttack @('effect.creature.giant_bat.resonant_cry.damage') -Every 4)))
)

Add-Creature 'Undead' 'undead' @(
    (New-Ability 'Undead' 'undead' 'necrotic_slash' Active 'Necrotic Slash' 'Deal 150% Physical Damage and apply Decay(8).' @(
        (New-Effect 'effect.creature.undead.necrotic_slash.damage' Damage CurrentTarget 0 Power 1.50 -AttackType Melee -DamageType Physical),
        (New-Effect 'effect.creature.undead.necrotic_slash.decay' ApplyCondition CurrentTarget 8 -Condition Decay)
    )),
    (New-Ability 'Undead' 'undead' 'corpse_explosion' Passive 'Corpse Explosion' 'When killed by a direct hit, deal 350% Magical Damage to the attacker.' @(
        (New-Effect 'effect.creature.undead.corpse_explosion.damage' Damage EventTarget 0 Power 3.50 -DamageType Magical -Tags @('Damage.Secondary'))
    ) @((New-Trigger OnDeath @('effect.creature.undead.corpse_explosion.damage') -Conditions @(
        (New-Condition EventWasDirectHit EventTarget),
        (New-Condition EventSourceIsSelf EventSource)
    ))))
)

$abilitiesPath = Join-Path $RepositoryRoot 'LL/src/API/API.LL/Data/combat/abilities.json'
$existingAbilities = @(Read-JsonArray $abilitiesPath)
$existingAbilities = @($existingAbilities | Where-Object { $_.id -notlike 'ability.creature.*' })
ConvertTo-Json -InputObject @($existingAbilities + $abilities) -Depth 30 | Set-Content -LiteralPath $abilitiesPath -Encoding utf8

$profilePath = Join-Path $RepositoryRoot 'LL/src/API/API.LL/Data/combat/creature-abilities.json'
[pscustomobject][ordered]@{ creatures = @($profiles) } | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $profilePath -Encoding utf8

$behaviorPath = Join-Path $RepositoryRoot 'LL/src/API/API.LL/Data/combat/ability-behaviors.json'
$existingBehaviors = @(Read-JsonArray $behaviorPath)
$existingBehaviors = @($existingBehaviors | Where-Object { $_.abilityId -notlike 'ability.creature.*' })
$creatureBehaviors = foreach ($ability in $abilities) {
    [pscustomobject][ordered]@{
        id = "behavior.$($ability.id.Substring('ability.'.Length)).executes"
        abilityId = $ability.id
        friendlyAbilityIds = @($ability.id)
        maxTicks = 1
        expectedLogs = @()
    }
}
ConvertTo-Json -InputObject @($existingBehaviors + $creatureBehaviors) -Depth 30 | Set-Content -LiteralPath $behaviorPath -Encoding utf8

$statusesPath = Join-Path $RepositoryRoot 'LL/src/API/API.LL/Data/combat/statuses.json'
$statuses = @(Read-JsonArray $statusesPath)
$foxfire = $statuses | Where-Object id -eq 'status.foxfire_stack'
if ($foxfire) {
    $foxfire.maxStacks = 3
    $damage = $foxfire.effects | Where-Object id -eq 'effect.foxfire.damage'
    $damage.baseValue = 0
    $damage | Add-Member -NotePropertyName scalingAttribute -NotePropertyValue Power -Force
    $damage | Add-Member -NotePropertyName scalingStatusId -NotePropertyValue status.foxfire_stack -Force
    $damage | Add-Member -NotePropertyName statusScalingCoefficient -NotePropertyValue 0.35 -Force
}
$toxicBlood = $statuses | Where-Object id -eq 'status.toxic_blood'
if ($toxicBlood) {
    ($toxicBlood.effects | Where-Object id -eq 'effect.toxic_blood.poison').baseValue = 10
}
ConvertTo-Json -InputObject @($statuses) -Depth 30 | Set-Content -LiteralPath $statusesPath -Encoding utf8

$summonsPath = Join-Path $RepositoryRoot 'LL/src/API/API.LL/Data/combat/summons.json'
$summons = @(Read-JsonArray $summonsPath)
$summons = @($summons | Where-Object id -ne 'creatureShadowImage')
$shadowImage = $summons | Where-Object id -eq 'shadowImage'
if ($shadowImage) {
    $health = $shadowImage.attributes | Where-Object attribute -eq 'MaxHealth'
    $health.baseValue = 1
    $health.minimumValue = 1
    $health | Add-Member -NotePropertyName scalingAttribute -NotePropertyValue SummonHealth -Force
    $health | Add-Member -NotePropertyName scalingCoefficient -NotePropertyValue 1 -Force

    $creatureShadowImage = $shadowImage | ConvertTo-Json -Depth 20 | ConvertFrom-Json
    $creatureShadowImage.id = 'creatureShadowImage'
    $creatureShadowImage.name = 'Creature Shadow Image'
    $creatureHealth = $creatureShadowImage.attributes | Where-Object attribute -eq 'MaxHealth'
    $creatureHealth.PSObject.Properties.Remove('scalingAttribute')
    $creatureHealth.PSObject.Properties.Remove('scalingCoefficient')
    $creatureHealth.baseValue = 1
    $creatureHealth.minimumValue = 1
    $summons += $creatureShadowImage
}
ConvertTo-Json -InputObject @($summons) -Depth 30 | Set-Content -LiteralPath $summonsPath -Encoding utf8

Write-Output "Generated $($abilities.Count) abilities across $($profiles.Count) creature profiles."

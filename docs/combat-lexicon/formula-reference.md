# Formula Reference

All values are current engine behavior. `Math.Round` uses .NET midpoint-to-even rounding unless code specifies otherwise.

## Basic attack

`round((max(1, WeaponDamage) + 0.1 × Power) × BasicAttackDamageMultiplier)`, minimum 1.

Current attack rate is `clamp((1 + AttackSpeed/100) / intervalMultiplier, 0.25, 4)`. Default interval is 30 ticks. Attack Speed does not accelerate active cooldowns.

## Defence

`effectiveDefence = max(0, defence - penetration)`

`mitigation = effectiveDefence / (effectiveDefence + 100)`

Order: dodge → critical → typed defence → block → Damage Reduction → barrier → health. Block reduces eligible damage by 50%. Damage Reduction is capped at +40% and floored at -100%.

An older `ArmorDamageReductionConstants.cs` contains an exponential/K=400 model; it is not the formula used by `FastCombatEngine` and should be treated as stale until removed or reconciled.

### Corrosion

`corrosionStacks = min(50, currentStacks + appliedStacks)`

`corrosionMultiplier = 1 - corrosionStacks/100`

`corrodedArmor = max(0, Armor) × corrosionMultiplier`

`corrodedResistance = max(0, Resistance) × corrosionMultiplier`

`effectiveArmor = max(0, corrodedArmor - ArmorPenetration)`

`effectiveResistance = max(0, corrodedResistance - MagicPenetration)`

Other Armor and Resistance modifiers resolve before Corrosion; penetration resolves after it. Values remain numeric through mitigation rather than rounding the reduced defence first. Every successful application refreshes the shared duration to exactly 12 seconds, including at 50 stacks.

## Outgoing and incoming damage modifiers

Power can affect an effect's authored scaling, and the target's `DamageReduction` attribute is the current general incoming reduction. The current runtime has no shared Vulnerable percentage stage.

### Vulnerable

For an incoming direct hit:

`vulnerableStacksAfterApplication = currentStacks + appliedStacks`

`vulnerableMultiplier = 1 + 0.25 × currentVulnerableStacks`

`amplifiedDirectDamage = authoredOutgoingDamage × vulnerableMultiplier`

There is no stack cap. Apply the multiplier after the attacker's authored outgoing scaling and before typed defence, block, general Damage Reduction, Guard, and Barrier. Periodic, reflected, stored, and self-damage use a multiplier of 1. Vulnerable stacks do not expire or get consumed by hits; Cleanse removes all stacks.

### Empower and Weaken Power

Canonical condition modifier:

`conditionPowerPercent = (EmpowerActive ? 20 : 0) - (WeakenActive ? 20 : 0)`

`effectivePower = max(0, powerBeforeConditions × (1 + conditionPowerPercent/100))`

Power remains numeric and is rounded only when the consuming damage, healing, or other effect formula rounds its result. Empower and Weaken therefore cancel exactly when both are active. Each lasts 10 seconds and refreshes, rather than stacking, on reapplication.

Bleed, Burn, Poison, and Doom snapshot `effectivePower` when applied. A later Empower or Weaken application does not change an existing snapshot.

## Critical

`round(value × (1 + max(0, CritDamage)/100))`; crit chance caps at 75%.

## Healing

`authored = round(BaseValue + ScalingStat × ScalingCoefficient)`

`modified = round(authored × max(0, 1 + HealingPower/100))`

Eligible healing may then critically hit. Restored health is capped by missing health.

## Healing received

> No target-side Healing Received multiplier exists in the current runtime.

Canonical Wound and Recovery modifier:

`healingReceivedPercent = (RecoveryActive ? 30 : 0) - (WoundActive ? 30 : 0)`

`healingReceivedMultiplier = max(0, 1 + healingReceivedPercent/100)`

`receivedHealing = round(eligibleHealing × healingReceivedMultiplier)`

Wound and Recovery each contribute at most one fixed modifier regardless of their active stack count, so they cancel exactly while both are active. Eligible healing includes direct healing, Regeneration, and Lifesteal, but excludes Barrier. Missing-Health clamping occurs after this multiplier.

Each `Wound(X)` or `Recovery(X)` application creates an independent X-second stack. A stack expiring or being removed does not affect other stacks of the same condition.

### Health Regeneration amount modifiers

Canonical Decay and Renewal modifier:

`regenerationAmountPercent = (RenewalActive ? 30 : 0) - (DecayActive ? 30 : 0)`

`modifiedRegenerationAmount = max(0, baseRegenerationAmount × (1 + regenerationAmountPercent/100))`

Decay and Renewal each contribute at most one fixed modifier regardless of their active stack count, so they cancel exactly while both are active. They modify only the amount restored per Regeneration trigger; interval and progress rate are unchanged. The Healing Received multiplier is applied afterward when the Regeneration healing resolves.

Each `Decay(X)` or `Renewal(X)` application creates an independent X-second stack. A stack expiring or being removed does not affect other stacks of the same condition.

## Lifesteal

`round(actual health damage × clamp(source LifeSteal + effect percentage, 0, 50)/100)`.

Current effect damage, including periodic effect damage, can lifesteal; basic attacks do not. Lifesteal healing cannot crit but is amplified by Healing Power.

## Barrier

Application is capped at 2.5 times current Maximum Health. Consumption is `absorbed = min(barrier, post-mitigation damage)` followed by subtraction from the oldest source contribution and then health.

Canonical cap: `barrierCap = 2.5 × MaxHealth`.

Canonical accepted grant: `accepted = min(max(0, requested), max(0, barrierCap - currentBarrier))`.

Canonical total after grant: `currentBarrier = currentBarrier + accepted`. Contributions retain their source and are consumed oldest-first. Barrier has no duration formula.

## Guard

For a qualifying direct hit when at least one Guard charge exists:

`guardedDamage = round(postMitigationDirectDamage × 0.75)`

Then consume exactly one Guard charge and pass `guardedDamage` to Barrier absorption. `postMitigationDirectDamage` is the value after dodge, critical, typed defence, block, and general Damage Reduction. A dodged hit or a direct hit with no positive remaining damage consumes no charge. Barrier absorbing the guarded damage does not refund the charge.

`Guard(X)` adds X charges without a cap. Charges have no timer and no removal formula.

## Ward application ordering

For each attempted harmful condition application:

1. Check target validity and specific immunity.
2. Resolve the condition's landing roll, when one exists.
3. If the application would succeed and Ward has at least one charge, consume one Ward charge and cancel the entire application.
4. Otherwise create or mutate condition state and publish the successful application event.

`Poison(3)` is one application and therefore consumes one Ward charge, not three. Ward has no cap, duration, or removal formula.

## Thorns

For a qualifying direct hit:

`activeThornsPercent = sum(X for each active Thorns stack)`

`reflectedDamage = roundAwayFromZero(actualHealthDamageFromHit × activeThornsPercent/100)`

`actualHealthDamageFromHit` is the damage that remains after dodge, critical scaling, typed defence, block, general Damage Reduction, Guard, and Barrier. Resolve one Reflected Damage event against the attacker after the original Health loss. Reflected damage cannot critically strike, trigger Lifesteal, consume Guard, or trigger any reflection effect.

Each `Thorns(X)` application creates one independently expiring stack. There is no canonical stack count or summed-percentage cap. Expiration removes only the expired stack; generic Dispel removes the earliest-expiring stack, then the earliest-applied stack when expiration times tie.

## Cooldown

`max(1, ceil(authoredTicks × (100 - clamp(CooldownReduction, 0, 40))/100 - 1e-9))`.

Cooldowns subsequently progress by exactly one tick per combat tick. Combat Speed/Attack Speed does not change that progression.

## Chill

`chillStacksAfterApplication = min(20, currentChillStacks + appliedStacks)`

`chillMultiplier = 1 - chillStacksAfterApplication/100`

Haste and Slow shared multiplier:

`hasteSlowMultiplier = 1 + (HasteActive ? 0.25 : 0) - (SlowActive ? 0.25 : 0)`

Canonical attack progression:

`baseRate = (1 + AttackSpeed/100) / intervalMultiplier`

`attackProgressRate = clamp(baseRate × hasteSlowMultiplier × chillMultiplier, 0.25, 4)`

Haste and Slow cancel exactly when both are active. Chill is multiplied separately, so it remains independent of Slow. Each successful `Chill(X)` application resets Chill's shared duration to exactly 10 seconds.

## Threat, Taunt, and Stealth

Normal Threat before the Stealth override:

`underlyingThreat = max(0, ThreatAfterAllBuffsAndDebuffs)`

Taunt participates in the underlying Threat modifier pipeline:

`underlyingThreat = max(0, authoredThreatModifiers + (TauntActive ? tauntThreatBonus : 0))`

`tauntThreatBonus` defaults to 100 and is configurable through `FastCombatEngineOptions`.

Final effective Threat:

`effectiveThreat = StealthActive ? 1 : underlyingThreat`

Stealth is evaluated last, after every Threat buff and debuff. Underlying Threat and modifiers continue updating while Stealth is active. When Stealth ends, effective Threat immediately returns to the recalculated underlying value.

`Taunt(X)` and `Stealth(X)` use `durationTicks = max(1, round(X × 10))`. Neither duration is reduced by Status Resistance or Crowd Control Resistance.

## Status duration

`max(1, ceil(authoredTicks / (1 + max(0, applicableResistance)/100)))`.

Only statuses tagged `Control.Stun` use Crowd Control Resistance. Other statuses use Status Resistance. No diminishing-returns formula exists.

### Freeze and Stun

Canonical landing roll:

`lands = randomPercent < 80`

Canonical successful duration:

`durationTicks = max(1, round(X × 10))`

Freeze and Stun use one roll per application attempt. On failure, no status is created or refreshed. On success, X is the exact duration in seconds; generic Status Resistance and Crowd Control Resistance do not shorten it. No canonical modifier to the 80% base chance has been defined.

### Unstoppable

`Unstoppable(X)` uses:

`durationTicks = max(1, round(X × 10))`

X is the exact duration in seconds. A successful reapplication replaces remaining duration with the new value. Status Resistance and Crowd Control Resistance do not modify it.

## Periodic snapshots

Authored periodic damage value is calculated when each runtime tick resolves from the stored effect specification and current source attributes. Source attribution persists and the effect continues after source death while the target remains alive. No explicit snapshot mode is configurable.

Canonical Bleed, Burn, and Poison stacks instead snapshot their own damage when applied:

`stackDamage = applierPowerAtApplication × 0.01`

The 1% coefficient is canonical and shared by Bleed, Burn, and Poison. The authored effect supplies only stack count. Each stack stores the unrounded result, source, tick progress, and remaining duration. Later Power changes do not alter existing stacks. Bleed resolves through Armor/Armor Penetration; Burn and Poison resolve through Resistance/Magic Penetration.

| Condition | Fixed interval | Fixed duration | Ticks | Total damage per stack |
|---|---:|---:|---:|---:|
| Burn | 1 second | 4 seconds | 4 | 4% of snapshotted Power |
| Bleed | 2 seconds | 8 seconds | 4 | 4% of snapshotted Power |
| Poison | 2 seconds | 12 seconds | 6 | 6% of snapshotted Power |

There is no immediate tick. When a tick and expiration share a timestamp, damage resolves first and expiration follows, preserving the final tick.

These fixed schedules are not modified by Status Resistance. Typed standard conditions implement the exception directly; legacy generic statuses retain their authored resistance behavior.

For X identical stacks created by one `Condition(X)` application and due on the same tick:

`combinedTickDamage = round(X × stackDamage)`

`Poison(3)` resolves as `roundAwayFromZero(3 × Power × 0.01)`, or 3% Power-based Magical Damage every interval. Identical due stacks from one source are summed before rounding, avoiding per-stack rounding loss. Underlying stacks remain separate so later partial removal or different expiration times stay deterministic.

## Doom

At application:

`storedDoomDamage = applierPowerAtApplication × X/100`

At exactly 15 seconds:

`doomDamage = round(storedDoomDamage)`

The resulting Magical Damage uses Resistance and Magic Penetration, then the remaining damage may be absorbed by Barrier. Every `Doom(X)` application stores one independent value and trigger time. Doom does not critically strike or trigger Lifesteal by default. Cleanse removes the earliest-triggering stack rather than dealing its damage.

## Attribute caps

Cooldown Reduction 40%; Damage Reduction 40%; Dodge 50%; Block 50%; Critical Chance 75%; Lifesteal 50%.

## Implementation references

- Canonical current rules: `LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs`
- Damage/heal/barrier ordering and periodic evaluation: `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`
- Runtime cooldown/status progression: `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`
- Divergent unused Armor constants: `LL/src/Core/Domain/Helpers/Constants/ArmorDamageReductionConstants.cs`
- Executable examples: `LL/tests/EssenceSystem.Tests/AttributeCombatSystemTests.cs` and `LL/tests/EssenceSystem.Tests/AbilitySystemTests.cs`

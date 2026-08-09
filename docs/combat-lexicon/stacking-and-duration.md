# Stacking and Duration

## Status identity

A combatant holds at most one runtime status per status ID. Catalogue condition identity and runtime status identity are not automatically the same.

## Policies

| Policy    | Reapplication                                         |
| --------- | ----------------------------------------------------- |
| `Refresh` | Preserve the greater stack count and reset duration.  |
| `Stack`   | Add stacks up to `MaxStacks` and reset duration.      |
| `Replace` | Expire the existing status and create a new instance. |

Every successful reapplication publishes `OnStatusApplied`. Reapplying a status with application-owned periodic effects can therefore add another runtime effect even when only one status object remains.

## Canonical stacking models

These catalogue models are design contracts layered over the current runtime policies:

| Model                | Canonical rule                                                                                                                                                     |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Strongest**        | Track valid applications, apply only the strongest magnitude, and reveal the next strongest when it expires. A weaker application never overwrites a stronger one. |
| **Intensity Stacks** | One condition accumulates stack magnitude to a declared cap. The entry defines shared or individual duration.                                                      |
| **Independent Stacks** | Every application creates a separate stack with its own source, magnitude, tick progress, and duration. Stacks do not refresh or replace one another. The entry states whether the count is capped. |
| **Per Source**       | Each source owns one independent application; same-source reapplication follows the entry's refresh rule.                                                          |
| **Charges**          | Applications add expendable charges to a cap. A qualifying event consumes a defined number.                                                                        |
| **Pool**             | Applications contribute quantities to one pool. The entry defines cap, consumption order, and contribution expiry.                                                 |
| **Unique**           | Only one effective instance exists. Reapplication refreshes or replaces it as the entry states.                                                                    |

Runtime `Refresh`, `Stack`, and `Replace` do not by themselves implement source-aware Strongest, fully independent stacks, Per Source, Charges, or contribution-aware Pool behavior.

## Uncapped damage-over-time stacks

[Bleed](conditions/bleed.md), [Burn](conditions/burn.md), and [Poison](conditions/poison.md) use Independent Stacks with no maximum count:

- Every application creates a new stack, even from the same source.
- Each stack stores its source, Power-based damage, tick progress, and remaining duration.
- `Bleed(X)`, `Burn(X)`, or `Poison(X)` creates X stacks in one operation.
- Stacks created by one operation share their initial values and tick alignment, so simultaneous ticks may be aggregated.
- One stack expiring never refreshes, shortens, or removes another.
- Cleanse removes all qualifying stacks of the selected condition unless an effect explicitly states a stack count.
- Stack damage is captured at application; later changes to the applier's Power do not rewrite existing stacks.

## Chill intensity stacks

[Chill](conditions/chill.md) uses Intensity Stacks:

- `Chill(X)` adds X stacks up to 20.
- Each stack applies `-1% Attack Speed`.
- All stacks share one 10-second duration.
- Every successful application refreshes the shared duration to 10 seconds, even at the cap.
- Cleanse removes the condition and all Chill stacks.
- Chill duration is not reduced by Status Resistance.

## Corrosion intensity stacks

[Corrosion](conditions/corrosion.md) also uses Intensity Stacks:

- `Corrosion(X)` adds X stacks to one shared count, up to 50.
- Each stack reduces Armor and Resistance by 1%.
- All stacks share one 12-second duration.
- Every successful application refreshes the shared duration to 12 seconds, even at the cap.
- Stacks from every source contribute to the same condition.
- Cleanse removes the condition and every Corrosion stack.
- Corrosion duration and magnitude are not reduced by Status Resistance.

## Vulnerable intensity stacks

[Vulnerable](conditions/vulnerability.md) uses permanent Intensity Stacks:

- `Vulnerable(X)` adds X stacks to one shared count.
- Each stack increases incoming direct-hit damage by 25%.
- There is no stack cap.
- Stacks have unlimited duration and reapplication only adds stacks.
- Hits do not consume stacks.
- Cleanse removes the condition and every Vulnerable stack.
- Periodic, reflected, stored, and self-damage do not benefit from Vulnerable.

## Fixed-effect independent duration stacks

[Wound](conditions/wound.md), [Recovery](conditions/recovery.md), [Decay](conditions/decay.md), and [Renewal](conditions/renewal.md) use Independent Stacks while allowing only one copy of their fixed modifier to be effective:

- X is the duration in seconds of the newly created stack.
- Every application creates a separate stack with its own source and timer, including same-source applications.
- Reapplication never refreshes, replaces, or extends another stack.
- Any positive stack count activates one fixed 30% modifier; additional stacks extend potential coverage through their own timers but do not increase magnitude.
- Expiration removes only that stack. If another remains, the fixed modifier remains active.
- Generic Cleanse removes one harmful Wound or Decay stack. Generic Dispel removes one beneficial Recovery or Renewal stack.
- Removal selects the earliest-expiring stack, then application order for ties.
- X is exact and is not modified by resistance.

Wound and Recovery cancel at the Healing Received layer while both are active. Decay and Renewal cancel at the Health Regeneration amount layer while both are active.

## Doom independent delayed stacks

[Doom](conditions/doom.md) uses Independent Stacks with potency notation:

- `Doom(X)` creates one stack; X is a Power percentage, not a stack count.
- Each stack snapshots `X%` of its applier's Power and owns a separate 15-second timer.
- Reapplication never refreshes or combines existing Doom stacks.
- There is no maximum number of Doom stacks.
- A generic Cleanse removes only the stack scheduled to trigger first.
- Doom's delay is not reduced by Status Resistance.

## Thorns independent timed stacks

[Thorns](conditions/thorns.md) uses Independent Stacks with potency notation:

- `Thorns(X)` creates one stack; X is that stack's reflected-damage percentage, not a stack count.
- Every stack owns its source, X, and application-supplied duration.
- Reapplication always creates a new stack and never refreshes another stack.
- Add X across every stack active when a qualifying direct hit deals Health damage.
- There is no canonical stack count or combined-percentage cap.
- One stack expiring leaves all other stacks and their timers unchanged.
- A generic Dispel removes only the earliest-expiring stack, with application order breaking ties.

## Empower and Weaken unique refresh

[Empower](conditions/empower.md) and [Weaken](conditions/weaken.md) use Unique stacking:

- Empower is always `+20% Power`; Weaken is always `-20% Power`.
- Each has one instance per combatant and cannot stack with itself.
- Every successful same-condition application resets its duration to 10 seconds.
- Empower and Weaken may coexist and cancel at the shared Power-percentage layer.
- Dispel removes Empower; Cleanse removes Weaken.
- Weaken's duration is not reduced by Status Resistance.

## Haste and Slow unique refresh

[Haste](conditions/haste.md) and [Slow](conditions/slow.md) follow the same Unique pattern:

- Haste is always `+25% Attack Speed`; Slow is always `-25% Attack Speed`.
- Each has one instance per combatant and cannot stack with itself.
- Every successful same-condition application resets its duration to 10 seconds.
- Haste and Slow may coexist and cancel at their shared Attack Speed multiplier.
- Chill remains a separate multiplicative Attack Speed penalty.
- Dispel removes Haste; Cleanse removes Slow.
- Slow's duration is not reduced by Status Resistance.

## Freeze and Stun duration notation

[Freeze](conditions/freeze.md) and [Stun](conditions/stun.md) use Unique stacking with duration notation:

- `Freeze(X)` and `Stun(X)` each make one application roll at an 80% base chance.
- X is the successful duration in seconds, not a stack count.
- A successful reapplication replaces remaining duration with the new X.
- A failed application creates no condition, emits no successful application event, and leaves an existing condition unchanged.
- Successful duration is not reduced by Status Resistance or Crowd Control Resistance.

## Unstoppable duration notation

[Unstoppable](conditions/unstoppable.md) uses Unique stacking with duration notation:

- `Unstoppable(X)` grants control immunity for X seconds.
- X is duration, not a stack count or potency.
- Unstoppable does not stack with itself.
- A successful reapplication replaces the remaining duration with the new X.
- X is the exact duration and is not modified by Status Resistance or Crowd Control Resistance.

## Taunt and Stealth duration notation

[Taunt](conditions/taunt.md) and [Stealth](conditions/stealth.md) are Unique Threat states:

- X is duration in seconds.
- Neither condition stacks.
- Successful reapplication replaces remaining duration with the new X.
- Taunt modifies underlying Threat; its exact bonus remains a balance constant.
- Stealth is a final override that sets effective Threat to 1.
- Underlying Threat continues changing during Stealth and becomes visible again when Stealth ends.

## Guard charges

[Guard](conditions/guard.md) uses Charges:

- `Guard(X)` adds X charges to one shared pool.
- There is no charge cap and reapplication only adds charges.
- Charges have unlimited duration.
- One qualifying direct hit consumes one charge and receives 25% damage reduction.
- Cleanse, Dispel, expiration, and explicit removal cannot remove charges.
- Periodic, reflected, stored, and self-damage do not consume Guard.

## Ward charges

[Ward](conditions/ward.md) also uses permanent Charges:

- `Ward(X)` adds X charges to one shared pool.
- There is no charge cap and charges do not expire.
- After immunity and landing checks, one otherwise-successful harmful condition application consumes one charge and is canceled.
- One multi-stack application consumes only one charge and applies zero stacks.
- Failed and immune applications consume no charge.
- Cleanse, Dispel, expiration, and explicit removal cannot remove Ward.

## Duration

- Ten ticks equal one second.
- Non-positive status duration is permanent.
- Positive duration is reduced as `ceil(authoredTicks / (1 + resistance/100))`, minimum one tick.
- `Control.Stun` uses Crowd Control Resistance; other statuses, including `Control.Freeze`, use Status Resistance.
- Canonical Bleed, Burn, and Poison are explicit exceptions: their fixed durations are not reduced by Status Resistance.
- Canonical Chill is also an exception: its refreshed duration is always 10 seconds.
- Canonical Corrosion is an exception: its refreshed duration is always 12 seconds.
- Canonical Vulnerable stacks are permanent until cleansed or the encounter ends.
- Wound, Recovery, Decay, and Renewal each give every application its own exact X-second duration.
- Canonical Freeze and Stun are exceptions: X is their exact successful duration.
- Canonical Unstoppable is also an exception: X is its exact duration.
- Every Thorns stack keeps its own authored duration; applying another stack does not alter it.
- Cooldowns and status durations continue progressing while stunned.
- Timed status-owned attribute effects are scaled with the resisted status duration.

## Removal

`RemoveStatus` targets an exact runtime ID and publishes `OnStatusRemoved`. Cleanse removes harmful standard conditions and publishes `OnStatusCleansed`; Dispel removes removable beneficial standard conditions and publishes `OnStatusDispelled`. Natural duration expiry alone publishes `OnStatusExpired`. Legacy status Cleanse still lacks beneficial/harmful classification and cleanses all legacy statuses, but now reports the Cleanse removal reason.

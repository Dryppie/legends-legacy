# Guard

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.guard`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Charges                                               |
| Default Removal        | Charge consumption only                               |
| Primary Tags           | Buff, Guard                                           |
| Player-Facing Term     | Guard                                                 |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial                                            |
| Runtime IDs            | `StandardConditionType.Guard`                         |

## Definition

Stores permanent charges that each reduce one incoming direct hit by 25%.

## Design Purpose

Create predictable protection against discrete attacks.

## Current Implementation

`ApplyCondition` grants uncapped permanent charges. One charge reduces one qualifying direct hit by 25% before Barrier and is then consumed.

## Canonical Target Behaviour

`Guard(X)` grants X Guard charges. When the guarded unit receives a qualifying direct hit, one charge reduces that hit's post-mitigation damage by 25%, then is consumed.

## Parameters

X is the number of charges. Reduction is fixed at 25% per qualifying hit. Guard has no duration and no charge cap.

## Stacking and Reapplication

Every application adds X charges to the existing Guard pool. Charges have no maximum, do not expire, and do not track separate durations.

## Timing Rules

After dodge, critical, typed defence, block, and general Damage Reduction, check Guard before Barrier. A non-dodged direct damage instance with positive remaining damage consumes one charge and multiplies that damage by 0.75. Multi-hit effects consume one charge per qualifying hit.

## Valid Targets

Living combatants.

## Removal and Prevention

Guard cannot be Cleansed, Dispelled, expired, stolen, or explicitly removed. Charges leave the pool only when consumed by qualifying hits. Encounter teardown clears the combatant's transient combat state.

## Interactions

Direct melee, ranged, and ability hits qualify. Periodic, reflected, stored, and self-damage do not. Barrier absorbs damage after Guard. A qualifying hit consumes one charge even when Barrier subsequently absorbs all remaining damage.

## Immunity and Resistance

Guard is a permanent beneficial charge mechanic; Status Resistance and Crowd Control Resistance do not apply.

## Examples

- **Ability text:** “Gain Guard(3).”

**Hover text:** “Guard(X) grants X Guard charges. Each charge reduces one incoming direct hit by 25%, then is consumed. Guard does not expire and cannot be removed.”

`Guard(3)` grants three charges. The next three qualifying direct hits are each reduced by 25%.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime; legacy Guard-tagged Damage Reduction remains a separate authored effect.

## Related Entries

[Barrier](barrier.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

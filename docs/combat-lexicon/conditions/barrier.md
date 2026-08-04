# Barrier

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.barrier`                                   |
| Status                 | Implemented                                           |
| Default Stacking Model | Pool                                                  |
| Default Removal        | Damage, explicit removal, or encounter reset          |
| Primary Tags           | Buff, Barrier                                         |
| Player-Facing Term     | Barrier                                               |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial                                            |
| Runtime IDs            | runtime Barrier pool                                  |

## Definition

Stores source-attributed protection that absorbs damage before health is lost.

## Design Purpose

Provide temporary effective health without counting as healing.

## Current Implementation

`GrantBarrier` enforces the 2.5× MaxHealth cap and records source-attributed contributions. Damage consumes contributions oldest-first after mitigation, and application, absorption, and break trigger events are published.

## Canonical Target Behaviour

Barrier has no duration. Each grant records its source and accepted contribution. Total Barrier is capped at `2.5 × MaxHealth`; overflow is discarded. Damage consumes contributions in oldest-first order after mitigation.

## Parameters

Requested amount, accepted amount, source, current total, and cap. The cap is exactly `2.5 × current MaxHealth` and does not require integer rounding because the runtime pool is numeric.

## Stacking and Reapplication

Contributions add until the cap. Each contribution retains its source and remaining amount. Oldest contributions absorb first. A MaxHealth reduction clamps excess Barrier immediately while preserving the oldest contributions first.

## Timing Rules

Available immediately and persists until consumed, explicitly removed, or the encounter resets. Barrier does not expire over time and is unaffected by status-duration progression.

## Valid Targets

Living combatants, including full-health targets.

## Removal and Prevention

Damage consumption, an explicit Barrier-removal effect, or encounter reset. Source death does not remove an existing contribution.

## Interactions

Guard reduces qualifying damage before Barrier absorption. `OnBarrierApplied`, `OnBarrierAbsorbed`, and `OnBarrierBroken` publish their source/target event boundaries. Combat logs retain accepted and absorbed magnitudes, while the runtime contribution list retains Barrier-source ownership.

## Immunity and Resistance

Barrier is a resource-like mechanic rather than a timed status; Status Resistance and Crowd Control Resistance do not apply.

## Examples

- **Ability text:** “Gain 100 Barrier, up to 2.5 times your Max Health.”

After mitigation, 30 Barrier absorbs 30 of 50 damage and 20 reaches health.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard combat runtime. Legacy combat logs retain `RestoreBarrier` for statistics compatibility while the runtime publishes the dedicated Barrier trigger events.

## Related Entries

[Guard](guard.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

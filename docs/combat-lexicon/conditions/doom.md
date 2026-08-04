# Doom

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.doom`                                      |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks                                    |
| Default Removal        | Single-stack Cleanse or Triggered Expiration          |
| Primary Tags           | Affliction, Delayed Damage, Magical                   |
| Player-Facing Term     | Doom                                                  |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful                                               |
| Runtime IDs            | None                                                  |

## Definition

Creates one independent delayed-damage stack that deals X% of the applier's Power as Magical Damage after 15 seconds.

## Design Purpose

Create visible delayed threat with cleanse counterplay.

## Current Implementation

`ApplyCondition` creates an independent 15-second Doom instance that snapshots effective Power and resolves stored Magical Damage only on natural trigger.

## Canonical Target Behaviour

`Doom(X)` applies one Doom stack whose potency is X% of the applier's snapshotted Power. After exactly 15 seconds, that stack deals Magical Damage and removes itself.

## Parameters

X is the percentage of Power used by that stack. Delay is fixed at 15 seconds. There is no shared stack cap; each application stores its own source, Power snapshot, potency, and trigger time.

## Stacking and Reapplication

Every application creates exactly one independent Doom stack, including repeated applications from the same source. Applying Doom never refreshes, combines, or replaces another Doom stack.

## Timing Rules

Each stack triggers exactly 15 seconds after its own application. Damage resolves before that stack is removed. Existing stacks keep their stored Power value and still trigger if their applier dies.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse removes exactly one Doom stack: the stack with the earliest trigger time, with application order breaking ties. Ward or Doom immunity may prevent a new stack. Encounter end removes all remaining stacks without triggering them.

## Interactions

Doom is Magical Damage and uses Resistance/Magic Penetration before Barrier. It does not critically strike or trigger Lifesteal by default. Removal reason is essential because only the natural 15-second trigger deals damage.

## Immunity and Resistance

Doom's fixed 15-second delay and potency are not reduced by Status Resistance. A specific Doom immunity may prevent application.

## Examples

- **Ability text:** “Deal 100% Magical Damage to a target, and apply Doom(40).”

**Hover text:** “Doom(X) applies one Doom stack. After 15 seconds, it deals X% of the applier's Power as Magical Damage.”

`Doom(40)` snapshots 40% of its applier's Power and deals that Magical Damage after 15 seconds. A second application creates a separate timer; Cleanse removes only the stack scheduled to trigger first.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime. Legacy statuses do not gain Doom behavior merely from a matching display tag.

## Related Entries

[Barrier](barrier.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

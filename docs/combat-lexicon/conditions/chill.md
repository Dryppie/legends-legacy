# Chill

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.chill`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Intensity Stacks                                      |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Affliction, Cold, Attack Speed                        |
| Player-Facing Term     | Chill                                                 |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful, Affliction                                   |
| Runtime IDs            | `StandardConditionType.Chill`                         |

## Definition

Accumulates up to 20 stacks, with each stack reducing Attack Speed by one percentage point.

## Design Purpose

Create an escalating Attack Speed penalty that is mechanically separate from Slow.

## Current Implementation

`ApplyCondition` adds Chill stacks to a shared 20-stack count, applies -1% Attack Speed per stack independently of Slow, and refreshes the exact ten-second duration.

## Canonical Target Behaviour

`Chill(X)` applies X Chill stacks. Each stack applies `-1% Attack Speed`, to a maximum of 20 stacks and `-20% Attack Speed`. Chill is not Slow and does not modify active-ability cooldown progression.

## Parameters

One percentage point of Attack Speed reduction per stack, maximum 20 stacks, and a fixed 10-second shared duration.

## Stacking and Reapplication

Add X stacks up to the 20-stack cap. Every successful application refreshes the entire Chill condition to 10 seconds, including an application received while already at 20 stacks. Stacks share one duration rather than expiring independently.

## Timing Rules

The Attack Speed penalty changes immediately after stack mutation. All stacks expire together 10 seconds after the most recent successful application.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse removes the entire Chill condition and all stacks in one operation. Expiration and encounter end also remove every stack.

## Interactions

Chill and Slow are independent Attack Speed modifiers. Haste and Slow share one ±25% multiplier; Chill applies a separate `1%` reduction per stack afterward. Chill stacks are never counted as Slow magnitude and do not alter Slow's duration.

## Immunity and Resistance

Chill's fixed 10-second duration is not reduced by Status Resistance or Crowd Control Resistance. A specific Chill immunity may prevent application.

## Examples

- **Ability text:** “Deal 100% Magical Damage to a target, and apply Chill(5).”

**Hover text:** “Chill(X) applies X stacks of Chill. Each stack decreases Attack Speed by 1%, up to 20 stacks. Chill lasts 10 seconds, and applying Chill refreshes its duration.”

`Chill(5)` applies `-5% Attack Speed`. Applying `Chill(18)` afterward produces 20 stacks and refreshes the shared duration to 10 seconds.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Legacy Cold statuses remain separate authored statuses and are not automatically converted into canonical Chill.

## Related Entries

[Slow](slow.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

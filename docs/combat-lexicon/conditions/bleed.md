# Bleed

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.bleed`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks                                    |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Affliction, Bleed, Periodic, Physical                 |
| Player-Facing Term     | Bleed                                                 |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful, Damage over time                             |
| Runtime IDs            | `StandardConditionType.Bleed`                         |

## Definition

Deals periodic Physical damage through independently timed stacks.

In ability text, `Bleed(X)` means that the effect applies X Bleed stacks.

## Design Purpose

Provide sustained physical-family damage.

## Current Implementation

`ApplyCondition` creates uncapped independent Bleed applications, snapshots effective Power, and resolves Physical ticks every two seconds for eight seconds.

## Canonical Target Behaviour

Every application creates a new uncapped stack. Each stack records its applier, its own remaining duration, tick progress, and damage per tick. Damage is Physical and uses Armor/Armor Penetration.

## Parameters

Each stack deals exactly 1% of its applier's Power every 2 seconds for 8 seconds. Only stack count is configured by the applying ability; damage, interval, and duration are shared Bleed rules.

## Stacking and Reapplication

Every application adds one stack, including repeated applications from the same source. There is no maximum stack count. Reapplication never refreshes or replaces another stack; every stack expires on its own timer.

A single `Bleed(X)` operation creates X independent stacks with identical source, stored damage, interval, duration, and initial tick progress.

## Timing Rules

Each stack ticks at 2, 4, 6, and 8 seconds after application, for four ticks total. There is no immediate tick. The tick at 8 seconds resolves before the stack expires. Existing stacks keep their stored damage if the applier's Power later changes or the applier dies.

Stacks created together tick together and their damage may be summed into one resolution/log entry, provided each stack's duration and removal state remain independently tracked.

## Valid Targets

Living targets that can bleed; target restrictions need an explicit trait system.

## Removal and Prevention

Cleanse, immunity, death, encounter end.

## Interactions

Uses the Physical defence channel: Armor and Armor Penetration. Multiple stacks may tick on the same combat tick and resolve independently.

## Immunity and Resistance

Bleed's 8-second duration and damage are not reduced by Status Resistance. A specific Bleed immunity may prevent new stacks.

## Examples

- **Ability text:** “Deal 150% Physical Damage to a target, and apply Bleed(3).”

**Hover text:** “Bleed(X) applies X stacks of Bleed. Each stack deals 1% Physical Damage every 2 seconds for 8 seconds.”

`Bleed(3)` deals 3% Physical Damage at 2, 4, 6, and 8 seconds. Two Bleed applications made at different times retain different tick and expiration schedules.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Authored Bleed content uses the typed `ApplyCondition` contract.

## Related Entries

[Burn](burn.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

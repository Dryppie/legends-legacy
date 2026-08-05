# Burn

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.burn`                                      |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks                                    |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Affliction, Burn, Periodic, Magical                   |
| Player-Facing Term     | Burn                                                  |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful, Damage over time                             |
| Runtime IDs            | `StandardConditionType.Burn`                          |

## Definition

Deals periodic Magical damage through independently timed stacks.

In ability text, `Burn(X)` means that the effect applies X Burn stacks.

## Design Purpose

Provide sustained magical-family damage with fire identity.

## Current Implementation

`ApplyCondition` creates uncapped independent Burn applications, snapshots effective Power, and resolves Magical ticks every second for four seconds.

## Canonical Target Behaviour

Every application creates a new uncapped stack. Each stack records its applier, its own remaining duration, tick progress, and damage per tick. Damage is Magical and uses Resistance/Magic Penetration.

## Parameters

Each stack deals exactly 1% of its applier's Power every 1 second for 4 seconds. Only stack count is configured by the applying ability; damage, interval, and duration are shared Burn rules.

## Stacking and Reapplication

Every application adds one stack, including repeated applications from the same source. There is no maximum stack count. Reapplication never refreshes or replaces another stack; every stack expires on its own timer.

A single `Burn(X)` operation creates X independent stacks with identical source, stored damage, interval, duration, and initial tick progress.

## Timing Rules

Each stack ticks at 1, 2, 3, and 4 seconds after application, for four ticks total. There is no immediate tick. The tick at 4 seconds resolves before the stack expires. Existing stacks keep their stored damage if the applier's Power later changes or the applier dies.

Stacks created together tick together and their damage may be summed into one resolution/log entry, provided each stack's duration and removal state remain independently tracked.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse, immunity, death, encounter end.

## Interactions

Uses the Magical defence channel: Resistance and Magic Penetration. Multiple stacks may tick on the same combat tick and resolve independently.

## Immunity and Resistance

Burn's 4-second duration and damage are not reduced by Status Resistance. A specific Burn immunity may prevent new stacks.

## Examples

- **Ability text:** “Deal 150% Magical Damage to a target, and apply Burn(3).”

**Hover text:** “Burn(X) applies X stacks of Burn. Each stack deals 1% Magical Damage every 1 second for 4 seconds.”

`Burn(3)` deals 3% Magical Damage at 1, 2, 3, and 4 seconds. Two Burn applications made at different times retain different tick and expiration schedules.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Authored Burn content uses the typed `ApplyCondition` contract.

## Related Entries

[Poison](poison.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

# Poison

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.poison`                                    |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks                                    |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Affliction, Poison, Periodic, Magical                 |
| Player-Facing Term     | Poison                                                |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful, Damage over time                             |
| Runtime IDs            | `StandardConditionType.Poison`                        |

## Definition

Deals periodic Magical damage through independently timed stacks.

In ability text, `Poison(X)` means that the effect applies X Poison stacks.

## Design Purpose

Provide sustained magical-family damage with source attribution.

## Current Implementation

`ApplyCondition` creates uncapped independent Poison applications, snapshots effective Power, and resolves Magical ticks every two seconds for twelve seconds.

## Canonical Target Behaviour

Every application creates a new uncapped stack. Each stack records its applier, its own remaining duration, tick progress, and damage per tick. Damage is Magical and uses Resistance/Magic Penetration.

## Parameters

Each stack deals exactly 1% of its applier's Power every 2 seconds for 12 seconds. Only stack count is configured by the applying ability; damage, interval, and duration are shared Poison rules.

## Stacking and Reapplication

Every application adds one stack, including repeated applications from the same source. There is no maximum stack count. Reapplication never refreshes or replaces another stack; every stack expires on its own timer.

A single `Poison(X)` operation creates X independent stacks with identical source, stored damage, interval, duration, and initial tick progress.

## Timing Rules

Each stack ticks at 2, 4, 6, 8, 10, and 12 seconds after application, for six ticks total. There is no immediate tick. The tick at 12 seconds resolves before the stack expires. Existing stacks keep their stored damage if the applier's Power later changes or the applier dies.

Stacks created together tick together and their damage may be summed into one resolution/log entry, provided each stack's duration and removal state remain independently tracked.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse, immunity, death, encounter end.

## Interactions

Uses the Magical defence channel: Resistance and Magic Penetration. Multiple stacks may tick on the same combat tick and resolve independently. Lifesteal excludes periodic Poison by default.

## Immunity and Resistance

Poison always lasts 12 seconds. Its duration and damage are not reduced by Status Resistance. A specific Poison immunity may prevent new stacks.

## Examples

- **Ability text:** “Deal 150% Physical Damage to a target, and apply Poison(3).”

**Hover text:** “Poison(X) applies X stacks of Poison. Each stack deals 1% Magical Damage every 2 seconds for 12 seconds.”

`Poison(3)` therefore deals `3 × 1% = 3%` Magical Damage every 2 seconds for 12 seconds. The direct 150% Physical Damage is a separate damage instance.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Authored Poison content uses the typed `ApplyCondition` contract.

## Related Entries

[Burn](burn.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

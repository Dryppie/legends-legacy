# Slow

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.slow`                                      |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Debuff, Attack Speed                                  |
| Player-Facing Term     | Slow                                                  |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful                                               |
| Runtime IDs            | `StandardConditionType.Slow`                          |

## Definition

Decreases Attack Speed by exactly 25% for 10 seconds.

## Design Purpose

Provide a simple fixed basic-attack speed debuff without disabling actions.

## Current Implementation

`ApplyCondition` applies a Unique -25% Attack Speed multiplier for ten seconds. Reapplication refreshes it and basic-attack progress reads it every tick.

## Canonical Target Behaviour

While Slow is active, apply a `0.75×` multiplier to Attack Speed. Slow affects basic-attack progression but not active-ability cooldowns, condition timers, periodic intervals, regeneration, or encounter time.

## Parameters

Magnitude is fixed at `-25% Attack Speed`; duration is fixed at 10 seconds. Abilities do not author alternate Slow magnitudes or durations.

## Stacking and Reapplication

Slow does not stack, regardless of source. Any successful new application refreshes the one Slow instance to 10 seconds, including an application received while Slow is already active.

## Timing Rules

The Attack Speed multiplier applies immediately without removing existing attack progress. Slow expires 10 seconds after the most recent successful application.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse or immunity; encounter end clears it.

## Interactions

Haste and Slow modify one shared multiplier and cancel exactly while both are active. Chill applies its separate Attack Speed multiplier afterward.

## Immunity and Resistance

Slow's fixed 10-second duration and magnitude are not reduced by Status Resistance. A specific Slow immunity may prevent application.

## Examples

- **Ability text:** “Apply Slow to the target.”

**Hover text:** “Slow decreases Attack Speed by 25% for 10 seconds. Slow does not stack; applying Slow refreshes its duration.”

A unit with Slow gains basic-attack progress at 0.75 times its otherwise applicable rate before Chill and global rate clamps.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime. Slow intentionally affects Attack Speed rather than active cooldowns.

## Related Entries

[Haste](haste.md) · [Chill](chill.md) · [Stacking and duration](../stacking-and-duration.md)

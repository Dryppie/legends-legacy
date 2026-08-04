# Haste

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.haste`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Dispel or Expiration                                  |
| Primary Tags           | Buff, Attack Speed                                    |
| Player-Facing Term     | Haste                                                 |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial                                            |
| Runtime IDs            | `StandardConditionType.Haste`                         |

## Definition

Increases Attack Speed by exactly 25% for 10 seconds.

## Design Purpose

Provide a simple fixed basic-attack speed buff.

## Current Implementation

`ApplyCondition` applies a Unique +25% Attack Speed multiplier for ten seconds. Reapplication refreshes it and basic-attack progress reads it every tick.

## Canonical Target Behaviour

While Haste is active, apply a `1.25×` multiplier to Attack Speed. Haste affects basic-attack progression but not active-ability cooldowns, condition timers, periodic intervals, regeneration, or encounter time.

## Parameters

Magnitude is fixed at `+25% Attack Speed`; duration is fixed at 10 seconds. Abilities do not author alternate Haste magnitudes or durations.

## Stacking and Reapplication

Haste does not stack, regardless of source. Any successful new application refreshes the one Haste instance to 10 seconds, including an application received while Haste is already active.

## Timing Rules

The Attack Speed multiplier applies immediately without granting attack progress. Haste expires 10 seconds after the most recent successful application.

## Valid Targets

Living combatants, normally allies or self.

## Removal and Prevention

Dispel; prevented only by general beneficial-effect restrictions. Encounter end clears it.

## Interactions

Haste and Slow modify one shared multiplier and cancel exactly while both are active. Chill applies its separate Attack Speed multiplier afterward.

## Immunity and Resistance

Status Resistance does not apply to beneficial Haste.

## Examples

- **Ability text:** “Gain Haste.”

**Hover text:** “Haste increases Attack Speed by 25% for 10 seconds. Haste does not stack; applying Haste refreshes its duration.”

A unit with Haste gains basic-attack progress at 1.25 times its otherwise applicable rate before Chill and global rate clamps.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime. Haste intentionally affects Attack Speed rather than active cooldowns.

## Related Entries

[Slow](slow.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

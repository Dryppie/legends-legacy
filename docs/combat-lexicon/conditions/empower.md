# Empower

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.empower`                                   |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Dispel or Expiration                                  |
| Primary Tags           | Buff, Power                                           |
| Player-Facing Term     | Empower                                               |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial                                            |
| Runtime IDs            | None                                                  |

## Definition

Increases Power by exactly 20% for 10 seconds.

## Design Purpose

Provide a simple fixed offensive buff through the existing Power attribute.

## Current Implementation

`ApplyCondition` creates the fixed +20% effective-Power layer for ten seconds and refreshes its Unique instance on reapplication.

## Canonical Target Behaviour

While Empower is active, add `+20%` to the unit's condition-based Power modifier. Every formula that reads Power uses the resulting effective Power.

## Parameters

Magnitude is fixed at `+20% Power`; duration is fixed at 10 seconds. Abilities do not author alternate Empower magnitudes or durations.

## Stacking and Reapplication

Empower does not stack, regardless of source. Any successful new application refreshes the one Empower instance to 10 seconds, including an application received while Empower is already active.

## Timing Rules

The Power increase applies immediately. Empower expires 10 seconds after the most recent successful application.

## Valid Targets

Living combatants.

## Removal and Prevention

Dispel; encounter end.

## Interactions

Empower and Weaken modify the same percentage layer and cancel one another while both are active. Empower affects new Bleed, Burn, Poison, and Doom snapshots, but does not rewrite existing snapshots.

## Immunity and Resistance

No resistance for beneficial application.

## Examples

- **Ability text:** “Gain Empower.”

**Hover text:** “Empower increases Power by 20% for 10 seconds. Empower does not stack; applying Empower refreshes its duration.”

At 100 Power, Empower produces 120 effective Power. Reapplying it after 6 seconds resets its remaining duration to 10 seconds.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime; legacy flat Power modifiers remain independent.

## Related Entries

[Weaken](weaken.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

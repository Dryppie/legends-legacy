# Weaken

| Field                  | Value                                                   |
| ---------------------- | ------------------------------------------------------- |
| Stable ID              | `condition.weaken`                                      |
| Status                 | Implemented                                             |
| Default Stacking Model | Unique                                                  |
| Default Removal        | Cleanse or Expiration                                   |
| Primary Tags           | Debuff, Power                                           |
| Player-Facing Term     | Weaken                                                  |
| Known Aliases          | See Runtime IDs below; no additional canonical alias.   |
| Classification         | Harmful                                                 |
| Runtime IDs            | `StandardConditionType.Weaken`                        |

## Definition

Decreases Power by exactly 20% for 10 seconds.

## Design Purpose

Provide a simple fixed offensive debuff through the existing Power attribute.

## Current Implementation

`ApplyCondition` creates the fixed -20% effective-Power layer for ten seconds and refreshes its Unique instance on reapplication.

## Canonical Target Behaviour

While Weaken is active, add `-20%` to the unit's condition-based Power modifier. Every formula that reads Power uses the resulting effective Power.

## Parameters

Magnitude is fixed at `-20% Power`; duration is fixed at 10 seconds. Abilities do not author alternate Weaken magnitudes or durations.

## Stacking and Reapplication

Weaken does not stack, regardless of source. Any successful new application refreshes the one Weaken instance to 10 seconds, including an application received while Weaken is already active.

## Timing Rules

The Power reduction applies immediately. Weaken expires 10 seconds after the most recent successful application.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse, immunity, encounter end.

## Interactions

Empower and Weaken modify the same percentage layer and cancel one another while both are active. Weaken affects new Bleed, Burn, Poison, and Doom snapshots, but does not rewrite existing snapshots.

## Immunity and Resistance

Weaken's fixed 10-second duration and magnitude are not reduced by Status Resistance. A specific Weaken immunity may prevent application.

## Examples

- **Ability text:** “Apply Weaken to the target.”

**Hover text:** “Weaken decreases Power by 20% for 10 seconds. Weaken does not stack; applying Weaken refreshes its duration.”

At 100 Power, Weaken produces 80 effective Power. Reapplying it after 6 seconds resets its remaining duration to 10 seconds.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime; legacy flat Power modifiers remain independent.

## Related Entries

[Empower](empower.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

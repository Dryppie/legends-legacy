# Wound

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.wound`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks, one effective                    |
| Default Removal        | Single-stack Cleanse or Expiration                    |
| Primary Tags           | Debuff, Healing Received                              |
| Player-Facing Term     | Wound                                                 |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful                                               |
| Runtime IDs            | None                                                  |

## Definition

`Wound(X)` creates one independent stack lasting X seconds. While at least one stack is active, healing received is reduced by 30%.

## Design Purpose

Counter sustain without changing damage.

## Current Implementation

`ApplyCondition` stores independent Wound timers and applies one fixed -30% Healing Received modifier while any stack remains.

## Canonical Target Behaviour

Apply one fixed `-30% Healing Received` modifier while any Wound stack is active. Direct healing, Regeneration, and Lifesteal qualify; Barrier does not.

## Parameters

X is the duration of the new stack in seconds. The magnitude is always 30%.

## Stacking and Reapplication

Every application creates a separate stack with its own source and X-second timer, including applications from the same source. Only one fixed 30% penalty is effective regardless of stack count. Reapplication never refreshes or replaces another stack; when one expires, Wound remains active if any other stack remains.

## Timing Rules

Checked when healing resolves.

## Valid Targets

Living combatants.

## Removal and Prevention

A generic Cleanse removes one Wound stack, choosing the earliest-expiring stack and then application order for ties. Natural expiration removes only that stack. Ward can prevent a new application. Encounter end removes all stacks.

## Interactions

Reduces direct healing, Regeneration, and Lifesteal healing, but not Barrier. [Recovery](recovery.md) applies an equal opposite modifier, so the two cancel while both are active.

## Immunity and Resistance

X is the exact duration in seconds and is not reduced by Status Resistance.

## Examples

- **Ability text:** “Apply Wound(6).”
- **Hover text:** “Wound(X) lasts X seconds. While Wounded, healing received is reduced by 30%. Applications have independent durations, but only one penalty is effective.”

Wound(6) and Wound(10) applied together still reduce 100 healing to 70. When the six-second stack expires, the ten-second stack keeps the 30% penalty active until its own timer expires.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition and healing pipelines.

## Related Entries

[Recovery](recovery.md) · [Regeneration](regeneration.md) · [Lifesteal](lifesteal.md) · [Stacking and duration](../stacking-and-duration.md)

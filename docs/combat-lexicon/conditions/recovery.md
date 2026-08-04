# Recovery

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.recovery`                                  |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks, one effective                    |
| Default Removal        | Single-stack Dispel or Expiration                     |
| Primary Tags           | Buff, Healing Received                               |
| Player-Facing Term     | Recovery                                              |
| Known Aliases          | None                                                  |
| Classification         | Beneficial                                            |
| Runtime IDs            | None                                                  |

## Definition

`Recovery(X)` creates one independent stack lasting X seconds. While at least one stack is active, healing received is increased by 30%.

## Design Purpose

Create a fixed healing-amplification window without allowing overlapping applications to multiply its magnitude.

## Current Implementation

`ApplyCondition` stores independent Recovery timers and applies one fixed +30% Healing Received modifier while any stack remains.

## Canonical Target Behaviour

Apply one fixed `+30% Healing Received` modifier while any Recovery stack is active. Direct healing, Regeneration, and Lifesteal qualify; Barrier does not.

## Parameters

X is the duration of the new stack in seconds. The magnitude is always 30%.

## Stacking and Reapplication

Every application creates a separate stack with its own source and X-second timer, including applications from the same source. Only one fixed 30% bonus is effective regardless of stack count. Reapplication never refreshes or replaces another stack; when one expires, Recovery remains active if any other stack remains.

## Timing Rules

Check whether any Recovery stack is active when healing resolves.

## Valid Targets

Living combatants.

## Removal and Prevention

A generic Dispel removes one Recovery stack, choosing the earliest-expiring stack and then application order for ties. Natural expiration removes only that stack. Encounter end removes all stacks.

## Interactions

Amplifies direct healing, Regeneration, and Lifesteal healing, but not Barrier. [Wound](wound.md) applies an equal opposite modifier, so the two cancel while both are active.

## Immunity and Resistance

X is the exact duration in seconds and is not modified by resistance.

## Examples

- **Ability text:** “Gain Recovery(6).”
- **Hover text:** “Recovery(X) lasts X seconds. While active, healing received is increased by 30%. Applications have independent durations, but only one bonus is effective.”

Recovery(6) and Recovery(10) applied together still increase 100 healing to 130. When the six-second stack expires, the ten-second stack preserves the bonus until its own timer expires.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition and healing pipelines.

## Related Entries

[Wound](wound.md) · [Regeneration](regeneration.md) · [Lifesteal](lifesteal.md) · [Stacking and duration](../stacking-and-duration.md)

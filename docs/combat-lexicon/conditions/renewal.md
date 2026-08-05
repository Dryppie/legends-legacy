# Renewal

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.renewal`                                   |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks, one effective                    |
| Default Removal        | Single-stack Dispel or Expiration                     |
| Primary Tags           | Buff, Health Regeneration                            |
| Player-Facing Term     | Renewal                                               |
| Known Aliases          | None                                                  |
| Classification         | Beneficial                                            |
| Runtime IDs            | `StandardConditionType.Renewal`                       |

## Definition

`Renewal(X)` creates one independent stack lasting X seconds. While at least one stack is active, Health Regeneration is increased by 30%.

## Design Purpose

Create a fixed passive-sustain window without altering direct healing or Regeneration timing.

## Current Implementation

`ApplyCondition` stores independent Renewal timers and applies one fixed +30% Health Regeneration amount modifier while any stack remains.

## Canonical Target Behaviour

Apply one fixed `+30% Health Regeneration` modifier while any Renewal stack is active. This changes the amount restored by Regeneration triggers, not their interval or progress rate.

## Parameters

X is the duration of the new stack in seconds. The magnitude is always 30%.

## Stacking and Reapplication

Every application creates a separate stack with its own source and X-second timer, including applications from the same source. Only one fixed 30% bonus is effective regardless of stack count. Reapplication never refreshes or replaces another stack; when one expires, Renewal remains active if any other stack remains.

## Timing Rules

Check whether any Renewal stack is active when a Regeneration trigger calculates its healing amount.

## Valid Targets

Living combatants.

## Removal and Prevention

A generic Dispel removes one Renewal stack, choosing the earliest-expiring stack and then application order for ties. Natural expiration removes only that stack. Encounter end removes all stacks.

## Interactions

Only Health Regeneration is increased. Direct healing and Lifesteal are unaffected. [Decay](decay.md) applies an equal opposite modifier, so the two cancel while both are active. Wound and Recovery apply afterward to the healing actually received.

## Immunity and Resistance

X is the exact duration in seconds and is not modified by resistance.

## Examples

- **Ability text:** “Gain Renewal(8).”
- **Hover text:** “Renewal(X) lasts X seconds. While active, Health Regeneration is increased by 30%. Applications have independent durations, but only one bonus is effective.”

Renewal(8) increases a Regeneration amount of 100 to 130 without changing how often Regeneration triggers.

## Implementation References

`LL/src/Core/Domain/Models/Attributes/AttributeType.cs`; `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime.

## Related Entries

[Decay](decay.md) · [Regeneration](regeneration.md) · [Recovery](recovery.md) · [Stacking and duration](../stacking-and-duration.md)

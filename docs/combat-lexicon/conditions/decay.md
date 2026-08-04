# Decay

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.decay`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks, one effective                    |
| Default Removal        | Single-stack Cleanse or Expiration                    |
| Primary Tags           | Debuff, Health Regeneration                          |
| Player-Facing Term     | Decay                                                 |
| Known Aliases          | None                                                  |
| Classification         | Harmful                                               |
| Runtime IDs            | `StandardConditionType.Decay`                         |

## Definition

`Decay(X)` creates one independent stack lasting X seconds. While at least one stack is active, Health Regeneration is reduced by 30%.

## Design Purpose

Counter passive regeneration without reducing other healing sources.

## Current Implementation

`ApplyCondition` stores independent Decay timers and applies one fixed -30% Health Regeneration amount modifier while any stack remains.

## Canonical Target Behaviour

Apply one fixed `-30% Health Regeneration` modifier while any Decay stack is active. This changes the amount restored by Regeneration triggers, not their interval or progress rate.

## Parameters

X is the duration of the new stack in seconds. The magnitude is always 30%.

## Stacking and Reapplication

Every application creates a separate stack with its own source and X-second timer, including applications from the same source. Only one fixed 30% penalty is effective regardless of stack count. Reapplication never refreshes or replaces another stack; when one expires, Decay remains active if any other stack remains.

## Timing Rules

Check whether any Decay stack is active when a Regeneration trigger calculates its healing amount.

## Valid Targets

Living combatants.

## Removal and Prevention

A generic Cleanse removes one Decay stack, choosing the earliest-expiring stack and then application order for ties. Natural expiration removes only that stack. Ward can prevent a new application. Encounter end removes all stacks.

## Interactions

Only Health Regeneration is reduced. Direct healing and Lifesteal are unaffected. [Renewal](renewal.md) applies an equal opposite modifier, so the two cancel while both are active. Wound and Recovery apply afterward to the healing actually received.

## Immunity and Resistance

X is the exact duration in seconds and is not reduced by Status Resistance.

## Examples

- **Ability text:** “Apply Decay(8).”
- **Hover text:** “Decay(X) lasts X seconds. While active, Health Regeneration is reduced by 30%. Applications have independent durations, but only one penalty is effective.”

Decay(8) reduces a Regeneration amount of 100 to 70 without changing how often Regeneration triggers.

## Implementation References

`LL/src/Core/Domain/Models/Attributes/AttributeType.cs`; `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition runtime; legacy flat `HealthRegeneration` modifiers remain supported.

## Related Entries

[Renewal](renewal.md) · [Regeneration](regeneration.md) · [Wound](wound.md) · [Stacking and duration](../stacking-and-duration.md)

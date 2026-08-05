# Unstoppable

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.unstoppable`                               |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Dispel or Expiration                                  |
| Primary Tags           | Buff, Control Immunity                                |
| Player-Facing Term     | Unstoppable                                           |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial                                            |
| Runtime IDs            | None                                                  |

## Definition

`Unstoppable(X)` grants control immunity for X seconds.

## Design Purpose

Create a clear anti-control window.

## Current Implementation

`ApplyCondition` grants exact X-second control immunity. Standard and tagged legacy control applications are rejected before Ward consumption.

## Canonical Target Behaviour

While active, prevent Control applications without consuming charges. Gaining Unstoppable does not remove existing Control; an applying ability must use a separate Cleanse operation to do so.

## Parameters

X is duration in seconds.

## Stacking and Reapplication

Unstoppable is Unique and has no stack magnitude. A successful reapplication replaces the remaining duration with the new X.

## Timing Rules

Prevention occurs before control status application.

## Valid Targets

Living combatants.

## Removal and Prevention

Dispel and expiry; encounter end.

## Interactions

Blocks Stun and Freeze when classified as control. Taunt is a beneficial Threat state and is not blocked.

## Immunity and Resistance

X is the exact duration and is not modified by Status Resistance or Crowd Control Resistance. Prevented controls do not proceed to their landing roll or duration calculation.

## Examples

- **Ability text:** “Gain Unstoppable(3).”
- **Hover text:** “Unstoppable(X) grants immunity to Control conditions for X seconds.”

Unstoppable(3) prevents a Stun or Freeze application attempted during its three-second window.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition application pipeline.

## Related Entries

[Stun](stun.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

# Freeze

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.freeze`                                    |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Hard Control, Cold, Freeze                            |
| Player-Facing Term     | Freeze                                                |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful, Control                                      |
| Runtime IDs            | `StandardConditionType.Freeze`                        |

## Definition

Attempts to apply cold hard control for X seconds with an 80% base landing chance.

## Design Purpose

Provide a cold-themed hard-control effect without redefining Stun behavior.

## Current Implementation

`ApplyCondition` performs one 80% base landing roll, applies the exact X-second duration on success, and blocks actions while active.

## Canonical Target Behaviour

`Freeze(X)` makes one application roll with an 80% base chance. On success, Freeze blocks active abilities, basic attacks, and basic-attack progress for exactly X seconds while allowing cooldowns, passive triggers, and timed effects to continue.

## Parameters

X is duration in seconds. Base landing chance is fixed at 80%. X converts to combat ticks at ten ticks per second and should be authored at 0.1-second precision.

## Stacking and Reapplication

Freeze is Unique and does not stack. A successful reapplication sets remaining duration to the new X seconds. A failed application does not apply Freeze or alter an existing Freeze.

## Timing Rules

Roll once when the effect attempts to apply Freeze. On success, control begins immediately and ends after X seconds. Failed rolls do not publish the successful condition-applied event.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse, Unstoppable, Ward, immunity, encounter end.

## Interactions

Freeze does not automatically consume Chill. An ability that intentionally consumes Chill must say so explicitly.

## Immunity and Resistance

The canonical base landing chance is 80%; no generic chance modifier is currently defined. Freeze's successful X-second duration is not shortened by Status Resistance or Crowd Control Resistance. Freeze immunity prevents application.

## Examples

- **Ability text:** “Deal 100% Magical Damage to a target, and apply Freeze(2).”

**Hover text:** “Freeze(X) has an 80% base chance to Freeze the target for X seconds. Freeze prevents active abilities and basic attacks.”

`Freeze(2)` has an 80% base chance to apply two seconds of hard control. Existing Chill retains its own stacks and duration.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Authored Freeze content uses the typed `ApplyCondition` contract.

## Related Entries

[Chill](chill.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

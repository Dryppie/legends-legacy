# Stun

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.stun`                                      |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Hard Control, Stun                                    |
| Player-Facing Term     | Stun                                                  |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful, Control                                      |
| Runtime IDs            | `StandardConditionType.Stun`                          |

## Definition

Attempts to prevent active abilities and basic attacks for X seconds with an 80% base landing chance.

## Design Purpose

Provide complete, temporary action denial.

## Current Implementation

`ApplyCondition` performs one 80% base landing roll, applies the exact X-second duration on success, and blocks actions while cooldowns and durations continue progressing.

## Canonical Target Behaviour

`Stun(X)` makes one application roll with an 80% base chance. On success, Stun blocks active abilities, basic attacks, and basic-attack progress for exactly X seconds while allowing cooldowns, passive triggers, and timed effects to continue.

## Parameters

X is duration in seconds. Base landing chance is fixed at 80%. X converts to combat ticks at ten ticks per second and should be authored at 0.1-second precision.

## Stacking and Reapplication

Stun is Unique and does not stack. A successful reapplication sets remaining duration to the new X seconds. A failed application does not apply Stun or alter an existing Stun.

## Timing Rules

Roll once when the effect attempts to apply Stun. On success, control begins immediately and ends after X seconds. Failed rolls do not publish the successful condition-applied event.

## Valid Targets

Living combatants.

## Removal and Prevention

Cleanse/removal; future Unstoppable/Ward; encounter end.

## Interactions

Freeze and Stun share hard-control behavior and landing chance, but retain separate identities, tags, immunity, and removal interactions.

## Immunity and Resistance

The canonical base landing chance is 80%; no generic chance modifier is currently defined. Stun's successful X-second duration is not shortened by Status Resistance or Crowd Control Resistance. Stun immunity prevents application.

## Examples

- **Ability text:** “Deal 100% Physical Damage to a target, and apply Stun(2).”

**Hover text:** “Stun(X) has an 80% base chance to Stun the target for X seconds. Stun prevents active abilities and basic attacks.”

`Stun(2)` has an 80% base chance to apply two seconds of hard control. A stunned unit gains no basic-attack progress, but its cooldowns continue.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Authored Stun content uses the typed `ApplyCondition` contract.

## Related Entries

[Freeze](freeze.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

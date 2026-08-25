# Silence

| Field                  | Value                           |
| ---------------------- | ------------------------------- |
| Stable ID              | `condition.silence`             |
| Status                 | Implemented                     |
| Default Stacking Model | Unique                          |
| Default Removal        | Cleanse or Expiration           |
| Primary Tags           | Debuff, Action Denial           |
| Player-Facing Term     | Silence                         |
| Known Aliases          | None                            |
| Classification         | Harmful                         |
| Runtime IDs            | `StandardConditionType.Silence` |

## Definition

Prevents active abilities for X seconds. Basic attacks, passive triggers, timed effects, durations, and cooldowns continue normally.

## Design Purpose

Temporarily disrupt an active-ability rotation without fully removing a combatant from play.

## Current Implementation

`ApplyCondition` converts the authored X seconds to combat ticks. Silence is Unique: a successful reapplication replaces its remaining duration. It does not use the 80% hard-control landing roll and is not blocked by Unstoppable.

## Removal and Prevention

Ward consumes one charge to negate a new Silence application. Cleanse removes an active Silence. It also ends through expiration, death, or encounter teardown.

## Example

“Deal 250% Magical Damage to a random enemy and Silence them for 15 seconds.”

## Implementation References

`LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; and `LL/tests/EssenceSystem.Tests/StandardConditionSystemTests.cs`.

## Related Entries

[Ward](ward.md) · [Stun](stun.md) · [Ability authoring](../ability-authoring.md)

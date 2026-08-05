# Thorns

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.thorns`                                    |
| Status                 | Implemented                                           |
| Default Stacking Model | Independent Stacks                                    |
| Default Removal        | Single-stack Dispel or Expiration                     |
| Primary Tags           | Buff, Reflection                                      |
| Player-Facing Term     | Thorns                                                |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial, Reactive                                  |
| Runtime IDs            | `StandardConditionType.Thorns`                        |

## Definition

`Thorns(X)` creates one independent timed stack that reflects X% of qualifying direct Health damage back to the attacker.

## Design Purpose

Discourage repeated direct attacks while allowing multiple Thorns applications and durations to coexist predictably.

## Current Implementation

`ApplyCondition` stores independent timed percentages, sums active stacks on qualifying direct Health damage, and emits terminal Reflected Damage. General event publication has a depth guard and reflection itself cannot recurse.

## Canonical Target Behaviour

When a combatant takes positive Health damage from an eligible direct hit, sum X across all active Thorns stacks and deal that percentage back to the attacker as Reflected Damage. Reflected damage cannot trigger Thorns or other reflection.

## Parameters

X is the percentage reflected by one stack. The applying effect supplies that stack's duration. There is no canonical stack count or total-reflection cap.

## Stacking and Reapplication

Every application creates a separate stack, including applications from the same source. Each stack stores its own X, source, and expiration time. Active X values are summed. Reapplication never refreshes, replaces, or extends another stack, and one stack expiring leaves every other stack unchanged.

## Timing Rules

Resolve after mitigation, Guard, Barrier, and Health loss are known. Sum all Thorns stacks active at the moment of the hit, then resolve one Reflected Damage event. A stack expiring at that timestamp is active for a hit that resolves before status expiration in the same combat tick.

## Valid Targets

Living combatants.

## Removal and Prevention

Natural expiration removes only that stack. A generic Dispel removes one stack, choosing the earliest-expiring stack and then application order for ties. Encounter end removes all stacks.

## Interactions

Only direct damage that reaches Health contributes to reflection; damage absorbed by Barrier does not. Guard reduces the amount before Thorns calculates reflection. Reflected damage cannot critically strike, trigger Lifesteal, consume Guard, or recursively trigger Thorns.

## Immunity and Resistance

No resistance for beneficial application.

## Examples

- **Ability text:** “Gain Thorns(20) for 8 seconds.”
- **Hover text:** “Thorns(X) reflects X% of qualifying direct Health damage back to the attacker. Each application has its own duration, and active percentages are added together.”

If Thorns(20) and Thorns(15) are both active when 100 damage reaches Health, the attacker takes 35 Reflected Damage. When Thorns(15) expires, Thorns(20) continues until its own expiration and the same hit would reflect 20.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented using Health damage after Barrier. Changing to Barrier-inclusive reflection would be a deliberate balance change.

## Related Entries

[Lifesteal](lifesteal.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

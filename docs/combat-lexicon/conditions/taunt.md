# Taunt

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.taunt`                                     |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Dispel or Expiration                                  |
| Primary Tags           | Buff, Threat, Taunt                                   |
| Player-Facing Term     | Taunt                                                 |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial, Threat                                    |
| Runtime IDs            | `StandardConditionType.Taunt`                         |

## Definition

Increases the taunter's Threat for X seconds so hostile Threat-weighted selection is more likely to choose it.

## Design Purpose

Redirect attacks through the shared Threat system while preserving the weighted-selection rule.

## Current Implementation

`ApplyCondition` applies exact X-second Taunt. Threat-weighted target selection adds the engine's configurable Taunt Threat bonus to the taunting combatant's underlying Threat.

## Canonical Target Behaviour

`Taunt(X)` applies the canonical Taunt Threat modifier to the user for exactly X seconds. Threat-Weighted Enemy selection continues to roll normally using the modified weight; Taunt does not bypass the selector.

## Parameters

X is duration in seconds. The Threat increase is a separate engine option that defaults to +100.

## Stacking and Reapplication

Taunt is Unique and does not stack. A successful reapplication sets remaining duration to the new X seconds.

## Timing Rules

Threat changes immediately on application and is read during target resolution. Taunt ends exactly X seconds after the most recent successful application.

## Valid Targets

Living combatants, normally self.

## Removal and Prevention

Dispel, expiration, death, or encounter end.

## Interactions

Area and multi-target effects ignore Threat. Stealth overrides effective Threat to 1 even while Taunt remains active; if Taunt outlasts Stealth, its underlying Threat modifier becomes effective again afterward.

## Immunity and Resistance

Taunt is a beneficial Threat state; Status Resistance and Crowd Control Resistance do not apply.

## Examples

- **Ability text:** “Gain Taunt(5).”

**Hover text:** “Taunt(X) increases Threat for X seconds. Hostile Threat-weighted targeting uses the increased Threat.”

`Taunt(5)` increases the user's Threat for five seconds. Reapplying `Taunt(8)` replaces the remaining duration with eight seconds.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

The default Taunt Threat bonus is 100 and can be configured through `FastCombatEngineOptions.TauntThreatBonus`.

## Related Entries

[Stealth](stealth.md) · [Targeting rules](../targeting-rules.md) · [Stacking and duration](../stacking-and-duration.md)

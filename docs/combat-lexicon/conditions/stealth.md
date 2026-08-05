# Stealth

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.stealth`                                   |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique                                                |
| Default Removal        | Dispel or Expiration                                  |
| Primary Tags           | Buff, Threat, Stealth                                 |
| Player-Facing Term     | Stealth                                               |
| Known Aliases          | None                                                  |
| Classification         | Beneficial, Threat                                    |
| Runtime IDs            | None                                                  |

## Definition

Sets the user's effective Threat to exactly 1 for X seconds.

## Design Purpose

Provide temporary protection from Threat-weighted targeting without deleting accumulated Threat or its modifiers.

## Current Implementation

Threat is a runtime modifier used by the Threat-weighted selector. `ApplyCondition` applies exact X-second Stealth and overrides final effective Threat to 1 while active.

## Canonical Target Behaviour

`Stealth(X)` sets effective Threat to exactly 1 for X seconds. This final override ignores every Threat buff, debuff, and other modifier while active.

## Parameters

X is duration in seconds. Effective Threat is fixed at 1 and is not an authored magnitude.

## Stacking and Reapplication

Stealth is Unique and does not stack. A successful reapplication sets remaining duration to the new X seconds.

## Timing Rules

The override begins immediately and ends exactly X seconds after the most recent successful application. Underlying base Threat and modifiers continue updating while hidden, but do not affect effective Threat until Stealth ends.

## Valid Targets

Living combatants, normally self.

## Removal and Prevention

Dispel, expiration, death, or encounter end.

## Interactions

Stealth overrides Taunt and every other Threat buff or debuff. When Stealth ends, effective Threat is recalculated from the current underlying Threat state. Threat-Weighted Enemy uses weight 1 for a stealthed candidate.

## Immunity and Resistance

Stealth is a beneficial Threat state; Status Resistance and Crowd Control Resistance do not apply.

## Examples

- **Ability text:** “Gain Stealth(6).”

**Hover text:** “Stealth(X) sets Threat to 1 for X seconds, ignoring all other Threat modifiers.”

`Stealth(6)` sets effective Threat to 1 for six seconds. Gaining Taunt during Stealth updates underlying state but does not change effective Threat until Stealth expires.

## Implementation References

`LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition and target-selection runtime.

## Related Entries

[Taunt](taunt.md) · [Targeting rules](../targeting-rules.md) · [Stacking and duration](../stacking-and-duration.md)

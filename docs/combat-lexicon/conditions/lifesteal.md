# Lifesteal

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.lifesteal`                                 |
| Status                 | Implemented                                           |
| Default Stacking Model | Strongest                                             |
| Default Removal        | Dispel or Expiration                                  |
| Primary Tags           | Buff, Lifesteal                                       |
| Player-Facing Term     | Lifesteal                                             |
| Known Aliases          | None                                                  |
| Classification         | Beneficial, Sustain                                   |
| Runtime IDs            | `LifeSteal` attribute and per-effect percentages      |

## Definition

Heals the source for a percentage of eligible damage dealt to health.

## Design Purpose

Convert offence into sustain using the same term as the existing `LifeSteal` combat attribute.

## Current Implementation

Basic attacks and eligible direct effect damage restore health from post-mitigation Health damage, capped at 50%. Periodic, reflected, stored, and self-damage do not qualify; Lifesteal healing cannot crit.

## Canonical Target Behaviour

Apply to eligible direct health damage, including basic attacks, excluding periodic/reflected/self damage by default; decide Healing Power interaction.

## Parameters

Percentage, cap, eligible categories.

## Stacking and Reapplication

Sources add into the capped percentage; timed sources reverse on expiry.

## Timing Rules

After health damage, before subsequent death events according to an explicit test.

## Valid Targets

Living damage source; target must take health damage.

## Removal and Prevention

Dispel timed buffs; encounter end.

## Interactions

Lifesteal healing is affected by Wound and Recovery. Thorns damage is excluded from generating Lifesteal by default.

## Immunity and Resistance

No status resistance for beneficial application.

## Examples

- **Ability text:** “Gain Lifesteal using its authored magnitude and duration.”

20% Lifesteal on 50 actual health damage restores 10 before other modifiers.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the direct-damage pipeline. Healing Power and target-side Wound/Recovery modifiers affect the resulting healing.

## Related Entries

[Thorns](thorns.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

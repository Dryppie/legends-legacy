# Regeneration

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.regeneration`                              |
| Status                 | Implemented                                           |
| Default Stacking Model | Unique, attribute-driven                              |
| Default Removal        | Not removable; attributes may be modified             |
| Primary Tags           | Mechanic, Healing, Interval                           |
| Player-Facing Term     | Regeneration                                          |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial mechanic                                   |
| Runtime IDs            | `HealthRegeneration` attribute                        |

## Definition

Builds regeneration progress over time and restores health whenever that progress reaches its interval threshold.

## Design Purpose

Provide persistent, tunable sustain analogous to basic-attack progression: rate controls how often it occurs and amount controls how much health it restores.

## Current Implementation

Regeneration uses carry-forward progress, publishes healing events, and restores the modified `HealthRegeneration` amount. `ModifyRegenerationRate` and `ModifyRegenerationInterval` let abilities tune speed independently from amount.

## Canonical Target Behaviour

Regeneration is a persistent combat mechanic, not a timed condition. Each combatant has regeneration progress. Progress increases every combat tick; reaching the interval threshold restores the configured amount and carries excess progress forward.

## Parameters

`RegenerationAmount` controls health restored per trigger. `RegenerationRate` controls progress gained per combat tick. `RegenerationInterval` is the progress threshold. Modifiers can independently increase or decrease amount and rate, with non-negative floors.

## Stacking and Reapplication

There are no condition instances to stack. Attribute modifiers combine under the attribute system and change either regeneration amount or regeneration rate.

## Timing Rules

Combat starts at zero regeneration progress. Progress behaves like basic-attack progress: rate modifiers make triggers faster or slower, excess progress carries forward, and at most one regeneration trigger resolves per combat tick.

## Valid Targets

Living combatants; ticks stop while dead.

## Removal and Prevention

Regeneration itself cannot be cleansed or dispelled. Attribute buffs or debuffs affecting its amount or rate follow their own removal rules. Progress resets at encounter end.

## Interactions

Decay and Renewal modify the amount restored per trigger without changing progress or interval. Regeneration then counts as healing and is affected by Wound, Recovery, and other Healing Received modifiers. Define whether it publishes `OnHeal` and `OnHealed` when the runtime mechanic is upgraded.

## Immunity and Resistance

Resistance does not apply to the mechanic. Harmful modifiers to regeneration amount or rate use the modifier's own resistance and removal rules.

## Examples

- **Ability text:** “Increase Regeneration Amount by 5.”

Doubling Regeneration Rate halves the progress time needed for the same interval; reducing Regeneration Amount changes healing without changing trigger timing.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the combat runtime. The default interval remains 50 ticks until an ability modifies it.

## Related Entries

[Decay](decay.md) · [Renewal](renewal.md) · [Wound](wound.md) · [Recovery](recovery.md) · [Stacking and duration](../stacking-and-duration.md)

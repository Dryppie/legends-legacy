# Ward

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.ward`                                      |
| Status                 | Implemented                                           |
| Default Stacking Model | Charges                                               |
| Default Removal        | Charge consumption only                               |
| Primary Tags           | Buff, Ward                                            |
| Player-Facing Term     | Ward                                                  |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Beneficial                                            |
| Runtime IDs            | None                                                  |

## Definition

Stores permanent charges that each negate one new harmful condition application.

## Design Purpose

Offer proactive condition defence distinct from cleansing.

## Current Implementation

`ApplyCondition` grants permanent charges. Harmful standard conditions and tagged legacy statuses are intercepted after immunity/landing checks, consuming one charge and canceling the whole application.

## Canonical Target Behaviour

`Ward(X)` grants X Ward charges. When a new Debuff, Affliction, Control, or other condition classified as harmful would successfully apply, consume one charge and cancel that entire application before state mutation.

## Parameters

X is the number of charges. Ward has no duration and no charge cap. Eligible effects are new harmful condition applications; direct damage, resource loss, and costs do not qualify.

## Stacking and Reapplication

Every application adds X charges to the existing Ward pool. Charges have no maximum, do not expire, and do not track separate durations.

## Timing Rules

First resolve target immunity and any landing roll. If the condition would land, Ward consumes one charge before status creation, application effects, and `OnStatusApplied`. Failed or immune applications do not consume Ward.

## Valid Targets

Living combatants.

## Removal and Prevention

Ward cannot be Cleansed, Dispelled, expired, stolen, or explicitly removed. Charges leave the pool only when they negate a qualifying application. Encounter teardown clears the combatant's transient combat state.

## Interactions

Ward negates the entire application: `Poison(3)`, `Chill(5)`, or `Corrosion(10)` each consumes one charge and applies zero stacks. Ward also blocks one Doom stack or one successful Freeze/Stun attempt. Unstoppable covers Control only and is checked before Ward so an already-immune application does not consume a charge.

## Immunity and Resistance

Ward is a permanent beneficial charge mechanic; Status Resistance and Crowd Control Resistance do not apply.

## Examples

- **Ability text:** “Gain Ward(3).”

**Hover text:** “Ward(X) grants X Ward charges. Each charge negates one new Debuff, Affliction, Control, or other negative condition, then is consumed. Ward does not expire.”

`Ward(2)` negates the next two qualifying harmful applications. A failed `Stun(2)` landing roll does not consume a charge.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented by the standard condition application pipeline. Legacy statuses must carry Debuff, Affliction, or Control tags to be recognized as harmful.

## Related Entries

[Unstoppable](unstoppable.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

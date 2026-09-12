# Exposed

| Field                  | Value                              |
| ---------------------- | ---------------------------------- |
| Stable ID              | `condition.exposed`                |
| Status                 | Proposed                           |
| Classification         | Harmful                            |
| Default Stacking Model | Unique                             |
| Default Removal        | Cleanse / Expiration               |
| Primary Tags           | Debuff                             |
| Player-Facing Term     | Exposed                            |
| Known Aliases          | None                               |
| Runtime IDs            | None                               |

## Definition

Damage against an Exposed target has 10% Increased Critical Chance. Exposed lasts 10 seconds.

## Design Purpose

Create a temporary focus-fire opportunity by making the target more susceptible to critical strikes from any attacker.

## Current Implementation

Proposed lexicon contract only. `StandardConditionType` has no Exposed entry, and `FastCombatEngine.RollCriticalStrike` accepts source and effect bonuses without an Exposed target modifier.

## Canonical Target Behaviour

The target owns one shared Exposed condition. For damage eligible to critically strike against that target, add 10 percentage points to the attacker's critical chance before the existing 75% cap. This is an additive chance bonus, not a 10% relative multiplier. It does not change the attacker's critical chance against other targets or increase critical damage.

## Parameters

The critical chance bonus is fixed at 10 percentage points; duration is fixed at 10 seconds. Ability text takes no magnitude, stack count, or duration parameter.

## Stacking and Reapplication

Exposed does not stack, regardless of source. Every successful application refreshes the target's one instance to 10 seconds without increasing the bonus.

## Timing Rules

The bonus applies to critical rolls resolved after successful application and before expiry or removal. Exposed expires 10 seconds (100 combat ticks) after its latest successful application. It has no periodic ticks and does not change damage already resolved.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse removes Exposed. Natural expiration and encounter end clear it. A specific immunity can prevent application; [Ward](ward.md) can negate an otherwise-successful application, including a refresh. Damage does not consume Exposed. Exposed is not control, so Unstoppable does not prevent it.

## Interactions

Exposed modifies chance only for damage already eligible to critically strike under the [damage-category rules](../damage-categories.md). It does not grant critical eligibility to damage that cannot critically strike, and it does not affect healing. [Vulnerable](vulnerability.md) independently amplifies qualifying direct-hit damage; both conditions may coexist.

## Immunity and Resistance

The fixed 10-second duration and critical chance bonus are not reduced by Status Resistance. Specific Exposed immunity prevents application.

## Examples

- **Ability text:** “Apply Exposed.”
- **Hover text:** “Damage against an Exposed target has 10% Increased Critical Chance. Lasts 10 seconds. Does not stack; reapplication refreshes its duration.”

An attacker with 20% critical chance has 30% against an Exposed target. An attacker with 70% reaches the existing 75% cap. Reapplying Exposed after 6 seconds resets its remaining duration to 10 seconds and preserves the same bonus.

## Implementation References

- `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`: `StandardConditionType` (no Exposed runtime ID).
- `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`: `RollCriticalStrike` (existing additive chance calculation and cap).

These references establish existing primitives, not implementation of Exposed.

## Known Differences or Open Questions

The shared runtime condition, target-side critical chance integration, authoring support, frontend presentation, and executable coverage are not implemented. Unique refresh and the additive interpretation above follow existing fixed-condition and critical chance conventions.

## Related Entries

[Vulnerable](vulnerability.md) · [Ward](ward.md) · [Stacking and duration](../stacking-and-duration.md) · [Critical formula](../formula-reference.md#critical) · [Combat tags](../combat-tags.md)

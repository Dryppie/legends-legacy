# Corrosion

| Field                  | Value                                                 |
| ---------------------- | ----------------------------------------------------- |
| Stable ID              | `condition.corrosion`                                 |
| Status                 | Implemented                                           |
| Default Stacking Model | Intensity Stacks                                      |
| Default Removal        | Cleanse or Expiration                                 |
| Primary Tags           | Debuff, Defence                                       |
| Player-Facing Term     | Corrosion                                             |
| Known Aliases          | See Runtime IDs below; no additional canonical alias. |
| Classification         | Harmful                                               |
| Runtime IDs            | None                                                  |

## Definition

Accumulates up to 50 stacks, with each stack reducing both Armor and Resistance by 1%.

## Design Purpose

Create escalating defence erosion that benefits both Physical and Magical damage builds.

## Current Implementation

`ApplyCondition` maintains a shared 50-stack count and reduces Armor and Resistance by 1% per stack before penetration.

## Canonical Target Behaviour

`Corrosion(X)` applies X Corrosion stacks. Each stack reduces both Armor and Resistance by 1%, to a maximum of 50 stacks and a 50% reduction. Corrosion is calculated before Armor or Magic Penetration.

## Parameters

One percent reduction to both defences per stack, maximum 50 stacks, and a fixed 12-second shared duration.

## Stacking and Reapplication

Add X stacks to the target's shared Corrosion count, up to 50. Every successful application refreshes the entire condition to 12 seconds, including an application received while already at 50 stacks. Stacks from all sources contribute to the same count.

## Timing Rules

Both defence reductions change immediately after stack mutation. All stacks expire together 12 seconds after the most recent successful application.

## Valid Targets

Living enemies.

## Removal and Prevention

Cleanse removes the entire Corrosion condition and all stacks in one operation. Expiration, explicit removal, and encounter end also remove every stack. Ward or Corrosion immunity may prevent application.

## Interactions

Corrosion modifies both Armor and Resistance before penetration. It affects Physical, Bleed, Magical, Burn, and Poison mitigation through their normal defence channels.

## Immunity and Resistance

Corrosion's fixed 12-second duration and percentage magnitude are not reduced by Status Resistance. A specific Corrosion immunity may prevent application.

## Examples

- **Ability text:** “Deal 100% Magical Damage to a target, and apply Corrosion(15).”

**Hover text:** “Corrosion(X) applies X stacks of Corrosion. Each stack decreases Armor and Resistance by 1%, up to 50 stacks. Corrosion lasts 12 seconds, and applying Corrosion refreshes its duration.”

`Corrosion(15)` reduces both Armor and Resistance by 15%. Applying `Corrosion(40)` afterward produces 50 stacks and refreshes the shared duration to 12 seconds.

## Implementation References

`LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`; `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`; `LL/src/API/API.LL/Data/combat/`; and `LL/tests/EssenceSystem.Tests/`.

## Known Differences or Open Questions

Implemented for typed standard conditions; legacy flat defence modifiers retain their authored behavior.

## Related Entries

[Vulnerable](vulnerability.md) · [Stacking and duration](../stacking-and-duration.md) · [Combat tags](../combat-tags.md)

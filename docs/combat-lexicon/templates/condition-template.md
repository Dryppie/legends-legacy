# {Condition Name}

| Field                  | Value                                                                 |
| ---------------------- | --------------------------------------------------------------------- |
| Stable ID              | `condition.{lowercase-kebab-id}`                                      |
| Status                 | Implemented / Partially Implemented / Proposed / Deprecated / Unknown |
| Classification         | Beneficial / Harmful / Control / Marker                               |
| Default Stacking Model | Strongest / Intensity Stacks / Per Source / Charges / Pool / Unique   |
| Default Removal        | Cleanse / Dispel / Expiration / Consumption / Not Removable           |
| Primary Tags           | Relevant linked tags                                                  |
| Player-Facing Term     | Canonical display name                                                |
| Known Aliases          | Repository terms or None                                              |
| Runtime IDs            | Exact IDs, or None                                                    |

## Definition

One precise sentence describing observable behavior.

## Design Purpose

Why this condition exists and what gameplay decision it creates.

## Current Implementation

Repository-backed behavior only. Distinguish primitives, authored instances, and shared semantics.

## Canonical Target Behaviour

Desired shared contract. State source ownership and affected operations.

## Parameters

Magnitude, stacks, duration, interval, cap, charges, or threshold.

## Stacking and Reapplication

Identity key, maximum stacks, refresh/replace rules, and per-source behavior.

## Timing Rules

Application phase, tick interval, expiry boundary, and event order.

## Valid Targets

Living/dead, ally/enemy/self/summon restrictions.

## Removal and Prevention

Cleanse, dispel, immunity, ward, unstoppable, death, encounter reset.

## Interactions

Links to related conditions and ordering rules.

## Immunity and Resistance

Applicable resistance, duration scaling, magnitude scaling, and immunity.

## Examples

At least one concrete authored and/or canonical example.

## Implementation References

Exact repository paths, symbols, runtime IDs, and tests.

## Known Differences or Open Questions

Material gaps only; say “None” when complete.

## Related Entries

Relative links to related catalogue pages.

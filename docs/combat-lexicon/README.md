# Combat Lexicon and Catalogue

This catalogue is the repository-backed source of truth for combat terminology. It separates behavior verified in the current engine from intended design. An entry's **Status** is therefore as important as its canonical definition.

## Evidence and authority

When sources disagree, use this order:

1. executable tests and `FastCombatEngine`;
2. domain models and compiler validation;
3. authored ability/status data;
4. frontend presentation;
5. older design documents.

“Canonical target behaviour” describes the desired shared contract. “Current implementation” describes what the repository does today.

## Conditions

| ID                                                     | Name          | Status                | Summary                                                      |
| ------------------------------------------------------ | ------------- | --------------------- | ------------------------------------------------------------ |
| [condition.haste](conditions/haste.md)                 | Haste         | Implemented | Unique +25% Attack Speed for 10s; reapplication refreshes.   |
| [condition.slow](conditions/slow.md)                   | Slow          | Implemented | Unique -25% Attack Speed for 10s; reapplication refreshes.   |
| [condition.empower](conditions/empower.md)             | Empower       | Implemented | Unique +20% Power for 10s; reapplication refreshes.          |
| [condition.weaken](conditions/weaken.md)               | Weaken        | Implemented | Unique -20% Power for 10s; reapplication refreshes.          |
| [condition.vulnerability](conditions/vulnerability.md) | Vulnerable    | Implemented | Permanent uncapped stacks; each adds 25% direct-hit damage.  |
| [condition.regeneration](conditions/regeneration.md)   | Regeneration  | Implemented | Adjustable amount, rate, interval, and progress.             |
| [condition.wound](conditions/wound.md)                 | Wound         | Implemented | Independent X-second stacks; one -30% healing modifier.      |
| [condition.recovery](conditions/recovery.md)           | Recovery      | Implemented | Independent X-second stacks; one +30% healing modifier.      |
| [condition.decay](conditions/decay.md)                 | Decay         | Implemented | Independent X-second stacks; one -30% Regeneration modifier. |
| [condition.renewal](conditions/renewal.md)             | Renewal       | Implemented | Independent X-second stacks; one +30% Regeneration modifier. |
| [condition.barrier](conditions/barrier.md)             | Barrier       | Implemented | Capped source-tracked pool with lifecycle events.            |
| [condition.guard](conditions/guard.md)                 | Guard         | Implemented | Permanent charges; each reduces one direct hit by 25%.       |
| [condition.ward](conditions/ward.md)                   | Ward          | Implemented | Permanent charges; each negates one new harmful condition.   |
| [condition.unstoppable](conditions/unstoppable.md)     | Unstoppable   | Implemented | X-second Unique control-immunity window.                     |
| [condition.poison](conditions/poison.md)               | Poison        | Implemented | 1% Magical per stack, every 2s for 12s.                      |
| [condition.burn](conditions/burn.md)                   | Burn          | Implemented | 1% Magical per stack, every 1s for 4s.                       |
| [condition.bleed](conditions/bleed.md)                 | Bleed         | Implemented | 1% Physical per stack, every 2s for 8s.                      |
| [condition.stun](conditions/stun.md)                   | Stun          | Implemented | 80% base chance for X seconds of hard control.               |
| [condition.taunt](conditions/taunt.md)                 | Taunt         | Implemented | Taunt(X) increases weighted Threat for X seconds.            |
| [condition.stealth](conditions/stealth.md)             | Stealth       | Implemented | Stealth(X) overrides effective Threat to 1 for X seconds.    |
| [condition.chill](conditions/chill.md)                 | Chill         | Implemented | Up to 20 stacks; each applies -1% Attack Speed for 10s.      |
| [condition.freeze](conditions/freeze.md)               | Freeze        | Implemented | 80% base chance for X seconds of hard control.               |
| [condition.corrosion](conditions/corrosion.md)         | Corrosion     | Implemented | Up to 50 stacks; each reduces both defences by 1% for 12s.   |
| [condition.doom](conditions/doom.md)                   | Doom          | Implemented | Doom(X) deals X% snapshotted Power after 15 seconds.         |
| [condition.thorns](conditions/thorns.md)               | Thorns        | Implemented | Independent timed reflection percentages sum.               |
| [condition.lifesteal](conditions/lifesteal.md)         | Lifesteal     | Implemented | Eligible direct damage restores health within the 50% cap.   |

Status totals: **26 Implemented**, **0 Partially Implemented**, **0 Proposed**, **0 Deprecated**, **0 Unknown**.

## Reference

- [Design principles](design-principles.md)
- [Core concepts](core-concepts.md)
- [Ability authoring](ability-authoring.md)
- [Combat tags](combat-tags.md)
- [Combat verbs](combat-verbs.md)
- [Targeting rules](targeting-rules.md)
- [Trigger events](trigger-events.md)
- [Damage types](damage-types.md)
- [Damage categories](damage-categories.md)
- [Stacking and duration](stacking-and-duration.md)
- [Formula reference](formula-reference.md)
- [Catalogue audit](catalogue-audit.md)
- [Contributing](contributing.md)
- [Condition index](conditions/README.md)
- [Templates](templates/condition-template.md)

## Scope

The standard-condition contracts are implemented by the typed ability and combat runtime. Authored
Burn, Bleed, Poison, Chill, Freeze, Stun, Empower, Weaken, Vulnerable, Taunt, Decay, and Thorns
effects use `ApplyCondition`. Legacy `ApplyStatus` remains available only for bespoke status
behaviour that has no standard-condition equivalent.

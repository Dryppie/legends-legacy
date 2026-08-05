# Trigger Events

## Event catalogue

| Stable ID                         | Runtime event     | Status                  | Publication boundary                                   |
| --------------------------------- | ----------------- | ----------------------- | ------------------------------------------------------ |
| `trigger.on-combat-start`         | `OnCombatStart`   | Implemented             | Before the first combat tick.                          |
| `trigger.on-ability-activated`    | `OnAbilityUsed`   | Implemented             | After costs/cooldown start, before effects.            |
| `trigger.on-basic-attack-started` | `OnBasicAttack`   | Implemented             | Before basic-attack damage.                            |
| `trigger.on-hit`                  | `OnHit`           | Implemented             | After a non-dodged direct hit and Barrier mutation.    |
| `trigger.on-damage-taken`         | `OnDamaged`       | Implemented             | After `OnHit` and typed attack events.                 |
| `trigger.on-attacked`             | `OnAttacked`      | Implemented             | After `OnDamaged`.                                     |
| `trigger.on-melee-attack`         | `OnMeleeAttack`   | Implemented             | For melee damage after `OnHit`.                        |
| `trigger.on-ranged-attack`        | `OnRangedAttack`  | Implemented             | For ranged damage after `OnHit`.                       |
| `trigger.on-health-changed`       | `OnHealthChanged` | Implemented             | Only when health, not only Barrier, changes.           |
| `trigger.on-healing-done`         | `OnHeal`          | Implemented             | After non-zero health restoration.                     |
| `trigger.on-healing-received`     | `OnHealed`        | Implemented             | After `OnHeal`.                                        |
| `trigger.on-lifesteal-heal`       | `OnLifestealHeal` | Implemented             | After lifesteal restores health.                       |
| `trigger.on-dodge`                | `OnDodge`         | Implemented             | A dodge exits damage processing.                       |
| `trigger.on-kill`                 | `OnKill`          | Implemented             | Before the victim's death event.                       |
| `trigger.on-death`                | `OnDeath`         | Implemented             | After kill event; owner summons then expire.           |
| `trigger.on-condition-applied`    | `OnStatusApplied` | Implemented             | After status mutation/application effects.             |
| `trigger.on-condition-expired`    | `OnStatusExpired` | Implemented             | After natural duration expiry only.                    |
| `trigger.on-condition-removed`    | `OnStatusRemoved` | Implemented             | After explicit removal, replacement, or charge consumption. |
| `trigger.on-condition-cleansed`   | `OnStatusCleansed` | Implemented            | After Cleanse removes a harmful condition.             |
| `trigger.on-condition-dispelled`  | `OnStatusDispelled` | Implemented           | After Dispel removes a beneficial condition.           |
| `trigger.every-x-seconds`         | `OnInterval`      | Implemented             | At tick zero, then according to the trigger internal cooldown. |
| `trigger.on-critical-hit`         | None              | Proposed                | Critical is telemetry/event data, not a trigger event. |
| `trigger.on-barrier-applied`      | `OnBarrierApplied` | Implemented            | After a grant is capped and its source contribution is recorded. |
| `trigger.on-barrier-absorbed`     | `OnBarrierAbsorbed` | Implemented           | Once per consumed source contribution, after pool mutation. |
| `trigger.on-barrier-broken`       | `OnBarrierBroken` | Implemented             | Once when a positive Barrier total reaches zero.       |
| `trigger.on-combat-end`           | None              | Proposed                | Accepted tag only.                                     |

Direct damage publishes `OnHit` after damage/Barrier mutation even when Barrier absorbs all damage. Periodic, stored, reflected, and self-damage do not publish direct-hit events. A dodge returns early and publishes only `OnDodge`. Death publishes `OnKill` before `OnDeath`.

## Interval cadence

`OnInterval` is published once for each living combatant at the start of every combat tick. It is owner-scoped: only that combatant's abilities and statuses receive its interval event. The trigger's `internalCooldownTicks` is the repeat cadence. A positive cooldown `X` fires at tick zero and every `X` ticks thereafter; zero fires every tick.

## Removal reasons

Removal events publish after state mutation. Natural duration expiry is exclusively `OnStatusExpired`. Explicit `RemoveStatus`, replacement, and charge depletion publish `OnStatusRemoved`; Cleanse publishes `OnStatusCleansed`; Dispel publishes `OnStatusDispelled`. The combat log mirrors these reasons with distinct event types.

## Barrier ownership and payload

Barrier events use the contribution source as event source and the Barrier owner as event target. Applied and absorbed events carry the affected amount and contribution application order. Absorption also retains the attacking instigator. Because a hit may consume contributions from several sources, it publishes one `OnBarrierAbsorbed` event and combat-log item per contribution in oldest-first order. `OnBarrierBroken` is attributed to the final depleted contribution.

## Tags without runtime events

The tag catalogue accepts terms including `OnCombatEnd`, `OnCrit`, `OnBlock`, `OnBarrierBreak`, `OnLowHealth`, and `OnSummonDeath`; these are not current `AbilityTriggerEvent` implementations.

## Reentrancy

Publication is synchronous and capped at 64 nested events. Canonical Reflected Damage is terminal and cannot trigger reflection, so opposing Thorns effects do not recurse.

Triggers may define internal cooldowns and use limits. Non-qualifying events do not consume them. There is no engine-wide “once per originating hit” identity, so custom legacy reactive effects should still be authored conservatively.

A failed Freeze or Stun landing roll does not publish `OnStatusApplied`; only successful state mutation publishes the event.

Ward-blocked applications also do not publish `OnStatusApplied`. Ward consumes after immunity and landing checks but before condition state or application effects are created.

Implementation: `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`, `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`, and `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`.

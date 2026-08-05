# Core Concepts

| Stable ID           | Concept             |
| ------------------- | ------------------- |
| `concept.ability`   | Ability             |
| `concept.trigger`   | Trigger             |
| `concept.effect`    | Effect              |
| `concept.status`    | Runtime status      |
| `concept.condition` | Catalogue condition |
| `concept.source`    | Source              |
| `concept.target`    | Target              |
| `concept.tick`      | Combat tick         |

## Ability

An authored `AbilitySpec` containing triggers and effects. Active abilities publish `OnAbilityUsed`; basic attacks publish `OnBasicAttack`.

## Trigger

A rule evaluated synchronously when its event is published. A trigger chooses targets, checks conditions, and applies effects. Publication is protected by a maximum recursion depth of 64.

## Effect

An operation such as damage, heal, barrier, status application, attribute modification, summon, or resource restoration.

## Status and condition

A runtime status is one status ID on one combatant, with stacks, duration, tags, triggers, and owned effects. A catalogue condition is a stable design contract; it may be implemented by a status, attribute effect, engine rule, or combination.

## Source, target, and event participants

- **Source:** combatant credited with an ability/effect.
- **Target:** combatant receiving the effect.
- **Event source/target:** participants carried by the current event.
- **Owner:** summoner for summoned combatants.

## Tick

Combat runs at ten ticks per second. Actions resolve before timed effects, statuses, regeneration, cooldown decrement, status-duration decrement, and summon expiry for that tick.

## Primitive versus contract

`ModifyAttribute(Armor, -20)` proves that Armor can be modified; it does not create a separate named condition. Likewise, acceptance by `EssenceTagCatalog` proves vocabulary validation, not engine behavior.

## Condition stack notation

Player-facing ability descriptions use `Condition(X)` to expose the condition's primary numeric parameter. The hover definition determines what X means:

- `Poison(3)` means “apply three Poison stacks.”
- `Burn(2)` means “apply two Burn stacks.”
- `Bleed(4)` means “apply four Bleed stacks.”
- `Chill(5)` means “apply five Chill stacks.”
- `Corrosion(15)` means “apply fifteen Corrosion stacks.”
- `Doom(40)` means “apply one Doom stack that later deals 40% Power as Magical Damage.”
- `Freeze(2)` and `Stun(2)` mean “attempt to apply two seconds of hard control.”
- `Guard(3)` means “grant three Guard charges.”
- `Ward(3)` means “grant three Ward charges.”
- `Thorns(20)` means “grant one Thorns stack that reflects 20% of qualifying direct Health damage.”
- `Unstoppable(3)` means “grant control immunity for three seconds.”
- `Vulnerable(2)` means “apply two permanent stacks, each granting 25% incoming direct-hit damage.”
- `Wound(6)` and `Recovery(6)` mean “apply a six-second healing-received modifier.”
- `Decay(8)` and `Renewal(8)` mean “apply an eight-second Health Regeneration modifier.”
- `Taunt(5)` and `Stealth(5)` mean “apply the Threat state for five seconds.”

For Bleed, Burn, Poison, Chill, Corrosion, and Vulnerable, X is a stack count. For Doom, X is its Power percentage. For Thorns, X is the reflected-damage percentage of one independent stack. For Freeze, Stun, Taunt, Stealth, Unstoppable, Wound, Recovery, Decay, and Renewal, X is duration in seconds. For Guard and Ward, X is a charge count. Hovering the term explains the parameter:

> Poison(X) applies X stacks of Poison. Each stack deals 1% Magical Damage every 2 seconds for 12 seconds.

Bleed, Burn, and Poison use a fixed canonical value of 1% of the applier's Power per stack per tick. The applying effect supplies only the stack count. Each condition owns its schedule:

| Condition | Damage channel | Interval | Duration | Ticks |
|---|---|---:|---:|---:|
| Burn | Magical | 1 second | 4 seconds | 4 |
| Bleed | Physical | 2 seconds | 8 seconds | 4 |
| Poison | Magical | 2 seconds | 12 seconds | 6 |

For Bleed, Burn, and Poison, stacks created by one `Condition(X)` operation remain independent but share the same source, Power snapshot, damage, interval, duration, and initial tick progress. Chill and Corrosion instead add X stacks to one capped condition with a shared refreshed duration.

## References

Implementation: `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`, `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/AbilityRuntime.cs`, `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`, and `LL/src/Core/Domain/Models/Attributes/AttributeCombatRules.cs`.

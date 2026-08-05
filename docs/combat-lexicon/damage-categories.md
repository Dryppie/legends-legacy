# Damage Categories

Delivery category is separate from [damage type](damage-types.md). The engine directly stores `AttackType`, while several canonical categories require additional causality metadata.

| Stable ID                   | Current representation                           | Crit default                         | On-hit / Lifesteal / reflection                                                     | Recursion                         |
| --------------------------- | ------------------------------------------------ | ------------------------------------ | ----------------------------------------------------------------------------------- | --------------------------------- |
| `damage-category.direct`    | `Melee`, `Ranged`, or `None` non-periodic effect | Eligible for active/basic damage     | `OnHit` fires; may trigger Lifesteal/reflection; consumes one Guard charge if eligible | Event depth is guarded           |
| `damage-category.periodic`  | `DamageOverTime` or periodic runtime effect      | Ineligible                            | Does not trigger on-hit, Lifesteal, Guard, or reflection                            | Terminal for direct-hit reactions |
| `damage-category.triggered` | Trigger-applied `Damage`                         | Depends on effect flags              | No distinct category flag                                                           | Originating-event identity absent |
| `damage-category.reflected` | Internal standard damage delivery                | Ineligible                           | Cannot trigger on-hit, Lifesteal, Guard, or another reflection                       | Terminates reflection chains      |
| `damage-category.splash`    | Multi-target trigger/effect composition          | Depends on each effect               | No distinct category flag                                                           | Same as component effects         |
| `damage-category.chain`     | No general chain selector                        | Proposed                             | Not implemented                                                                     | Not implemented                   |
| `damage-category.stored`    | Internal standard Doom delivery                  | Ineligible                           | Does not trigger Lifesteal, Guard, reflection, or on-hit effects                    | One resolution per stored stack   |
| `damage-category.execute`   | Conditions plus damage composition               | Proposed                             | No distinct category flag                                                           | Same as component effect          |
| `damage-category.self`      | Internal source-equals-target delivery           | Ineligible                           | Does not trigger Lifesteal, Guard, reflection, or direct-hit effects                | Terminal for direct-hit reactions |

Canonical periodic damage does not critically strike, trigger on-hit effects, trigger Lifesteal, or recursively apply itself by default. It preserves source attribution, uses applicable outgoing/incoming modifiers, and continues after the source dies unless explicitly removed. The typed standard-condition runtime implements these rules.

Canonical Reflected Damage is a terminal delivery category. Thorns calculates it from qualifying direct damage that reaches Health, attributes it to the Thorns bearer, and deals it to the original attacker. It cannot critically strike or start on-hit, Lifesteal, Guard, or reflection processing.

Canonical Vulnerable amplification applies only to `damage-category.direct`. It does not amplify periodic, reflected, stored, or self-damage.

At the executable `AttackType` level:

| Attack type      | Dodge/block eligible | Periodic |
| ---------------- | -------------------: | -------: |
| `Melee`          |                  yes |       no |
| `Ranged`         |                  yes |       no |
| `DamageOverTime` |                   no |      yes |
| `None`           |                   no |       no |

Direct active damage and basic attacks can critically hit by default. Periodic damage cannot unless `CritEligibility` explicitly allows it. A type such as `Bleed` does not itself make damage periodic.

The `EffectTag` enum separately records delivery descriptors such as `Slashing`, `Blunt`, `Piercing`, `Arrows`, and `Spells`; these currently do not alter the mitigation pipeline.

Implementation: `LL/src/Core/Domain/Models/Damages/AttackType.cs`, `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`, and `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`.

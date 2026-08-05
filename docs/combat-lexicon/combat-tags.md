# Combat Tags

Tags are descriptors, not effects. Matching is string-based; only explicit engine checks give a tag executable behavior. “Implemented” below means the tag is verified in current content or code, not that it independently performs its label.

## Delivery and shape

| Stable ID                 | Status      | Exact meaning                                                                     | Does not mean / aliases                                 |
| ------------------------- | ----------- | --------------------------------------------------------------------------------- | ------------------------------------------------------- |
| `tag.delivery.melee`      | Implemented | Authored effect/ability is melee-delivered (`Range.Melee` or `AttackType.Melee`). | Does not guarantee single target.                       |
| `tag.delivery.ranged`     | Implemented | Ranged delivery (`Range.Ranged` or `AttackType.Ranged`).                          | Does not require a projectile object.                   |
| `tag.delivery.projectile` | Proposed    | A traveling projectile delivery model.                                            | `Arrows` is an `EffectTag`, not projectile simulation.  |
| `tag.shape.single-target` | Implemented | Authored for one target (`Pattern.SingleTarget`).                                 | Actual count is controlled by selector.                 |
| `tag.shape.multi-target`  | Implemented | Authored for multiple targets (`Pattern.MultiTarget`).                            | Alias: area; no geometry implied.                       |
| `tag.shape.area`          | Proposed    | Targets a defined area.                                                           | Accepted vocabulary does not create spatial area rules. |
| `tag.shape.chain`         | Proposed    | Resolves an ordered target chain.                                                 | No chain selector exists.                               |
| `tag.shape.splash`        | Proposed    | Secondary targets around a primary target.                                        | No adjacency/distance model exists.                     |
| `tag.shape.aura`          | Proposed    | Repeatedly affects units meeting an aura membership rule.                         | Periodic effects are not inherently auras.              |
| `tag.shape.trap`          | Proposed    | Delayed location-owned effect.                                                    | No location model exists.                               |
| `tag.delivery.summon`     | Implemented | Uses or describes summoned combatants (`Summon`).                                 | Does not imply owner death/duration rules by itself.    |

## Element and theme

| Stable ID               | Status      | Exact meaning                                 | Does not mean / aliases                                                      |
| ----------------------- | ----------- | --------------------------------------------- | ---------------------------------------------------------------------------- |
| `tag.element.magical`   | Implemented | Authored magical theme (`Element.Magical`).   | Mitigation still comes from `DamageType`.                                    |
| `tag.element.fire`      | Implemented | Fire theme (`Element.Fire`).                  | Does not automatically apply Burn.                                           |
| `tag.element.cold`      | Implemented | Frost/cold theme (`Element.Frost`).           | Alias: Frost; does not automatically Chill/Freeze.                           |
| `tag.element.dark`      | Implemented | Shadow theme (`Element.Shadow`).              | Alias: Shadow; no separate resistance.                                       |
| `tag.element.physical`  | Proposed    | Physical theme accepted by the tag catalogue. | Physical damage is implemented as a damage type, not tag behavior.           |
| `tag.element.lightning` | Proposed    | Lightning theme.                              | No Lightning damage channel exists.                                          |
| `tag.element.poison`    | Proposed    | Poison theme.                                 | `Status.Poison` and Poison damage exist; no separate elemental tag behavior. |
| `tag.element.nature`    | Proposed    | Nature theme.                                 | No separate damage/resistance channel.                                       |
| `tag.element.holy`      | Proposed    | Holy theme.                                   | Accepted effect vocabulary only.                                             |
| `tag.element.arcane`    | Proposed    | Arcane theme.                                 | No separate damage/resistance channel.                                       |

## Mechanical classification

| Stable ID                        | Status                | Exact meaning                                                             | Does not mean / aliases                                   |
| -------------------------------- | --------------------- | ------------------------------------------------------------------------- | --------------------------------------------------------- |
| `tag.mechanic.direct-damage`     | Implemented           | Authored as `Damage.Direct`.                                              | Does not select a damage type.                            |
| `tag.mechanic.periodic`          | Implemented           | Authored as `Pattern.Periodic`.                                           | Does not by itself schedule ticks.                        |
| `tag.mechanic.timed-buff`        | Implemented           | Authored as `Pattern.TimedBuff`.                                          | Closest current alias for Buff; no polarity model.        |
| `tag.mechanic.affliction`        | Partially Implemented | Specific `Status.Bleed`, `Status.Burn`, and `Status.Poison` labels exist. | No generic Affliction classification is consumed.         |
| `tag.mechanic.control`           | Partially Implemented | `Control.*` family labels control concepts.                               | Only Stun/Taunt have executable checks.                   |
| `tag.mechanic.hard-control`      | Partially Implemented | Stun and Freeze classification.                                           | No generic Hard Control tag/query exists.                 |
| `tag.mechanic.barrier`           | Implemented           | `Defense.Barrier` describes Barrier content.                              | The tag does not grant Barrier; the operation does.       |
| `tag.mechanic.curse`             | Implemented           | `Status.Curse` classification used by authored content.                   | It is not a universal condition or removal class.         |
| `tag.mechanic.healing-over-time` | Proposed              | Periodic healing classification.                                          | Health Regeneration is a fixed engine tick, not this tag. |

## Executable checks

| Runtime tag     | Current executable meaning                                                               |
| --------------- | ---------------------------------------------------------------------------------------- |
| `Control.Stun`  | Blocks active/basic actions and basic-attack progress; selects Crowd Control Resistance. |
| `Control.Taunt` | Makes the tagged combatant a preferred enemy target for current/random/basic targeting.  |

`Control.Freeze` is authored but not checked by action blocking. Accepted tags such as `Status.Chill`, `Target.Adjacent`, and `Trigger.OnCrit` are vocabulary only.

Known aliases are recorded in the tables; do not add synonyms without a real distinction. Runtime tags use PascalCase dot-separated segments, while lexicon stable IDs use lowercase namespaced kebab-case.

Implementation: `LL/src/Core/Domain/Models/Essences/Definitions/EssenceTagCatalog.cs`, `LL/src/API/API.LL/Data/combat/`, `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs`, and `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`. See the [audit](catalogue-audit.md).

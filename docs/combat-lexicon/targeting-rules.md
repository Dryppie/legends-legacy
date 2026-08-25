# Targeting Rules

All selectors operate on living combatants unless noted.

| Stable ID                        | Selector                                                   | Eligibility and priority                                                | Tie/no-target behavior                                                       |
| -------------------------------- | ---------------------------------------------------------- | ----------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| `target.self`                    | `Self`                                                     | Acting living combatant.                                                | No target if actor is unavailable.                                           |
| `target.threat-weighted-enemy`   | Threat-Weighted Enemy (`CurrentTarget` runtime enum)       | One living enemy. Each eligible enemy's Threat is its selection weight. | Weighted random roll; encounter order if every weight is zero; empty if none. |
| `target.source`                  | `Source`                                                   | Effect source carried by context.                                       | Empty if absent/dead.                                                        |
| `target.event-source`            | `EventSource`                                              | Living event source.                                                    | Empty if absent/dead.                                                        |
| `target.event-target`            | `EventTarget`                                              | Living event target.                                                    | Empty if absent/dead.                                                        |
| `target.random-enemy`            | `RandomEnemy`                                              | All living enemies, including summons; ignores Threat and Taunt.         | Uniform engine RNG; empty if none.                                           |
| `target.two-random-enemies`      | `TwoRandomEnemies`                                         | Two distinct living enemies, including summons; ignores Threat and Taunt. | Uniform engine RNG; returns fewer than two if needed.                        |
| `target.three-random-enemies`    | `ThreeRandomEnemies`                                       | Three distinct living enemies, including summons; ignores Threat and Taunt. | Uniform engine RNG; returns fewer than three if needed.                      |
| `target.highest-condition-stacks-enemy` | `HighestConditionStacksEnemy`                       | Living enemy, including summons, with the most stacks of the authored `targetCondition`; ignores Threat and Taunt. | Uniform engine RNG among ties, including the all-zero case; empty if none. |
| `target.lowest-health-enemy`     | `LowestHealthEnemy`                                        | Living enemy with the lowest Health percentage by default. Hard Taunt may override it. | Encounter order by default; empty if none.                                   |
| `target.highest-health-enemy`    | `HighestHealthEnemy`                                       | Living enemy with the highest raw current Health by default. Hard Taunt may override it. | Encounter order by default; empty if none.                                   |
| `target.lowest-health-ally`      | `LowestHealthAlly`                                         | Living ally including self, ordered by raw current health.              | Encounter order; empty if none.                                              |
| `target.all-enemies`             | `AllEnemies`                                               | Every living enemy.                                                     | Encounter order; empty collection if none.                                   |
| `target.all-allies`              | `AllAllies`                                                | Every living ally including self.                                       | Encounter order.                                                             |
| `target.everyone-but-self`       | `EveryoneButSelf`                                          | Every other living combatant.                                           | Encounter order.                                                             |
| `target.two-enemies`             | `TwoEnemies`                                               | First two living enemies.                                               | Encounter order; returns fewer than two if needed.                           |
| `target.two-allies`              | `TwoAllies`                                                | First two living allies including self.                                 | Encounter order; returns fewer than two if needed.                           |
| `target.highest-max-health-ally` | `HighestMaxHealthAlly`                                     | Living ally with highest maximum health.                                | Encounter order; empty if none.                                              |
| `target.summoned-allies`         | `SummonedAllies`                                           | Living allied summons.                                                  | Encounter order.                                                             |
| `target.non-summoned-allies`     | `NonSummonedAllies`                                        | Living non-summoned allies.                                             | Encounter order.                                                             |
| `target.summoned-enemies`        | `SummonedEnemies`                                          | Living enemy summons.                                                   | Encounter order.                                                             |

For Threat-Weighted Enemy, calculate `weight = max(0, enemy Threat)`, roll once across the sum of eligible weights, and select the enemy whose cumulative range contains the roll. Zero-weight enemies cannot be selected while any positive weight exists. If every eligible enemy has zero Threat, select the first living enemy to preserve deterministic behavior for legacy content that has not authored Threat.

[Stealth](conditions/stealth.md) sets a candidate's effective Threat weight to exactly 1 after all other Threat modifiers, including [Taunt](conditions/taunt.md). Taunt remains active underneath Stealth and becomes effective again if its duration outlasts Stealth.

Other tie-breaking generally follows encounter insertion order. `ModifyThreat` gives abilities a timed or permanent additive Threat modifier. Taunt adds the configurable `FastCombatEngineOptions.TauntThreatBonus`, which defaults to 100.

Dead units never qualify in current selectors. The engine has no separate untargetable or hidden state. Summons qualify unless a selector filters them.

Canonically, basic attacks and effects using Threat-Weighted Enemy share the same weighted roll. Taunt changes Threat rather than bypassing target selection. Area, multi-target, `RandomEnemy`, `TwoRandomEnemies`, and `HighestConditionStacksEnemy` selectors do not use Threat. See [Taunt](conditions/taunt.md).

An effect using `TwoRandomEnemies` or `ThreeRandomEnemies` may set `excludeEventTarget: true`. When its trigger carries an event target, that combatant is removed from the eligible pool before the random sample, allowing secondary arcs to guarantee “other” targets without a separate selector.

Health-extremum effects may opt into additional reusable constraints. `useHealthPercentage: true` compares current Health divided by Max Health, `excludeSummons: true` removes summoned candidates, `ignoreTaunt: true` prevents hard Taunt from replacing the selected extremum, and `randomizeTies: true` uses uniform reservoir sampling among equal extrema. These options are effect-local so older authored abilities retain their established targeting behavior.

Proposed identifiers such as `target.taunting-enemy`, `target.most-injured-ally`, and `target.chain-targets` have no current selector. Adjacency is intentionally absent because combat has no spatial model.

Implementation: `LL/src/Core/Domain/Models/Combat/Abilities/AbilitySpec.cs` and `LL/src/Infrastructure/Service/Services.LL/Combat/Engine/FastCombatEngine.cs`.

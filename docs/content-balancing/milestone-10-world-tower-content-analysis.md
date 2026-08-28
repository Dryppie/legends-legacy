# Milestone 10 World Tower Content Analysis

Milestone 10 is the first complete content-calibration pass: the one-button balance command now carries measured character progression through the authored World Tower Floors 1–10 and reports how those encounters perform.

## Production Boundary

The analyzer reads the production catalogs for:

- `world-tower/tower-floors.json`;
- `world/creatures.json`;
- `combat/creature-abilities.json`;
- `world/creature-essence-loot-tables.json`;
- `progression/region-combat-balance.json`;
- the existing ability, Essence, and crafting definitions.

Players are detached canonical builds from the representative library. A balance-only snapshot builder replaces database-backed item lookup, which is inappropriate for an offline deterministic tool, and then hands those builds to the production `CombatPreparationPipeline` and `WorldTowerCombatRuntimeFactory`. The run retains `CombatSetupService`, `CreatureScaler`, `WorldTowerGuardianScaling`, authored Guardian ability profiles, equipment behavior, Essence resolution, `CombatEngineExecutor`, and `FastCombatEngine`. It neither reads player accounts nor mutates production content.

An integrity check requires each Guardian creature name to resolve to the ability-profile ID authored on its floor. Missing floors, creatures, anchors, profiles, or mismatched Guardian identities fail the run explicitly.

## Target and Representative Selection

Each floor takes its target benchmark power from `WorldTower.Region1`. The analyzer considers the `E4_P75`, `E5_P75`, and `E6_P75` profiles and chooses the profile whose measured mean benchmark power is closest to that target. Ties resolve toward fewer Essence slots and then stable profile ID order.

With the seed-`8471` reference measurements, this produces:

| Floors | Selected profile | Reference gear |
| --- | --- | --- |
| 1–4 | `E4_P75` | T1, Rare, Exceptional |
| 5–7 | `E5_P75` | T1, Rare, Exceptional |
| 8–10 | `E6_P75` | T1, Epic, Exceptional |

This preserves the explicit Floor 1 and Floor 10 anchors while allowing intermediate profile transitions to follow measured power rather than hard-coded floor ranges.

## Party and Combat Sampling

The default run performs ten simulations per floor. `--tower-simulations <number>` accepts 1–1,000 trials without creating a separate workflow.

For every trial, the tool:

1. derives stable party-selection and combat seeds from the run seed, floor, and trial number;
2. deterministically shuffles the selected profile's representative builds;
3. fills the floor's authored 5-, 10-, or 15-player party size, cycling only when the party is larger than the retained library;
4. runs the authored Guardian encounter for the production 6,000-tick limit with event logging disabled;
5. records outcome, duration, deaths, surviving-health ratio, player/team CR, and build IDs.

This tests varied representative parties rather than identical copies of one optimized character. Runs remain reproducible for the same production content, seed, and options.

## Clear Rate, CR, and Warnings

The desired clear rate is `65%` with a `±10` percentage-point target window:

| Observed clear rate | Classification |
| --- | --- |
| Below 55% | `TooHard` |
| 55% through 75% | `OnTarget` |
| Above 75% | `TooEasy` |

Recommended display CR uses the same curve weight as target benchmark power, interpolated between the measured median display CR of the Floor 1 and Floor 10 anchors. This keeps CR a derived player-facing label rather than the source of encounter difficulty. The report separately includes the median mean-player CR of successful trials when a clear exists.

Warnings identify:

- clear rate outside the target window;
- samples with no clears or no defeats, where a clearing threshold is not bounded;
- a material difference between derived and currently authored recommended CR;
- high Essence similarity in the selected representative library.

Milestone 10 remains the authored-content diagnosis stage. It does not search Guardian multipliers or write recommendations into `tower-floors.json`; the completed Milestone 12 stage consumes this immutable baseline and performs the bounded search separately.

## Report Contract

Balance schema version 10 introduced `worldTowerAnalysis` in `summary.json` and `world-tower-analysis.json` under both `latest` and immutable history; the current combined pipeline uses schema version 14. `summary.md` includes the complete Floor 1–10 table and all warnings. Trial-level JSON retains the exact seeds and party build IDs needed to reproduce an observation. Schema 12 also retains each floor's authored health and offense multipliers so the separate calibration report can show exact before-and-after recommendations.

## Initial Measured Result

The default seed-`8471` run selected `E4_P75`, `E5_P75`, and `E6_P75` at the floor ranges above. All ten authored floors produced a `0%` clear rate across ten varied party trials and were classified `TooHard`. Every party member was defeated; average fight duration varied by encounter from roughly 204 to 851 ticks.

This is useful calibration evidence, not an acceptance constant. It indicates that the existing Guardian content scaling is substantially above the intended Region 1 P75 progression band. Milestone 12 feeds this result into bounded health/offense calibration and reports recommendations without triggering an unreviewed production-content rewrite; the later [Region 1 Scaling Validation Gate](region-1-scaling-validation.md) decides whether those recommendations generalize well enough to approve.

The derived CR curve spans the measured anchor CRs from 187 on Floor 1 to 213 on Floor 10. Because the sample had no clears, observed clearing CR is unavailable on every floor, and the report states that limitation explicitly.

## Verification Boundary

Automated coverage verifies:

- identical seed and production content reproduce the complete World Tower analysis;
- Floors 1–10 are present exactly once and match the progression band;
- Floor 1 selects `E4_P75` and Floor 10 selects `E6_P75`;
- every trial contains the floor's authored party size;
- clear rates and survival ratios remain bounded;
- Guardian ability-profile identity is validated;
- the dedicated JSON and Markdown sections are written identically to `latest` and immutable history.

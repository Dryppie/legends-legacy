# Milestone 12 Encounter Calibration

Milestone 12 converts the World Tower clear-rate diagnosis into bounded, reproducible content recommendations. It never edits production encounter data.

It runs automatically after World Tower analysis in the same `ProductionBalanceRunner.Run` invocation. The repository-level `.\build\run-balance.ps1` entry point therefore generates the baseline, performs calibration, and writes both reports without a separate command or manual handoff.

## Controlled calibration boundary

The initial calibrator exposes only the two production Guardian knobs approved by the design:

- health: `guardianScaling.health`;
- damage: `guardianScaling.offense`.

One temporary difficulty factor is applied equally to both authored multipliers during search. For example, factor `0.80` evaluates `authored health × 0.80` and `authored offense × 0.80`. Defense, resistance, penetration, regeneration, stagger behavior, ability definitions, party selection, gear, Essences, and every other encounter mechanic remain unchanged.

The shared factor deliberately avoids an underdetermined two-dimensional search in the first implementation. The JSON report retains both the adjustment factors and the resulting health and damage multipliers, allowing a developer to review the exact proposed content values. Later milestones can introduce separately weighted mappings if evidence justifies them.

## Bounded deterministic search

Each floor starts from its Milestone 10 baseline and uses the same representative profile, party-selection seeds, and combat seeds for every candidate. This common-random-number approach makes differences attributable to the temporary scaling change rather than different random parties.

The default search is:

| Setting | Default |
| --- | ---: |
| Minimum factor | 0.25 |
| Maximum factor | 2.00 |
| Binary-search iterations | 10 |
| Target clear rate | inherited from World Tower analysis: 65% |
| Target tolerance | inherited from World Tower analysis: ±10 percentage points |
| Trials per candidate | inherited from `--tower-simulations`: 10 |

The authored `1.00` result is reused from Milestone 10. The search evaluates both bounds, then bisects the bracket. Clear rate is expected to be non-increasing as the shared multiplier rises. All evaluated points are retained so a recommendation remains auditable even when the small deterministic sample produces a sharp clear-rate cliff.

Search outcomes are:

| Status | Meaning |
| --- | --- |
| `AlreadyOnTarget` | Authored values are already inside the target window. |
| `Converged` | An evaluated candidate is inside the target window. |
| `BestEffort` | No evaluated candidate entered the window; the closest measured candidate is reported. |
| `LowerBoundExhausted` | The encounter remains too hard at the approved minimum. No content change is recommended. |
| `UpperBoundExhausted` | The encounter remains too easy at the approved maximum. No content change is recommended. |

`--calibration-iterations <number>` accepts 1–20 iterations. Bounds are deliberately code-owned safety limits for this milestone.

## Recommendation safety

The report contains `productionContentModified: false`. Converged and best-effort results can recommend exact health and offense values, but applying them always requires developer review. Exhausted-bound results explicitly request mechanic review or a deliberate bound change and set `requiresContentChange` to false.

No part of calibration writes to `tower-floors.json`, a database, or an external environment.

## Report contract

Balance schema version 12 introduced `encounterCalibration` in `summary.json` and `encounter-calibration.json` under both `latest` and immutable history; the current combined pipeline uses schema version 15. Each floor records:

- baseline and desired clear rates;
- authored health and damage multipliers;
- recommended shared difficulty factor;
- suggested health and damage multipliers;
- suggested clear rate and search status;
- every evaluated factor and its combat measurements;
- the trial count used for every evaluated factor;
- a developer-facing recommendation.

`summary.md` includes a compact Floor 1–10 calibration table followed by the recommendations.

## Seed 8471 default result

The final default run used 10 trials per candidate and 10 binary-search iterations:

| Floor | Factor | Health | Damage | Suggested clear | Status |
| ---: | ---: | --- | --- | ---: | --- |
| 1 | 0.3184 | 1.270 → 0.404 | 1.240 → 0.395 | 70% | Converged |
| 2 | 0.3782 | 1.450 → 0.548 | 1.380 → 0.522 | 60% | Converged |
| 3 | 0.2842 | 0.860 → 0.244 | 3.790 → 1.077 | 70% | Converged |
| 4 | 0.2876 | 1.110 → 0.319 | 2.400 → 0.690 | 60% | Converged |
| 5 | 0.3730 | 1.250 → 0.466 | 1.580 → 0.589 | 80% | BestEffort |
| 6 | 0.3389 | 1.790 → 0.607 | 1.270 → 0.430 | 70% | Converged |
| 7 | 0.3320 | 1.150 → 0.382 | 1.800 → 0.598 | 70% | Converged |
| 8 | 0.4431 | 1.550 → 0.687 | 1.450 → 0.642 | 70% | Converged |
| 9 | 0.3303 | 1.200 → 0.396 | 1.900 → 0.628 | 70% | Converged |
| 10 | 0.3320 | 1.640 → 0.544 | 0.920 → 0.305 | 70% | Converged |

Floor 5 has a measured clear-rate cliff between nearby factors: the best evaluated result was 80%, just above the 55–75% target window. It is correctly labeled `BestEffort` rather than falsely reported as converged. These large recommended reductions also reinforce the Milestone 10 diagnosis that the current encounters are substantially overtuned for the specified Region 1 reference builds.

## Downstream validation status

These ten-trial calibration results are search observations, not final scaling approval. The automated [Region 1 Scaling Validation Gate](region-1-scaling-validation.md) subsequently evaluates every recommendation on eight distinct holdout seeds with confidence, stability, sensitivity, and percentile-order checks. In the canonical seed-`8471` run, only Floor 8 passed the complete policy; Floors 2–5 were unstable, while Floors 1, 6, 7, 9, and 10 required mechanic/build-profile review. The calibration table must therefore not be copied into production or used as the Milestone 14 template without remediation or an explicitly documented exception.

# Milestone 13 Encounter-Specific Optimization

Milestone 13 tests whether builds tailored to one Guardian materially outperform the generic P75 builds used for progression. It runs automatically after encounter calibration in the same `ProductionBalanceRunner.Run` invocation and in the repository-level `./build/run-balance.ps1` one-button flow.

The stage is diagnostic. It does not replace generic representative builds, power anchors, progression targets, recommended Combat Rating, or production encounter data.

## Candidate source and encounter evaluation

For each World Tower floor, the optimizer:

1. selects the complete unique evaluated population from the generic Essence optimizer with the same unlocked slot count as the floor's P75 profile;
2. evaluates each candidate specifically against that floor's Guardian using the Milestone 12 calibrated health and damage factors;
3. runs the production World Tower combat preparation, authored Guardian abilities, scaling rules, and combat engine;
4. ranks candidates by clear rate, surviving health, friendly deaths, and duration;
5. retains a diversity-aware specialized team; and
6. re-simulates that mixed team using the normal World Tower trial count.

Candidate scoring is:

```text
encounter score = clear rate × 100
                + remaining-health ratio × 10
                - friendly deaths × 2
                - duration / maximum ticks × 5
```

The default search uses three trials per candidate, retains five builds, and applies an eight-point maximum-overlap penalty during selection. The final specialized team uses `--tower-simulations`, which defaults to ten trials. Common seeds make comparisons deterministic for the same content and run seed.

Two command-line options expose the main cost controls:

```text
--encounter-candidate-simulations <number>  Trials per candidate (default: 3; range: 1–100).
--encounter-retained <number>               Specialized builds retained per floor (default: 5; range: 1–50).
```

## Generic comparison and findings

The comparison baseline is the calibrated generic P75 clear rate from Milestone 12. Generic PvE strength is the mean score of that P75 representative profile. The specialized side reports its clear rate, mean generic PvE score, delta from the generic profile, mean pairwise Essence overlap, retained builds, and dominant Essences.

The version-1 thresholds are intentionally conservative and explicit:

| Finding | Required evidence |
| --- | --- |
| `HardCounter` | Specialized clear rate at least 80% and at least 25 percentage points above generic. |
| `CheeseRisk` | Specialized clear rate at least 90%, at least 25 points above generic, generic PvE delta at most -5.00, and one Essence present in at least 80% of retained builds. |
| `None` | Neither complete rule is satisfied. |

`CheeseRisk` takes precedence over `HardCounter`. A dominant Essence alone is reported as evidence but does not create a finding. The generic-PvE penalty ensures that an ordinarily strong build is not mislabeled as a narrow exploit merely because it also excels against a Guardian.

## Report contract

Balance schema version 13 introduced `encounterSpecificOptimization` in `summary.json` and `encounter-specific-optimization.json` under both `latest` and immutable history; the current combined pipeline uses schema version 46. Each floor records:

- the candidate population and slot count;
- the calibration factors actually tested;
- generic and specialized clear rates and their delta;
- generic and specialized PvE scores and their delta;
- specialized-team similarity;
- retained builds with encounter telemetry and Essence IDs;
- dominant-Essence evidence, finding classification, and warnings.

`summary.md` includes the Floor 1–10 comparison, each floor's leading specialized build, and investigation warnings. The CLI prints the generic-to-specialized clear-rate comparison during the run.

## Seed 8471 default result

The canonical default run evaluated 80 candidates on each of 10 floors: 800 candidate evaluations, with three seeded trials per candidate and ten trials for each retained specialized team.

| Floor | Slots | Generic clear | Specialized clear | Advantage | Generic PvE delta | Similarity | Finding |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| 1 | 4 | 70% | 100% | +30% | -0.69 | 12.50% | HardCounter |
| 2 | 4 | 60% | 100% | +40% | +1.59 | 27.50% | HardCounter |
| 3 | 4 | 70% | 100% | +30% | -0.96 | 10.00% | HardCounter |
| 4 | 4 | 60% | 100% | +40% | +1.17 | 20.00% | HardCounter |
| 5 | 5 | 80% | 100% | +20% | -4.17 | 20.00% | None |
| 6 | 5 | 70% | 100% | +30% | -4.56 | 10.00% | HardCounter |
| 7 | 5 | 70% | 100% | +30% | -2.18 | 12.00% | HardCounter |
| 8 | 6 | 70% | 100% | +30% | -3.61 | 13.33% | HardCounter |
| 9 | 6 | 70% | 100% | +30% | -6.13 | 5.00% | HardCounter |
| 10 | 6 | 70% | 100% | +30% | -0.22 | 21.67% | HardCounter |

No floor met the complete cheese rule. Floor 9 had the required generic-PvE penalty but no Essence reached 80% usage across its diverse retained team. Enchanted Fairy reached 80% usage on Floors 2, 5, and 10, but those floors did not meet the narrow-build penalty; it remains visible evidence for investigation rather than an automatic cheese verdict. Floor 5's 20-point advantage stayed below the hard-counter threshold.

The nine hard-counter signals show that encounter-aware selection can materially outperform generic P75 parties even after calibration. They are investigation prompts, not automatic nerfs or changes to the Region 1 CR curve.

The generic clear rates in this table are the ten-trial calibration observations used for the same run's encounter-specific comparison. They are not holdout approval results. The downstream [Region 1 Scaling Validation Gate](region-1-scaling-validation.md) uses independent seeds and currently validates only Floor 8; its verdict takes precedence when deciding whether Region 1 scaling is safe to reuse.

## Verification

Automated coverage verifies deterministic end-to-end execution, encounter-specific artifact persistence in the current schema-15 report, CLI option validation, and a focused synthetic case where a dominant low-generic-PvE strategy is classified as `CheeseRisk`. The canonical seed-8471 invocation verifies all 10 production Guardians through the same one-button flow.

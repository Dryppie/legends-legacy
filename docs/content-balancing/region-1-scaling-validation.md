# Region 1 Scaling Validation Gate

This pre-Milestone-14 gate determines whether Region 1 calibration recommendations generalize beyond the seed used to find them. It runs automatically after Milestone 13 in the same `ProductionBalanceRunner.Run` invocation and through the repository-level `./build/run-balance.ps1` command.

The validator is intentionally allowed to reject scaling. It does not alter calibration recommendations, production World Tower definitions, representative builds, progression targets, or recommended Combat Rating.

## Default evidence

The complete Region 1 gate executes 18,000 production-engine combat trials, distributed as follows per floor:

| Evidence | Per floor |
| --- | ---: |
| Independent deterministic holdout seeds | 8 |
| Calibrated P75 trials per seed | 50 |
| Calibrated holdout trials | 400 |
| Trials per seed for each sensitivity probe | 25 |
| Common-sample control and sensitivity probes | calibrated P75, easier, harder, health-only, damage-only, P50, P90 |
| Total trials per floor | 1,800 |

Holdout seeds are derived deterministically from the run seed under a separate validation namespace. They are not the seed used by the calibration search. Every probe still uses the production World Tower preparation, authored Guardian abilities and mechanics, Guardian scaling, and combat engine.

The default command-line controls are:

```text
--validation-seeds <number>              Holdout seeds per floor (default: 8; range: 2–50).
--validation-simulations <number>        Calibrated P75 trials per seed (default: 50; range: 1–1,000).
--validation-probe-simulations <number>  Trials per sensitivity probe and seed (default: 25; range: 1–1,000).
```

## Acceptance rules

The calibrated P75 clear rate receives a 95% Wilson confidence interval. With the current World Tower target of 65% ±10 percentage points, that entire interval must fit inside 55%–75%.

A floor is `Validated` only when all of these are true:

- the 95% holdout interval is contained by the target window;
- clear-rate standard deviation across seeds is at most 10 percentage points;
- the maximum seed-to-seed range is at most 25 points;
- a 10% easier shared health/offense factor does not clear less often than calibrated scaling beyond a three-point tolerance;
- calibrated scaling does not clear less often than a 10% harder shared factor beyond the same tolerance;
- generic P50, P75, and P90 clear rates retain their expected order within a three-point tolerance;
- calibration did not finish as `BestEffort` or at an exhausted bound.

`Unstable` means the mechanics and percentile ordering behaved coherently, but the recommendation failed confidence, cross-seed stability, or calibration-quality requirements. `MechanicReviewRequired` means scaling was non-monotonic, generic percentile ordering broke, or calibration exhausted its approved bounds.

Health-only and damage-only +10% probes are reported as sensitivity evidence. They do not independently determine the verdict; they show whether local difficulty is primarily responding to survivability pressure, encounter duration, or both.

## Report contract

Balance schema version 14 adds `scalingValidation` to `summary.json` and writes `scaling-validation.json` beneath both `latest` and immutable history. The report records:

- all sample-size and acceptance settings;
- total combat-trial count and verdict totals;
- calibrated holdout clear rate and telemetry;
- 95% confidence interval;
- cross-seed standard deviation and range;
- easier, harder, health-only, and damage-only results;
- P50/P75/P90 clear rates and ordering status;
- per-floor verdicts and actionable warnings;
- `productionContentModified: false`.

`summary.md` contains both the acceptance table and sensitivity probes. The CLI prints each floor's holdout clear rate, confidence interval, and verdict.

## Seed 8471 default result

The canonical default run executed 18,000 validation battles:

| Floor | Holdout clear | 95% interval | Seed σ | Seed range | P50 / P75 / P90 | Verdict |
| ---: | ---: | --- | ---: | ---: | --- | --- |
| 1 | 87.5% | 83.9%–90.4% | 4% | 14% | 27.0% / 88.5% / 81.5% | MechanicReviewRequired |
| 2 | 55.3% | 50.4%–60.0% | 6% | 20% | 4.5% / 54.0% / 65.0% | Unstable |
| 3 | 87.3% | 83.6%–90.2% | 3% | 10% | 11.5% / 87.5% / 99.5% | Unstable |
| 4 | 57.8% | 52.9%–62.5% | 7% | 24% | 5.0% / 56.0% / 56.0% | Unstable |
| 5 | 94.0% | 91.2%–95.9% | 2% | 8% | 91.5% / 93.5% / 100.0% | Unstable |
| 6 | 63.8% | 59.0%–68.3% | 8% | 24% | 63.5% / 59.5% / 94.0% | MechanicReviewRequired |
| 7 | 53.8% | 48.9%–58.6% | 5% | 12% | 48.5% / 44.5% / 80.5% | MechanicReviewRequired |
| 8 | 69.0% | 64.3%–73.3% | 5% | 14% | 1.0% / 70.0% / 95.5% | Validated |
| 9 | 81.3% | 77.1%–84.8% | 6% | 22% | 99.5% / 81.5% / 50.5% | MechanicReviewRequired |
| 10 | 72.3% | 67.7%–76.4% | 4% | 12% | 0.0% / 73.5% / 38.5% | MechanicReviewRequired |

`Holdout clear` and its confidence interval use 400 trials per floor. The P50/P75/P90 comparison uses an equal-sized common sample of 200 trials per profile, which is why its P75 value can differ slightly from the 400-trial holdout estimate.

All ten floors passed the local shared-factor monotonicity check. Floor 8 is the only recommendation that generalizes under every acceptance rule. Floor 5 remains a best-effort calibration and clears far above target. Floors 1, 6, 7, 9, and 10 violate generic percentile ordering; this is consistent with encounter-specific strengths not represented by aggregate PvE percentile alone and requires build/mechanic review rather than blind numeric scaling.

## Milestone 14 gate

Region 1 is not yet approved as the calibration template for additional progression bands. Before recalibration, the designed [Elite Build Certification Gate](elite-build-certification-gate.md) should establish trustworthy P95/P99 and top-player stress cohorts. Unstable Floors 2–5 should then be recalibrated against the certified population and holdout distribution, while mechanic-review Floors 1, 6, 7, 9, and 10 should be inspected for encounter-specific interactions or unsuitable generic profile mapping. This gate should then be rerun unchanged. Expansion is safe only after both gates pass or a developer explicitly documents an exception.

## Verification

Automated coverage verifies Wilson confidence bounds, stable-seed acceptance, monotonic probes, percentile ordering, option validation, immutable report persistence, deterministic replay, and one-request orchestration. The canonical run verifies the default 18,000-trial gate against all production Region 1 Guardians.

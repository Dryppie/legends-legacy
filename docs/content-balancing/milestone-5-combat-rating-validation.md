# Milestone 5 Combat Rating Validation

Milestone 5 measures whether Combat Rating predicts the aggregate PvE benchmark performance produced by Milestone 4. It reports several complementary statistics because no single metric can describe CR health reliably.

## Input Population

Every generated build contributes one observation containing:

- build and Essence profile IDs;
- displayed and raw Combat Rating;
- aggregate benchmark score;
- retained scenario component scores.

The initial population contains E4, E5, and E6 random builds. Multiple builds intentionally share the same CR within a profile, exposing performance variance that the current CR algorithm does not represent.

## Predictive Statistics

The first implementation calculates:

| Metric | Definition | Interpretation |
| --- | --- | --- |
| Spearman correlation | Pearson correlation of average ranks, preserving ties | Whether higher CR generally corresponds to higher benchmark performance |
| R² | Ordinary least-squares performance regression using raw CR | How much aggregate-score variance a linear CR model explains |
| Mean absolute error | Mean absolute observed-minus-predicted aggregate score | Typical prediction error in benchmark-score points |
| Root mean square error | Square root of mean squared residuals | Prediction error with stronger emphasis on large misses |

The fitted model records its intercept and slope so every prediction is reproducible. A zero CR variance produces a constant mean-performance model, Spearman correlation `0`, and R² `0` rather than an undefined report.

## CR Bands

Displayed CR is grouped into fixed ten-point occupied bands such as `180–189`. Empty bands are omitted.

Each band records:

- build count;
- median performance;
- interpolated P10 and P90 performance;
- P90-minus-P10 spread;
- population variance and standard deviation;
- minimum and maximum performance.

Percentiles use linear interpolation over the ordered observations. Fixed bands keep historical reports comparable; they are not fitted to the current data.

## Prediction Errors and Outliers

Every build records predicted performance, signed residual, absolute residual, and percentage error relative to the prediction.

A build is initially classified as a CR outlier when both conditions hold:

```text
absolute residual >= 5 benchmark-score points
absolute residual >= 2 × population residual standard deviation
```

Positive residuals are high-performing CR outliers; negative residuals are low-performing CR outliers. The dual threshold prevents tiny errors in an unusually tight population from being over-reported.

Each outlier retains its Essence IDs and five benchmark component scores. Automatic root-cause diagnosis remains deferred.

## Health Classification

The overall report uses the strictest fully satisfied tier:

| Classification | Spearman | R² | MAE | Mean within-band P10–P90 spread |
| --- | ---: | ---: | ---: | ---: |
| Excellent | ≥ 0.90 | ≥ 0.80 | ≤ 5 | ≤ 10 |
| Good | ≥ 0.75 | ≥ 0.60 | ≤ 8 | ≤ 15 |
| Concerning | ≥ 0.50 | ≥ 0.35 | ≤ 12 | ≤ 25 |
| Poor | Anything below the concerning boundary | | | |

This classification is a diagnostic signal, not an automatic approval gate. A weak result is expected while CR excludes Essence ability performance.

## Report Contract

Every balance run will include CR-health results in `summary.json`, summarize the classification and principal metrics in `summary.md`, and write the complete analysis to `combat-rating.json` under both `latest` and the immutable history directory.

The report will include:

- overall health and model statistics;
- occupied CR-band distributions;
- every build prediction and residual;
- high- and low-performing CR outliers;
- explicit explanatory warnings when sample size or CR diversity limits interpretation.

## Verification Boundary

Automated coverage must verify:

- deterministic statistics for identical benchmark input;
- tied-rank correlation behavior;
- regression, prediction, percentile, and band calculations;
- bounded and internally consistent error metrics;
- outlier direction and threshold enforcement;
- JSON and Markdown immutable-history output.

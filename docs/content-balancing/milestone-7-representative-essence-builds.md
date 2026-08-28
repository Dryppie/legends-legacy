# Milestone 7 Representative Essence Builds

Milestone 7 converts the transient Milestone 6 search distribution into stable E4/E5/E6 P50, P75, and P90 Essence profiles. These profiles are the small, versioned build library consumed by Milestone 8 power anchors and later encounter calibration.

## Population Boundary

Percentiles are calculated from every unique candidate evaluated for a slot count during the current optimizer run:

- the Milestone 3 random seed population;
- any candidates added to fill optimizer generation zero;
- every mutated candidate;
- every randomly injected candidate.

Preserved elites are counted once, and rejected duplicate genomes are never evaluated or counted. Percentiles are not calculated from only the final generation or the optimizer's diversity-selected retained list, because either choice would bias P50 toward an already-elite population.

The full evaluated population remains transient. Only the representative library and the compact Milestone 6 optimizer summary are persisted.

## Percentile Meaning

Each slot count produces these profiles:

| Profile | Meaning within the generated search population |
| --- | --- |
| `E4_P50`, `E5_P50`, `E6_P50` | Median generated candidate performance |
| `E4_P75`, `E5_P75`, `E6_P75` | Competent upper-quartile generated performance |
| `E4_P90`, `E5_P90`, `E6_P90` | Strongly optimized generated performance |

These labels describe the generated balance population, not live-player behavior. Telemetry may later establish how those distributions correspond to actual players.

Target scores use linear interpolation over aggregate Milestone 4 benchmark scores sorted in ascending order. Candidate percentile ranks use their stable score-and-ID order in the same population.

## Representative Selection

For every slot-count and target-percentile pair:

1. Create a percentile window from the closest `2 × representative count` candidates by target-score distance.
2. Select the candidate closest to the interpolated target score.
3. Continue greedily within that window until the configured representative count is reached.
4. For subsequent candidates, minimize:

```text
absolute distance from target score
+ maximum Essence-set similarity to an already-selected representative
  × optimizer diversity penalty
```

The bounded window prevents diversity pressure from pulling a representative far away from the named percentile. Ties are resolved by aggregate score and then stable source-build ID. A representative profile never contains the same genome twice. The same evaluated source candidate may appear in two different percentile profiles when it is genuinely the best deterministic match for both targets in a small or tightly clustered population.

Every representative retains its legal Essence selection, reference Gear Package, character level, Combat Rating, aggregate score, benchmark component scores, optimizer discovery generation, source-build ID, and observed population percentile.

## Configuration

`--representative-count <number>` controls the number of builds retained in each of the nine profiles. The default is 10 and the accepted range is 1–500, additionally bounded by the number of unique evaluated candidates available per slot count.

This is intentionally separate from optimizer population size and Milestone 6's diagnostic retained-candidate count.

## Report Contract

Balance schema version 7 introduced `representativeBuilds` in `summary.json` and `representative-builds.json` under both `latest` and immutable history; the current combined pipeline uses schema version 15. `summary.md` reports every profile's target score, selected score range, mean score, mean pairwise similarity, and representative count, followed by a compact list of its closest representative.

The output contains exactly nine profiles in stable slot-count and percentile order. Milestone 8 uses `E4_P75` with the Region 1 Floor 1 Gear Package and `E6_P75` with the Floor 10 Gear Package to measure the first power anchors.

## Initial Measured Result

With seed `8471` and the default Milestone 6 and 7 settings, each slot count evaluated 80 unique candidates and retained ten builds per percentile profile:

| Profile | Target score | Selected range | Selected mean | Mean similarity |
| --- | ---: | ---: | ---: | ---: |
| `E4_P50` | 63.05 | 61.40–64.80 | 62.85 | 0.08 |
| `E4_P75` | 67.01 | 65.37–68.19 | 66.83 | 0.08 |
| `E4_P90` | 69.80 | 67.14–71.53 | 69.63 | 0.18 |
| `E5_P50` | 67.31 | 65.65–67.90 | 67.01 | 0.08 |
| `E5_P75` | 71.58 | 69.65–73.15 | 71.68 | 0.09 |
| `E5_P90` | 74.63 | 71.94–74.94 | 73.70 | 0.11 |
| `E6_P50` | 71.16 | 69.86–71.72 | 70.82 | 0.08 |
| `E6_P75` | 73.72 | 72.33–74.80 | 73.35 | 0.11 |
| `E6_P90` | 76.21 | 73.38–77.15 | 75.77 | 0.21 |

The closest build in every profile ranked within approximately 0.63 percentile points of its requested P50/P75/P90 target. Each retained profile contained ten unique genomes.

## Verification Boundary

Automated coverage verifies:

- deterministic libraries for identical input and settings;
- exactly P50/P75/P90 for each E4/E5/E6 slot count;
- interpolated target scores and stable candidate percentile ranks;
- configured representative counts and unique genomes within each profile;
- retained legality, character metadata, aggregate score, and benchmark components;
- JSON and Markdown immutable-history output;
- explicit rejection when the requested count exceeds the evaluated population.

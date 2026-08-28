# Milestone 8 Power Anchors

Milestone 8 measures the two endpoints of the first World Tower progression band from production Gear Packages and Milestone 7 representative Essence profiles.

## Region 1 Definitions

| Anchor | Progression point | Gear Package | Essence profile |
| --- | --- | --- | --- |
| `WorldTower.Region1.Start` | Floor 1 | `T1_Rare_Exceptional_Balanced` | `E4_P75` |
| `WorldTower.Region1.End` | Floor 10 | `T1_Epic_Exceptional_Balanced` | `E6_P75` |

The analyzer resolves these IDs from the current run and fails explicitly when an anchor is missing, duplicated, or paired with representative builds using a different Gear Package. It does not construct bespoke anchor characters or rerun combat: Milestone 7 builds already retain their production-engine benchmark results and canonical character metadata.

## Measurement Contract

Anchor benchmark power is the arithmetic mean of the representative builds' aggregate Milestone 4 PvE scores. Each anchor also retains:

- representative build count;
- minimum and maximum benchmark score;
- population variance and standard deviation;
- mean score for every benchmark component;
- minimum, median, mean, and maximum displayed and raw Combat Rating.

The variance is the population variance of the retained anchor sample. CR remains a diagnostic alongside simulated benchmark power rather than becoming the authority for the anchor.

## Report Contract

Balance schema version 8 introduced `powerAnchors` in `summary.json` and `power-anchors.json` under both `latest` and immutable history; the current combined pipeline uses schema version 14. `summary.md` reports the anchor composition, measured benchmark power and spread, and CR range.

Milestone 9 consumes the measured mean benchmark power of these anchors as its exact Floor 1 and Floor 10 endpoints.

## Initial Measured Result

With seed `8471` and the default ten-build P75 representative profiles:

| Anchor | Mean power | Power range | Variance | Standard deviation | Display CR | Raw CR |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Region 1 Start (`E4_P75`) | 66.83 | 65.37–68.19 | 0.4567 | 0.6758 | 187 | 1,874 |
| Region 1 End (`E6_P75`) | 73.35 | 72.33–74.80 | 0.4797 | 0.6926 | 213 | 2,134 |

The end anchor measures 6.52 benchmark points above the start anchor. The narrow within-anchor standard deviations indicate that the diversity-selected P75 libraries provide stable endpoints for the initial progression curve.

## Verification Boundary

Automated coverage verifies:

- exact Region 1 Gear Package and P75 profile mappings;
- deterministic mean, range, population variance, and standard deviation;
- benchmark component aggregation;
- displayed and raw CR distributions;
- rejection of mismatched representative Gear Packages;
- JSON and Markdown immutable-history output.

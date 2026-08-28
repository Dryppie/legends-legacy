# Milestone 9 Progression Bands

Milestone 9 converts the measured Region 1 start and end power anchors into a deterministic target benchmark-power curve for World Tower Floors 1–10.

## Region 1 Band

`WorldTower.Region1` is defined by:

- Floor 1: `WorldTower.Region1.Start`;
- Floor 10: `WorldTower.Region1.End`;
- no manually maintained intermediate character builds;
- one configurable interpolation curve.

Floor 1 always equals measured start-anchor power and Floor 10 always equals measured end-anchor power.

## Curves

For normalized floor position `t = (floor - 1) / 9`, the supported curve weights are:

| CLI value | Weight |
| --- | --- |
| `linear` | `t` |
| `ease-in` | `t²` |
| `ease-out` | `1 - (1 - t)²` |
| `smooth-step` | `3t² - 2t³` |

`smooth-step` is the default. It eases progression near both explicit anchors while preserving a smooth monotonic middle. `--progression-curve <value>` changes the curve for a run without changing production content.

Target power is:

```text
start anchor power
+ (end anchor power - start anchor power) × curve weight
```

Positions, weights, and target powers are rounded to four, six, and two decimal places respectively using midpoint-away-from-zero rounding. Endpoint power is assigned directly from its anchor to prevent rounding drift.

## Report Contract

Balance schema version 9 introduced `progressionBands` in `summary.json` and `progression-bands.json` under both `latest` and immutable history; the current combined pipeline uses schema version 15. Each floor retains its normalized position, curve weight, target benchmark power, and optional endpoint anchor ID. The Markdown report renders the complete ten-floor curve.

Milestone 9 describes progression intent only. Milestone 10 consumes these targets to select P75 representatives, simulate the authored World Tower encounters, derive CR, and report difficulty warnings. Milestone 12 now consumes that diagnosis and performs bounded health/offense calibration without modifying production content.

## Initial Measured Result

Seed `8471` produced this default `smooth-step` curve from the measured Milestone 8 anchors:

| Floor | Position | Curve weight | Target benchmark power |
| ---: | ---: | ---: | ---: |
| 1 | 0.0000 | 0.000000 | 66.83 |
| 2 | 0.1111 | 0.034294 | 67.05 |
| 3 | 0.2222 | 0.126200 | 67.65 |
| 4 | 0.3333 | 0.259259 | 68.52 |
| 5 | 0.4444 | 0.417010 | 69.55 |
| 6 | 0.5556 | 0.582990 | 70.63 |
| 7 | 0.6667 | 0.740741 | 71.66 |
| 8 | 0.7778 | 0.873800 | 72.53 |
| 9 | 0.8889 | 0.965706 | 73.13 |
| 10 | 1.0000 | 1.000000 | 73.35 |

The measured endpoints are preserved exactly and all intermediate targets are monotonic.

## Verification Boundary

Automated coverage verifies:

- all ten Region 1 floors appear exactly once and in order;
- exact measured endpoint preservation;
- the documented formulas for every supported curve;
- monotonic targets when end-anchor power exceeds start-anchor power;
- deterministic output and explicit anchor-reference validation;
- JSON and Markdown immutable-history output.

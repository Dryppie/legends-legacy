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

Balance schema version 9 introduced `progressionBands` in `summary.json` and `progression-bands.json` under both `latest` and immutable history; the current combined pipeline uses schema version 46. Each floor retains its normalized position, curve weight, target benchmark power, and optional endpoint anchor ID. The Markdown report renders the complete ten-floor curve.

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

## Reliability review boundary

Schema-43 diagnostic evidence confirms that the intermediate E5 population can materially change encounter conclusions, but it does not authorize replacing the current smooth-step target interpolation with fixed progression cohorts. A preregistered candidate mapping—Floors 1–4 E4, Floors 5–7 E5, and Floors 8–10 E6—was reviewed across protocol-compatible seeds 12041, 14281, and 16633. E5 materially changed at least two available floor conclusions in every population, but only seed 14281 retained a complete Floor-3–8 matrix and monotonic E4/E5/E6 P75 mean power. Seed 12041 produced non-monotonic mean power and no neutral Floor-4 reference; seed 16633 produced no neutral Floor-3 reference.

The three-stage mapping is therefore an author-review candidate, not current functionality. This milestone continues to own only monotonic target interpolation between measured endpoints; no intermediate anchor, gear package, floor mapping, or production content was changed by the review.

Schema 44 retains the complete progression neutral-reference search for each floor. The missing seed-12041 Floor-4 matrix reaches `35.56%` clear only at the minimum `0.25` factor; seed 16633 Floor 3 reaches `4.44%`. Both are lower-bound exhaustion rather than lost refinement evidence. The schema also makes the population-ordering limitation explicit: P75 slot profiles are built from separate deterministic Essence samples, so a small 17-candidate population can invert mean E5/E6 benchmark power even though the character level, slot count, and reference gear packages progress. These diagnostics do not change the smooth-step band contract.

Schema 45 confirms that limitation with a preregistered matched-genome probe. Across seeds 12041, 14281, and 16633, all ten source genomes per seed have strict E4<E5<E6 power when every 4-of-6 and 5-of-6 subset is averaged under the normal progression packages; population means and median step deltas also pass unanimously. The prior P75 inversion is therefore a generated-population-composition artifact. The smooth-step band remains unchanged because Floor 4 and Floor 3 still lack neutral references under the frozen search, so the fixed three-stage candidate remains unapproved.

Schema 46 prevents a later run with different upstream optimizer or cohort settings from being treated as a replication of that review. Reliability artifacts now carry the complete population-shaping protocol, and population-policy v3 requires an exact match. Historical artifacts without provenance remain valid evidence in their documented context but cannot be promoted into a new compatible panel. Any future three-stage review must preregister one schema-46 protocol and rerun every population under it; current smooth-step functionality remains unchanged.

## Verification Boundary

Automated coverage verifies:

- all ten Region 1 floors appear exactly once and in order;
- exact measured endpoint preservation;
- the documented formulas for every supported curve;
- monotonic targets when end-anchor power exceeds start-anchor power;
- deterministic output and explicit anchor-reference validation;
- JSON and Markdown immutable-history output.

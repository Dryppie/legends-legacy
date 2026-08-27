# Milestone 4 PvE Benchmark Suite

Milestone 4 evaluates every generated Essence build against a small deterministic PvE benchmark suite using the production combat engine. It measures complete-build behavior rather than inferring PvE strength from individual-Essence PvP rankings.

## Initial Scenarios

The first suite contains five synthetic scenarios:

| Scenario | Maximum duration | Targets | Health per target | Incoming damage budget | Primary purpose |
| --- | ---: | ---: | ---: | ---: | --- |
| `pve.short-single-target` | 300 ticks / 30 seconds | 1 | 0.45× character health | 0.25× character health | Burst, opening pressure, and short cooldown value |
| `pve.sustained-single-target` | 1,200 ticks / 120 seconds | 1 | 1.5× character health | 0.75× character health | Sustained damage, ramp mechanics, and long-duration efficiency |
| `pve.high-incoming-damage` | 600 ticks / 60 seconds | 1 | 4× character health | 2.4× character health | Mitigation, healing, shielding, and survival under pressure |
| `pve.three-targets` | 600 ticks / 60 seconds | 3 | 0.45× character health | 0.7× character health | AoE, cleave, target switching, and multi-target effects |
| `pve.attrition` | 1,800 ticks / 180 seconds | 1 | 2.5× character health | 1.8× character health | Sustain, resource efficiency, long cooldowns, and defensive scaling |

Synthetic enemies are deterministic and scaled from the reference character state for the profile. The incoming damage budget is the total pre-mitigation basic-attack pressure over the full scenario and is divided across targets where applicable. Every build in one profile therefore faces the same scenario parameters. Scenario and build IDs derive independent combat seeds from the balance-run seed so identical content and configuration are reproducible.

Crowd-control-resistant, high-armor, and high-magic-resistance scenarios remain deferred until the first suite demonstrates that they add actionable information.

## Metrics and Scoring

Each scenario retains the underlying combat measurements:

- outcome and elapsed ticks;
- damage dealt and damage taken;
- enemies defeated;
- whether the character survived;
- remaining-health ratio;
- healing and shielding produced when exposed by the production result.

Each benchmark also produces a 0–100 component score. Objective progress is damage dealt divided by total initial enemy health. Clear speed is awarded only after all targets are defeated. Survival duration reaches 100% when the character survives the complete scenario and otherwise reflects the fraction of the duration reached. Mitigation and sustain ratios are measured against incoming raw damage.

The initial weights are:

| Scenario | Component weights |
| --- | --- |
| Short single target | 80% objective progress, 20% clear speed |
| Sustained single target | 75% objective progress, 15% clear speed, 10% survival duration |
| High incoming damage | 40% survival duration, 25% remaining health, 20% mitigation, 15% objective progress |
| Three targets | 65% objective progress, 25% targets defeated, 10% survival duration |
| Attrition | 35% survival duration, 20% remaining health, 20% sustain, 25% objective progress |

Scores and intermediate ratios are clamped rather than allowed to grow without bound.

The aggregate Benchmark Performance Score is the equal-weight arithmetic mean of the five component scores in the initial implementation. Raw metrics and every component score remain in the report so the aggregate cannot hide a severe weakness. Configuration-driven weights can be introduced when benchmark evidence justifies them.

## Ranking Boundary

Milestone 4 may order the sampled builds by measured aggregate performance, but it does not alter candidate generation. This preserves the random baseline required for percentile analysis.

The existing Admin Essence Simulator remains a complementary signal:

- it measures Essence and team performance in its 1v1/3v3 comparison model;
- the PvE suite measures complete builds against synthetic encounter demands;
- Milestone 6 may use both signals to seed and evolve candidates while retaining random injection and diversity pressure.

## Report Contract

Every balance run includes benchmark results in `summary.json`, summarizes profile ranges and leading sampled builds in `summary.md`, and writes full results to `benchmarks.json` under both `latest` and the immutable history directory. The balance schema version is 4.

Each build result records its profile, aggregate score, scenario component scores, deterministic seeds, and raw combat measurements.

With production content current when this milestone was implemented, seed `8471`, and the default ten random builds per profile, the measured aggregate ranges were:

| Profile | Aggregate score range | Leading sampled build | Score |
| --- | ---: | --- | ---: |
| `E4_RANDOM` | 54.10–67.17 | `E4_RANDOM_008` | 67.17 |
| `E5_RANDOM` | 59.10–72.30 | `E5_RANDOM_003` | 72.30 |
| `E6_RANDOM` | 52.72–75.68 | `E6_RANDOM_007` | 75.68 |

These values are measured outputs rather than acceptance constants. They demonstrate meaningful performance spread inside CR-identical profiles and may change with production content, engine rules, or documented benchmark tuning.

Milestone 5 consumes each build's displayed/raw CR and aggregate benchmark score to measure how accurately CR predicts this observed spread.

## Verification Boundary

Automated coverage must verify:

- all five scenarios execute for every generated build;
- repeated runs with identical inputs produce identical benchmark results;
- scenario metrics and component scores remain within their defined bounds;
- aggregate scores are calculated from the retained component scores;
- profile ranking uses benchmark performance rather than Combat Rating;
- JSON and Markdown output includes immutable historical benchmark data.

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

Synthetic enemies are deterministic and scaled from the reference character state for the profile. The incoming damage budget is the total pre-mitigation basic-attack pressure over the full scenario and is divided across targets where applicable. Every build in one profile therefore faces the same scenario parameters.

The legacy objective derives each combat seed from `runSeed | buildId | scenarioId`. This is reproducible and separates scenario streams, but it does **not** give competing builds common random conditions: the build ID changes the proc, magnitude, and targeting trajectories. Fast combat derives three deterministic streams from that seed. They affect chance effects and random conditions, crit/dodge/block rolls, randomized magnitudes, randomized target ties, and threshold-sensitive clear/survival outcomes. The optimizer historically reused one root run seed for authoritative scoring, so a build could be rewarded for compatibility with one build-specific RNG trajectory.

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

Milestone 4 orders sampled builds by measured aggregate performance, but it does not alter candidate generation. This preserves the random baseline required for Combat Rating analysis and later optimizer seeding.

The existing Admin Essence Simulator remains a complementary signal:

- it measures Essence and team performance in its 1v1/3v3 comparison model;
- the PvE suite measures complete builds against synthetic encounter demands;
- Milestone 6 currently optimizes complete builds from the PvE signal while retaining random injection and diversity pressure; Admin Essence Simulator evidence remains complementary input for the later meta-analysis milestone.

## Report Contract

Every balance run includes benchmark results in `summary.json`, summarizes profile ranges and leading sampled builds in `summary.md`, and writes full results to `benchmarks.json` under both `latest` and the immutable history directory. This contract was introduced with balance schema version 4; the current combined pipeline uses schema version 46. PvE benchmark scoring version 2 preserves the five scenario scoring formulas and additionally records average initial-friendly health-deficit ratio once per completed tick for uncensored AttritionResilience diagnostics.

Each build result records its profile, aggregate score, scenario component scores, deterministic seeds, and raw combat measurements.

## Experimental common-seed objective audit

Certification analyzer algorithm v21 retains the legacy objective for comparison and adds an opt-in, verdict-isolated common-random-number audit. Every directly compared candidate faces the same frozen seed panel in each of the five scenarios. The audit serializes the exact seed panel, per-seed aggregate scores, scenario means and standard deviations, legacy and reference ranks, rank changes, confidence intervals, panel-size ranking metrics, promotion-depth telemetry, actual cumulative runtime, and projected complete-search runtime.

The seed-`8471` experiment evaluated a 512-build score-stratified E5 cohort plus the strongest 50 legacy E4 and E6 candidates over a nested 32-seed panel. The final immutable rerun produced 97,920 authoritative scenario executions in 48.8 seconds. The smaller prefixes were compared with the same 32-seed reference using gates frozen before the run: Spearman `>= 0.98`; top-10/20/50 overlap `>= 80%/85%/90%`; elite-top-50 and finalist pairwise agreement `>= 95%`; and no more than 2% reversals among reference-elite pairs separated by at least one point. A panel also had to pass together with the next larger prefix.

No submaximal panel passed. Four seeds fit the projected complete-search runtime at 10.0 minutes, but recovered only 82% of the reference top 50 and 81.6% of elite pair ordering. Twenty-four seeds reached 100%/90%/98% top-10/20/50 overlap but only 93.8% elite pair ordering and projected to 60.8 minutes. Only the 32-seed reference passed its own statistical gates, with an 80.9-minute complete-search projection. The robust objective is therefore not promoted and the population-size search experiment remains blocked.

Variance is concentrated in `pve.three-targets` and `pve.short-single-target`; sustained and attrition scenarios are much less seed-sensitive. A progressive architecture is worth a separate bounded experiment because a four-seed broad stage retained all reference top-50 builds within its top 100 in this cohort, but it must be validated prospectively and combined with caching before it can replace the legacy objective.

With production content current when this milestone was implemented, seed `8471`, and the default ten random builds per profile, the measured aggregate ranges were:

| Profile | Aggregate score range | Leading sampled build | Score |
| --- | ---: | --- | ---: |
| `E4_RANDOM` | 54.10–67.17 | `E4_RANDOM_008` | 67.17 |
| `E5_RANDOM` | 59.10–72.30 | `E5_RANDOM_003` | 72.30 |
| `E6_RANDOM` | 52.72–75.68 | `E6_RANDOM_007` | 75.68 |

These values are measured outputs rather than acceptance constants. They demonstrate meaningful performance spread inside CR-identical profiles and may change with production content, engine rules, or documented benchmark tuning.

Milestone 5 consumes each build's displayed/raw CR and aggregate benchmark score to measure how accurately CR predicts this observed spread.

## Verification Boundary

Automated coverage verifies:

- all five scenarios execute for every generated build;
- repeated runs with identical inputs produce identical benchmark results;
- scenario metrics and component scores remain within their defined bounds;
- aggregate scores are calculated from the retained component scores;
- profile ranking uses benchmark performance rather than Combat Rating;
- JSON and Markdown output includes immutable historical benchmark data.

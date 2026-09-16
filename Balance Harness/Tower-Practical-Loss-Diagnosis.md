# Saved-loss diagnosis: investigate Seal and pillar handling

**The next substantive question is how these teams handle Kharad’s recurring area damage and temporary pillars.** All 279 losses among the three confirmed recipes were complete party wipes. Seal of Ascension contributed about 68% of Kharad’s recorded ability damage in those losses. None of the 5,716 pillars created across all 768 confirmations was killed. These observations justify a focused mechanics investigation; they do not prove that changing a particular Essence or killing pillars would improve win rate.

This is a descriptive analysis of the closed pilot’s existing compact reports. It does not change `ImprovementNotDemonstrated`, pool earlier experiments, add samples or reopen the pilot. The [team handoff](Tower-Practical-Team-Handoff.md) remains the exact recipe reference, and the [execution review](Tower-Incumbent-Practical-Pilot-Execution-Review.md) remains the performance decision.

## What losing runs have in common

| Team | Losses /256 | Median first character death in losses | Median wipe time | Median boss health at wipe | Seal share of recorded boss damage in losses |
| --- | ---: | ---: | ---: | ---: | ---: |
| Selected challenger | 89 | 53.9 s | 100.0 s | 18.18% | 67.57% |
| Anchor 040e | 95 | 54.6 s | 98.2 s | 21.11% | 67.92% |
| Anchor 49f6 | 95 | 54.6 s | 101.1 s | 19.52% | 67.75% |

Every defeat ended with zero living characters and termination reason `Defeat`; there were no draws or time-limit terminations. The challenger’s losing boss-health interquartile range was 10.10–29.23%, so its losses are not uniformly near-finished wins. Aggregate win rates alone hide this substantial attrition.

First deaths occur later among victories that contain a death: medians are 83.4 seconds for the challenger and anchor 040e, and 74.75 seconds for anchor 49f6. Those summaries include only 140/167, 131/161 and 142/161 victories respectively; victories with no death are excluded rather than assigned a fabricated death time. Winning and losing populations also have different durations. These conditional descriptions are not causal estimates or a validated selection fitness.

In 208/279 losses, neither heavy-equipped character 1 nor 6 was among the first deaths. Simultaneous first deaths are counted together. This does not support a simple diagnosis that losing the highest-health front character always starts the collapse. It supports examining party-wide pressure as well as single-target survival. Ability totals cannot tell which attack caused a particular death.

## Pillar expiry is not pillar destruction

| Team | Pillars created | Killed | Expired | Still active at combat end |
| --- | ---: | ---: | ---: | ---: |
| Selected challenger | 1,932 | 0 | 1,858 | 74 |
| Anchor 040e | 1,886 | 0 | 1,778 | 108 |
| Anchor 49f6 | 1,898 | 0 | 1,818 | 80 |
| **All three** | **5,716** | **0** | **5,454** | **262** |

The analysis distinguishes an explicit death from an ended summon with no death, and independently reconciles expiry totals with the summoner’s `summonsExpired` counter. There were zero twin-pillar groups with both members killed. A summary field saying a hostile window cleared, or an ended pillar having zero final health, must not be interpreted as a kill. [CombatStatsAggregator](../LL/src/Infrastructure/Service/Services.LL/Combat/Stats/CombatStatsAggregator.cs) records death and expiry separately.

The same zero-kill pattern appears in all 489 victories. Pillar failure therefore cannot explain the win/loss difference by itself, and winning without killing pillars is demonstrably possible in this cohort. There is no pillar-control comparison group in these saved data.

## What the captured mechanics say

The pilot’s captured [ability definitions](../TestResults/balance/tower-incumbent-practical-pilot-20260916/content/Data/combat/abilities.json) and [summon definitions](../TestResults/balance/tower-incumbent-practical-pilot-20260916/content/Data/combat/summons.json) specify:

- Twin pillars last 120 ticks, or 12 seconds at the recorded ten ticks per second. Each pillar has 10% of Kharad’s maximum health. Expired pillars feed Resonance; destroying both permits a stack reduction.
- Seal of Ascension grants a barrier worth 5% of maximum health for up to ten seconds and delivers periodic magical area damage while linked to that barrier. Its recorded damage dominates both losses (about 68%) and victories (about 63%).
- Breaking the Seal barrier and letting it expire are different Resonance triggers. Resonance raises Power, Attack Speed and Damage Reduction, with a lock described at five stacks.

This makes Seal/barrier handling, pillar damage within their lifetime, and party survival under area pressure plausible coordinated-team concerns. It does **not** establish that the teams reached a particular Resonance state before their first deaths, that a Seal pulse killed them, or that diverting damage to pillars has positive net value. A pillar-focused party might sacrifice enough boss damage or healing to perform worse.

## Smallest useful next investigation

Before designing another search policy, establish whether the declared legal pool and fixed ability order can materially affect these mechanics:

1. Trace barrier-break/expiry and pillar-group resolution through the existing engine and telemetry. Determine which compact counters are already available and which would be needed to distinguish Seal suppression, pillar kills and Resonance exposure. The saved reports cannot recover absent events.
2. Inspect legal ability targeting, timing and damage/support interactions for ways to affect those mechanisms within the unchanged character, gear, copy and order constraints. Identify concrete feasible compositions or report that feasibility is unresolved; do not substitute role labels for mechanical evidence.
3. Only if that source-level investigation yields a concrete mechanism should a separate bounded diagnostic design specify the exact compositions, observation fields, fresh evaluation and stopping rule. Keep search promotion distinct from explaining a mechanic, and preserve the old pilot’s result.

The minimum immediate step is thus a **zero-combat feasibility and telemetry review**, not a new mutation ratio or another confirmation panel. No implementation, fresh allocation, combat or parameter tuning is proposed for automatic execution by this diagnosis. There is no supported recommendation yet to replace an anchor or optimize an individual Essence.

## Evidence, checks and limits

All 768 saved confirmation archives were read once, their compressed bytes matched the sealed inventory, and their outcomes matched the already audited evidence. The [per-trial summaries](../TestResults/balance/tower-practical-loss-diagnosis-20260916/trial-summaries.json) and [aggregate diagnosis](../TestResults/balance/tower-practical-loss-diagnosis-20260916/diagnosis.json) preserve the calculations. The analysis scope selected all three recipes and all confirmation values, with descriptive outcome splits; it selected no favorable subset. These are still post hoc descriptions, and the 768 observations share 256 values across teams rather than forming 768 independent samples.

The [battle adapter](../LL/tools/BalanceHarness/TowerBattleRunner.cs) disables full event capture, and every archived `eventLog` was null. Compact statistics provide first-death times and ability totals but no full time series of damage, barrier breaks or Resonance. This work did not run combat playback, reconstruct a missing event log or claim a causal mechanism.

This continuation adds the diagnosis, its small analysis/preservation package, and a pointer in the README and five handoff documents. Verification checks source/archive hashes, outcome and lifecycle reconciliation, recomputation from the extracted summaries, links, whitespace, previous package preservation and unrelated dirty files. Backend builds/tests are unnecessary for this saved-data/documentation change and were not run. No required command remains blocked. There are no gameplay changes, migrations, persistent configuration changes or deployment implications.

The scope is capped at **8 diagnostic seconds /2 MiB**, charged to the remaining engineering balance without changing component or overall caps. The [completion receipt](../TestResults/balance/tower-practical-loss-diagnosis-20260916/completion.json) carries forward all prior charges. All **483,640** exclusions are preserved, including V19’s 512 unused confirmation values. The pilot remains **Closed**, adoption **Hold**, and V19 reliability **Unresolved**.

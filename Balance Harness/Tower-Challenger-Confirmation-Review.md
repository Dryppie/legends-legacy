# Kharad challenger confirmation — 12 September 2026

Fresh confirmation establishes **two stronger builds that exceed the 50% ceiling** at Kharad's current settings. They won **564/1,000 (56.4%)** and **565/1,000 (56.5%)**, versus **471/1,000 (47.1%)** for the historical leader on the same new seeds. Both have adjusted lower win-rate bounds above 50% and supported paired improvements over the leader. The expanded benchmark therefore **fails** the approved 10–50% policy.

The complete job used **4,004 fights**, **86.98 seconds** of driver runtime and about **155 MiB** initially. The package is about **162 MiB** after independent evaluation and review receipts. All exact recipes and confirmation evidence are saved for reuse. No boss setting or retained catalog was changed. The historical 2,438-recipe Pass remains valid within its recorded scope; it cannot establish balance against these newly confirmed builds. Search quality and near-optimality remain unresolved.

## Frozen experiment

This is the separately authorized confirmation of the [bounded challenger pilot](Tower-Bounded-Challenger-Pilot-Review.md). All three pilot candidates observed above 50%, plus its predeclared historical leader, were selected before new outcomes. Their full recipes, actual identities, ordered Essences and equipment match the pilot exactly. The encounter scenario ID remains `competitive-calibration-floor-5` for every team.

The [protocol](../TestResults/balance/tower-challenger-confirmation-20260912/protocol.json), SHA-256 `545895142c077a72ed8d24c0e7d9e7e8dfa9dd1bdb5339d2f4011562d556b210`, freezes:

- Floor 5, Kharad Health **2.931552** / Power **3.705482**, ten characters, level 40, Standard tier-1 rank-2 fixed gear and five level-1 unascended/unevolved Essences per character. The full pool and hypothetical ownership assumptions continue; practical acquisition is separate coverage.
- The same **1,000 fresh paired seeds per team**, excluding **466,195 historical values** imported from 93 recorded sources. Prior pilot seed reservations are included. Known source files and all 3,500 files in the preceding pilot's artifact inventory passed their hashes before selection was reused. External unregistered jobs are outside the history audit.
- Exactly **4,000 confirmation fights**, one detailed replay per team on the first confirmation seed, and 32 reserved retry attempts: **4,036 maximum fights**. All four teams must complete; results cannot change the sample size.
- Sequential compact prepared execution, 32-record chunks, a 240-second active campaign limit, a 300-second driver cancellation budget and 2 GiB output limit. Limits are cooperative checks rather than hard OS quotas.
- Current producing assemblies, source hashes, selected combat settings, all 16 content files, the diagnostic driver and exact input definitions. The driver uses the existing tested harness build; no gameplay or harness implementation changes were required.

All **4,004 attempts completed**, with zero retries or lost attempts. All 32 retry attempts remained unused. The three challengers and leader each have exactly 1,000 distinct observations. Paired seeds are shared deliberately; their use across four teams does not create 4,000 independent random environments. Replay attempts reproduce earlier seeds and do not increase statistical sample sizes.

## Confirmation results

The table uses the rate intervals from the separately frozen joint uncertainty allocation described below.

| Case | Changes from historical leader | Wins / 1,000 | Rate | Adjusted rate interval |
| --- | --- | ---: | ---: | ---: |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | Character 3: Goblin Warrior → Venomous Spiderling; character 5: Cinder Beetle → Goblin Warrior | 564 | **56.4%** | **52.08–60.62%** |
| `team-5b7f0a2637ddc8253f7e191d25e9d560` | Character 7: `essence.hobgoblin_brutal_charge` → Web Weaver Spider | 512 | **51.2%** | **46.88–55.50%** |
| `team-ac1b1cac3fd315d19ad1606ab43ac036` | Character 3: Goblin Warrior → Venomous Spiderling | 565 | **56.5%** | **52.18–60.72%** |
| `team-693ffa8ec0b654154a06722aba06a968` | Historical leader, unchanged | 471 | **47.1%** | **42.82–51.42%** |

Every substitution occupies the same Essence slot as in the pilot: character 3's fourth, character 5's fourth and character 7's fifth. All other recipe fields match the selected reference. One Essence change in the whole party is sufficient to produce a supported improvement here. This reinforces the need to keep searching combinations around saved strong builds; it does not establish the new ceiling of the legal search space.

| Challenger | Paired gains / losses versus leader | Observed improvement | Adjusted paired interval |
| --- | ---: | ---: | ---: |
| Two substitutions, `a954…` | 273 / 180 | **+9.3 percentage points** | **+1.72 to +16.73 points** |
| Web Weaver Spider, `5b7f…` | 260 / 219 | **+4.1 points** | **−3.64 to +11.77 points** |
| One substitution, `ac1b…` | 263 / 169 | **+9.4 points** | **+1.95 to +16.69 points** |

Two challengers have supported breaches and improvements. The Web Weaver Spider candidate's 51.2% observation remains an above-ceiling finding under the existing policy, even though this sample does not establish its underlying rate above 50% or a paired improvement. Keep it in future calibration. All four teams support at least 10% viability. The leader's new sample alone does not establish an upper bound at 50%; its historical 50,000-sample result is separate evidence and is not pooled here.

The two leading challengers differ by just one win in 1,000. This workload does not establish which is stronger: it predeclared comparisons against the historical leader, not between challengers. Retain both. The [complete results](../TestResults/balance/tower-challenger-confirmation-20260912/results.json), [observations](../TestResults/balance/tower-challenger-confirmation-20260912/observations.json) and [paired comparisons](../TestResults/balance/tower-challenger-confirmation-20260912/paired-comparisons.json) preserve all measurements, including the less certain third challenger.

## Interpretation and uncertainty

The ordinary `tower-balance-v1` evaluator and its four-cell, approximately simultaneous 95% Wilson policy remain unchanged. It returns **Fail** because the three observed challenger rates exceed 50%. Independent CLI evaluation reproduced its assessment bytes and evidence exactly. That expected exit code 1 is a valid balance failure, not an execution or integrity failure.

The driver additionally froze a conservative joint interpretation before combat: alpha **0.025** for the four win-rate intervals and alpha **0.025** for the six discordant gain/loss probabilities across three leader comparisons. Existing Wilson calculations use effective family sizes **8** and **12**, respectively. Subtracting each loss interval from its gain interval produces the paired difference bounds. This gives approximate 95% simultaneous coverage of the declared rate and comparison claims by a union bound; it is not a new production evaluator or a general outcome-dependent stopping policy.

No discovery, pilot selection or historical samples were pooled into confirmation. There was no optional extension, missing-team exclusion or winner-only acceptance. This four-team test can demonstrate a breach in the wider known pool; it cannot accept omitted teams, all 65 pilot recipes, the historical 2,438-recipe portfolio or unsearched combinations. It also provides no lifetime guarantee across an unlimited sequence of studies.

## Reuse, performance and verification

The [confirmed candidates](../TestResults/balance/tower-challenger-confirmation-20260912/confirmed-candidates.json) save all four exact scenarios, fresh results and scope. Their seed arrays are empty deliberately: assign a separately frozen fresh schedule for future evidence. Calibration can load these recipes directly without repeating discovery. The ordinary retained catalogs remain unchanged; this compact confirmation package is a separate source of confirmed candidates, rather than an automatic catalog promotion.

| Phase | Fights | Seconds |
| --- | ---: | ---: |
| Campaign execution, inline verification and standard assessment | 4,000 | 71.70 |
| Complete campaign reconstruction | 0 | 2.82 |
| Streaming paired-observation verification | 0 | 2.40 |
| Four matching detailed replays | 4 | 9.76 |
| Timed phases | **4,004** | **86.68** |

The enclosing driver measured **86.98 seconds**, including its checks and operations between phases. CPU totaled **82.84 seconds**, cumulative allocation **41.07 GiB**, and process-lifetime peak working set **357.79 MiB**. Allocation is garbage-collected traffic, not simultaneous memory or disk usage. Engine simulation used **53.80 seconds** of the campaign phase. All four teams are competitive and have longer fights than weak independent candidates in the pilot; this workload is not a matched speed comparison. The later independent CLI evaluation and review/integrity work are outside the driver timing. The prior pilot's small-batch overhead remains a separate profiling target.

Verification completed:

- Driver restore/build and full preflight succeeded; the build had zero warnings/errors. Restore used its empty package-source configuration and approved access to local NuGet configuration.
- `TowerCompactBalanceRun.VerifyAsync` reconstructed the complete assessment without new combat. Streaming verification buffered at most **32 full reports**. Four fresh detailed replays matched.
- The retained executable's `tower-balance-evaluate --definition .../definition.json --sources .../campaign/sources.json --output .../independent-evaluation` returned expected **Fail / exit 1** and byte-identical assessment output.
- [Python analysis](../TestResults/balance/tower-challenger-confirmation-20260912/analyze.py) independently verified all seeds, selected recipes, counts, durable attempts, three kinds of rate intervals and every paired interval using `statistics.NormalDist`.
- The [final verification receipt](../TestResults/balance/tower-challenger-confirmation-20260912/final-verification.json) checks protected history, the complete preceding pilot, current content, sources, producing assemblies, catalogs and documentation links. `git -c core.safecrlf=false diff --check` passed.

The producing build is identical to the [previous 270-test regression run](Tower-Resumable-Bulk-Integration-Review.md#verification). Those backend tests were not rerun for this execution/documentation-only increment. New files are this review and the ignored confirmation driver, evidence and analysis scripts. Active discovery/loadout/harness plans, the acceptance policy and harness README are updated. No required verification remains blocked. There are no game configuration changes, migrations, deployments or external environment effects; the local driver has its own empty-source NuGet configuration.

## Next bounded calibration

Kharad needs a separate calibration against the expanded known builds. Start with a frozen **2,000-fight linked Health/Power screen**: multipliers **1.00, 1.02, 1.04, 1.06 and 1.08** relative to current settings, all four confirmed teams and 100 fresh paired seeds per cell. Reserve four replays and 32 retries for a **2,036-fight cap**. Use isolated local content copies and unchanged gear/Essence budgets. A screen chooses a setting to confirm; it does not apply a setting or grant balance acceptance.

Before running that screen, freeze the selection rule and exact content/seeds. Prefer a result with room below 50% over another boundary estimate, while retaining the requirement for at least one supported 10% team at confirmation. A setting with no adequate evidence remains unresolved at the cap. Then freeze fresh confirmation covering the complete relevant known family, including all three challengers and earlier breaches. Do not assume higher Health/Power makes every recipe monotonically weaker or shrink the family to obtain a Pass. The earlier proposal for a versioned staged allocation policy remains separate work; do not silently introduce optional stopping or repeat a 600k-fight schedule by default.

Independent discovery reliability, practical ownership and the floors 6–11 progression audit remain open. The new evidence changes Kharad's current expanded benchmark status to **Fail** while preserving all sealed historical assessments.

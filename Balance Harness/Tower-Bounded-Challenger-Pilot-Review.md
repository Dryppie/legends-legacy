# Bounded Kharad challenger pilot — 12 September 2026

The bounded pilot completed **6,212 combats** and found three new supplied-team improvements with observed validation rates above 50%. The strongest won **39/64 (60.94%)**, versus **32/64 (50%)** for the predeclared saved leader on the same fresh seeds. These are promising selection results, with insufficient precision to establish a ceiling breach or a paired improvement. They must remain visible and receive fresh confirmation before any tuning decision.

The measured execution, reconstruction and replay phases totaled **154.40 seconds**, with approximately **276 MiB** retained including the driver, dependencies, frozen history, archives and exports. This demonstrates a practical small campaign; it is not a matched performance comparison with earlier workloads. Boss settings and retained catalogs were not changed. Kharad's historical 2,438-recipe portfolio Pass remains scoped to that evidence; the enlarged candidate family has no new acceptance decision, and search quality remains Fail.

## Frozen scope and accounting

The target was the offline `LL/tools/BalanceHarness`, using the existing tested compact/prepared execution build. The [protocol](../TestResults/balance/tower-bounded-challenger-20260912/protocol.json) froze the driver, content/settings/assembly identities, input definitions, seeds, starting recipes and limits before combat. Its SHA-256 is `fd8fb8cf17588a6851d81d2a915097a561b568621dd0d3b94f5d2780c3e4f470`.

- Floor 5, Kharad: Health **2.931552**, Power **3.705482**.
- Ten characters, five level-1 unascended/unevolved Essences each, level 40, Standard tier-1 rank-2 fixed equipment. Full allowed pool and hypothetical ownership remain explicit; practical acquisition is not established.
- Two independent methods (`random`, `constructive-joint`) and two supplied-team methods (`retained-local`, `retained-joint`), each with two generation restarts and 32 evaluated candidates per restart. Both modes share the same eight fresh discovery seeds.
- All **49 distinct saved controls**, plus both complete eight-party discovery shortlists, enter a frozen **65-team** validation family. There was no exact overlap to remove. Every team receives the same separate 64-seed schedule. Actual actor identities, equipment and Essence order remain explicit; scenario IDs are standardized to the historical leader's encounter context.
- Four supplied parents were selected before combat from the highest observed retained controls in the sealed 2,438-recipe calibration: `party-79b8f51676888b540622e5f8`, `party-a1cd25815e2a38e729637eff`, `party-5a762665cb7843bc409a916b` and `party-452f7766867ada0f5086e598`. Historical scores select starting recipes only; they do not enter current fitness.
- **466,119 distinct historical exclusions** were imported from 91 recorded sources. The 76 allocated values comprise two generation seeds, eight discovery seeds, 64 validation seeds and two unused contract-stage seeds. They are disjoint from each other and the imported history. There are 72 distinct new combat seeds; common seeds deliberately pair teams. External unregistered jobs remain outside this history audit.

| Stage | Planned / actual fights |
| --- | ---: |
| Independent discovery: 128 candidates × 8 | 1,024 |
| Supplied-team improvement: 128 candidates × 8 | 1,024 |
| Fresh validation: 65 teams × 64 | 4,160 |
| Detailed replays | 4 |
| Actual total | **6,212** |
| Unused retry reservation | **96** |
| Frozen maximum | **6,308** |

Durable journals and combat trace counts agree: 6,212 started/completed attempts including replays, zero retries or lost attempts. Both campaign reconstructions execute zero additional fights and exactly reproduce their saved reports. Validation verified all 4,160 reports while buffering at most 32 full reports. Four fresh detailed replays matched; the retained and overall leader were the same team, so that identity was deliberately replayed twice under the frozen four-attempt rule.

Limits were 2 GiB for the whole package, 1 GiB and 1,200 active seconds per search, and a 1,800-second driver cancellation budget. Storage/time checks are cooperative boundaries, not hard OS quotas. No cap extension, automatic resume, optional statistical extension or additional confirmation was used.

## What the search found

All 128 independent candidates won zero of their eight discovery fights. Their eight finalists also won zero of 64 each on validation. The best independent validation finalist left Kharad with mean **78.17% HP**. This very small search did not independently recover competitive combinations. It neither proves that the independent methods cannot succeed at a larger budget nor supports a near-optimality claim.

Supplied-team search found clears in 24/32, 21/32, 24/32 and 21/32 candidate evaluations across its four arms. Their best eight-seed discovery rates were 75%, 50%, 75% and 62.5%. These are discovery rankings only. Fresh validation ranked the leading recipes as follows:

| Validation case | Source | Wins / 64 | Observed rate |
| --- | --- | ---: | ---: |
| `team-a954394f09e052e5e9c5d1dbaee5331b` | Supplied-team improvement | 39 | **60.94%** |
| `team-5b7f0a2637ddc8253f7e191d25e9d560` | Supplied-team improvement | 38 | **59.38%** |
| `team-ac1b1cac3fd315d19ad1606ab43ac036` | Supplied-team improvement | 34 | **53.13%** |
| `team-693ffa8ec0b654154a06722aba06a968` | Predeclared historical leader | 32 | **50.00%** |

Compared with the historical leader, the strongest team changes only character 3's fourth Essence from Goblin Warrior to Venomous Spiderling and character 5's fourth from Cinder Beetle to Goblin Warrior. The second changes character 7's fifth Essence from `essence.hobgoblin_brutal_charge` to Web Weaver Spider. The third makes only the character-3 substitution. This is evidence that small changes around a strong saved party are worth testing; it does not isolate each change's causal contribution.

The strongest team's pointwise 95% Wilson interval is **48.69–71.94%**; its 65-team adjusted interval is **40.31–78.28%**. Against the frozen leader it gained 16 paired wins and lost 9, an observed **+10.94 percentage points**, with the predeclared approximate simultaneous interval **−24.16 to +42.44 points**. No candidate has a supported paired improvement or an adjusted lower bound above 50% in this small sample. Conversely, none of the three observed above-ceiling candidates can be accepted as within the desired band. Failure to prove a breach is not a balance Pass.

The [complete validation results](../TestResults/balance/tower-bounded-challenger-20260912/validation-results.json), [per-seed observations](../TestResults/balance/tower-bounded-challenger-20260912/validation-observations.json) and [paired comparisons](../TestResults/balance/tower-bounded-challenger-20260912/paired-against-frozen-leader.json) preserve every control and finalist. Validation is fresh selection screening, not confirmation of a subsequently selected winner. Historical 47.67% and current 50% leader measurements are separate samples and are not pooled.

## Saved builds and future reuse

The [saved top ten](../TestResults/balance/tower-bounded-challenger-20260912/saved-candidates.json) contain exact full recipes and their selection results. The [strongest recipe export](../TestResults/balance/tower-bounded-challenger-20260912/recipes/team-a954394f09e052e5e9c5d1dbaee5331b.json) and nine peers in `recipes/` have empty seed arrays deliberately: assign a new frozen schedule before executing them. Both discovery shortlists and the complete validation family also retain all other builds.

These are **saved candidates requiring fresh confirmation**, not automatically promoted retained controls. The local and published retained catalogs remain unchanged. Future work can load these exact candidates without repeating discovery. Archived schedules are for verification/reproduction; fresh competitive evidence must exclude this pilot's seeds too. Retain each complete search campaign together because its child batches reference the parent's shared content. Detailed replays use the retained producing executable and matching runtime/platform.

## Measured cost and remaining improvements

| Phase | Fights | Elapsed seconds | Allocated GiB |
| --- | ---: | ---: | ---: |
| Independent search, including inline verification | 1,024 | 38.60 | 9.10 |
| Independent full reconstruction | 0 | 2.93 | 3.42 |
| Supplied-team search, including inline verification | 1,024 | 44.69 | 14.20 |
| Supplied-team full reconstruction | 0 | 2.92 | 3.56 |
| Validation archive creation | 4,160 | 51.91 | 22.54 |
| Validation full verification | 0 | 2.64 | 1.29 |
| Four fresh detailed replays | 4 | 10.71 | 5.27 |
| Total timed phases | **6,212** | **154.40** | **59.38** |

The [analysis](../TestResults/balance/tower-bounded-challenger-20260912/analysis.json) and `resource-*.json` record CPU, allocation and inclusive/exclusive trace stages. CPU totaled **142.25 seconds**. Process-lifetime peak working set was **628.32 MiB**. Allocation is cumulative garbage-collected traffic, not simultaneous memory or saved disk space. Timed phases exclude protocol preparation, driver compilation, small operations between phases, and later review/integrity work. Current receipts occupy about 276 MiB; logical bytes do not measure filesystem allocation or compression.

Prepared engine simulation consumed **7.64 seconds** in independent search and **10.31 seconds** in supplied search, about **21.6%** of their combined 83.29 seconds. The rest includes generation, contract/materialization checks, journal/archive work, hashing, content loads, verification and the pilot driver's directory-size checks. Each search creates 128 small eight-fight archives. Around **25.34 seconds** across those searches is outside named trace scopes; a further **22.91 seconds** is inside archive creation but outside its named child stages. These are separate, incompletely attributed costs. The current trace does not isolate durable `Flush(true)` calls, so it cannot establish that journaling is the dominant cause.

Validation batched 65 recipes in one archive: engine simulation was **32.45 of 51.91 seconds**. This points toward fixed costs per discovery batch as a useful optimization target, but the recipe strengths, battle lengths and workloads differ, so the timings are not an A/B speedup claim. Every detailed replay also verifies the complete validation archive; four selected replays therefore repeat substantial verification, accounting for most of their 10.71 seconds.

Before another large campaign, add separate timings for candidate generation, preflight/materialization, size scans, durable attempt writes and final inventory work. Then test bounded campaign-owned reuse of immutable content and incremental storage accounting, retaining tamper detection, durable attempts, deterministic adaptive order and complete reconstruction. Do not weaken crash accounting or skip verification on the assumption that it is expensive. The older prepared-mode 27% improvement remains specific to its sealed matched benchmark; no 600k-fight runtime projection is justified by this pilot.

## Verification, changed files and next step

The pilot driver built with zero warnings/errors against existing tested harness binaries. Preparation and execution completed successfully:

```powershell
dotnet build TestResults/balance/tower-bounded-challenger-20260912/driver/Driver.csproj --configuration Release --no-restore -p:UseSharedCompilation=false
dotnet TestResults/balance/tower-bounded-challenger-20260912/driver/bin/Release/net10.0/Driver.dll prepare
dotnet TestResults/balance/tower-bounded-challenger-20260912/driver/bin/Release/net10.0/Driver.dll run
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -X utf8 -B TestResults/balance/tower-bounded-challenger-20260912/analyze.py
& 'C:/Users/HrHoe/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' -X utf8 -B TestResults/balance/tower-bounded-challenger-20260912/verify-final.py
git -c core.safecrlf=false diff --check
```

These are retained execution records, not instructions to rerun into existing output: preparation/export scripts reject existing files. Python independently checked every validation seed order/count, pointwise and family Wilson intervals, all 64 paired comparisons, seed exclusions and attempt accounting. The [final verification receipt](../TestResults/balance/tower-bounded-challenger-20260912/final-verification.json) checks protected historical evidence, producing assemblies, live content/settings inputs, source hashes and saved catalogs. The prior [270 passing regression tests](Tower-Resumable-Bulk-Integration-Review.md#verification) cover this unchanged producing build; they were not rerun for this workload/documentation-only increment.

Initial driver setup needed sandbox approval to read NuGet configuration during restore; that succeeded. Compilation initially encountered internal harness helpers and was corrected in the diagnostic driver before freezing the protocol or running combat. No harness implementation or gameplay changes were needed, and no required command remains blocked. New files are this review and the ignored pilot driver/evidence/recipe exports; active discovery, loadout, harness and acceptance plans plus the harness README are updated. There are no game configuration changes, migrations, deployments or external environment effects. The empty-source NuGet configuration is local to the diagnostic driver.

**Next bounded workload:** freeze the three observed above-ceiling challengers plus the historical leader for **1,000 new paired seeds each**, reserving four replays and 32 retry attempts: **4,036 fights maximum**. Keep current boss settings and the same character budget. Use separately declared family-adjusted ceiling and paired-comparison rules, with no optional sample extension. This would confirm candidate strength and identify tuning needs; it would not accept the complete 65-team or historical 2,438-recipe portfolio by omitting other teams. A confirmed breach requires a separate calibration against the complete relevant known family. Independent search reliability and the floors 6–11 progression audit remain open.

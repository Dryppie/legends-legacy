# Kharad calibration after v13 replication

The frozen setting screen selected **+16% linked Health/Power**, giving **Health 3.5366243328 / Power 4.4702934848**. Fresh confirmation of all **32 nominated recipes** returns **Pass**, with separate joint assessment **Pass**. The strongest build won **149/512 (29.10%)**; its ordinary adjusted interval is **23.20%–35.80%**. Fresh v13 reliability at the candidate is **Fail (0/3)**.

**Application decision: Hold.** The candidate is saved for broader confirmation. Historical and unshortlisted recipes outside this confirmation remain unresolved. Live Kharad remains Health **3.04881408** / Power **3.85370128**. No game, runtime, catalog, configuration, migration, database or deployment change occurred.

Read the [precombat design](Tower-Kharad-V13-Calibration-Plan.md), [frozen protocol](../TestResults/balance/tower-kharad-v13-calibration-20260913/protocol.json), [candidate](../TestResults/balance/tower-kharad-v13-calibration-20260913/candidate.json), [summary](../TestResults/balance/tower-kharad-v13-calibration-20260913/summary.json), [independent audit](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/exports/analysis.json), and [confirmed recipes](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/exports/saved-builds.md).

## Replay findings and calibration choice

Four already-paid first-screen-seed replays were inspected with zero new combat. The two successful primaries won at 72.1 and 78 seconds, with nine and eight characters surviving. Kharad still used Seal of Ascension four times, Crushing Verdict five times and three summon waves in each. Those two trials contained two and one stagger breaks respectively. These observations support testing the existing linked Health/Power controls first; they do not establish a causal ability explanation or a general claim about control uptime.

[Replay measurements](../TestResults/balance/tower-kharad-v13-calibration-20260913/replay-findings.json) retain action, healing and control telemetry. Only floor-5 Health/Power differ across isolated content copies; restoring those fields reproduces the baseline JSON. Fixed equipment, character progression, five level-1 Essences and all 80 eligible Essences under hypothetical ownership remain unchanged.

## Complete setting screen

Every setting tested all 20 saved finalists/controls on the same 64 fresh seeds. Selection required the maximum observed rate to be 15–40%, nearest 30%, ties to the smaller factor. No interpolation or additional settings were added. These selection samples are not confirmation evidence.

| Linked increase | Maximum wins / 64 | Maximum rate |
| ---: | ---: | ---: |
| 0% | 63 | 98.44% |
| 2% | 60 | 93.75% |
| 4% | 54 | 84.38% |
| 6% | 51 | 79.69% |
| 8% | 45 | 70.31% |
| 10% | 39 | 60.94% |
| 12% | 35 | 54.69% |
| 16% | 23 | 35.94% |
| 20% | 11 | 17.19% |
| 30% | 0 | 0.00% |
| 40% | 0 | 0.00% |

## Fresh search and confirmation

Both unchanged v13 methods ran three fresh paired restarts with 384 candidates per arm and eight discovery seeds. Saved controls were absent from independent generation. The complete 2,304 evaluations and ordered recipes remain saved; the audit independently checked same-arm ancestry, source-ranked loadout libraries and nomination order.

The confirmation family contains all 20 controls, both discovery-ranked finalists from each arm, and every new discovery breach above 50%, deduplicated only by exact recipe. It contains **32 recipes**, within the frozen capacity of 64. Every nominated recipe received the same **512 untouched seeds**. Ordinary intervals allocate alpha .05 across the entire family. The separate joint analysis allocates .025 across rates and .025 across six paired comparisons. No samples are pooled across stages or studies.

| Method | Generation seed | Primary wins / 512 | Secondary wins / 512 |
| --- | ---: | ---: | ---: |
| coverage-deep-joint | 1144935365 | 0 | 0 |
| loadout-composition-joint | 1144935365 | 7 | 35 |
| coverage-deep-joint | 528558651 | 0 | 0 |
| loadout-composition-joint | 528558651 | 18 | 10 |
| coverage-deep-joint | 1031543897 | 0 | 0 |
| loadout-composition-joint | 1031543897 | 0 | 0 |

Largest ordinary adjusted upper: **35.80%**. **4** recipes have an ordinary adjusted lower at least 10%. Full ordinary/joint nominated-family assessment: **Pass / Pass**. [Every rate and paired comparison](../TestResults/balance/tower-kharad-v13-calibration-20260913/search-statistics.json) remains visible, including unsuccessful primaries.

At the candidate, **0/3** new primaries meet all three reliability conditions: adjusted viability, supported improvement over the same-restart deeper-v4 primary, and no more than a supported ten-percentage-point deficit against the fixed anchor. This is separate from the previously completed 2/3 reliability result at different content; near-optimality remains unestablished.

## Application boundary and reusable inventory

The new [combined inventory](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/next-family/summary.json) stores **17,821 distinct typed recipes**, of which **17,789** lie outside this confirmation. It merges the earlier 2,918-party family, the enumerated intervening independent studies and references, preserved partial/recovered discovery records, and the new challenge. Exact duplicate recipes retain all source associations. The compressed [complete seed-free family](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/next-family/family.json.gz) was read back and checked using the harness recipe hash. [Source hashes](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/next-family/sources.json) identify each consumed file.

This inventory is preparation for coverage work, not a new certification of historical outcomes or an exhaustive inventory of unregistered jobs. The current staged evaluator supports at most 10,000 recipes and 500,000 reserved fights; this inventory exceeds its recipe capacity. The next step is to settle and verify a bounded confirmation design with simultaneous coverage across the complete family, then freeze its inputs and allocation. A capacity change or partitioning must preserve the statistical guarantees, known breaches, retry accounting and unresolved cells. Dropping unshortlisted cells or treating separate unadjusted partitions as a single Pass would not resolve this requirement. A passing nominated family alone cannot justify application. Practical acquisition and floors 6–11 remain downstream work.

## Verification and resource accounting

Actual **48,896 completed attempts** = 14,080 screen + 18,432 discovery + 16,384 confirmation. The predeclared maximum was **65,280 attempts**, **5,400 execution seconds**, **4 GiB**, with **zero combat retries and zero new diagnostic replays**. Actual execution used **1792.67 seconds**. Global durable attempt pairs and each compact campaign account for the same work. The captured verifier reconstructs the complete screen, selected setting, generation, family and statistics with a zero-combat guard.

**102 relevant backend tests passed**, run through `build/run-tests.ps1`. The study driver and inventory helper both built with zero warnings. Independent analysis checks fresh seed disjointness, complete evidence, Wilson arithmetic, paired comparisons, primary selection and loadout ancestry. **2,361 preserved inputs** cover game/harness/test C# sources, live content, consumed prior evidence and sealed reviews. The [final receipt](../TestResults/balance/tower-kharad-v13-calibration-work-20260913/final-verification.json) records current documentation and artifact hashes.

Preparation corrected a compile-time property name and supplied the captured sanitized settings file to each isolated content root before freezing. A failed preparation process held its assembly open; its exact loaded module identified it for termination, then the helper rebuilt successfully. A process command-line query was denied by the sandbox; module inspection supplied the needed identification. No battles ran during those corrections, partial seed inputs were checked unchanged, and no execution recovery or allocation amendment was required.

Reconstruction command, from the repository root:

```powershell
dotnet TestResults/balance/tower-kharad-v13-calibration-20260913/executable/Study.dll verify
./build/run-tests.ps1 -NoBuild -Filter "FullyQualifiedName~BalanceHarnessTowerBalanceEvaluatorTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessTowerLoadoutCompositionTests|FullyQualifiedName~BalanceHarnessTowerCompactPublicationTests|FullyQualifiedName~BalanceHarnessTowerBulkTests"
```

Protocol SHA-256: `cab38bbdc96b7fa5b973205d496c8cdafd3c0f550851e679cdffbf3c695931ab`. Latest ledger: **474,838 distinct reservations**, including all 474,187 prior values and 651 new reservations (64 screen, three generation, eight discovery, 64 unused contract-selection and 512 confirmation). Exclude every ledger array in subsequent work. Historical reviews and producing packages retain their original claims and hashes.

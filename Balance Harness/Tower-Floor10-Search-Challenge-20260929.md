# Floor 10: current-runtime qualification and search challenge — 29 September 2026

**Subsequent calibration applied:** the [expanded-family health calibration](Tower-Floor10-Expanded-Calibration-20260929.md) uses all 38 retained recipes and establishes two viable compositions at **72/256 and 68/256**. Floor-10 health is now **12.8371 (+1%)**, offense remains **7.13**. All 9,728 confirmed inputs and 38 full replays match. Both search scopes below remain closed; their settings, counts and failure receipts are historical. Use the accepted calibration as the current baseline and the [handoff](Tower-Continuation-Handoff-20260928.md) for the next queue.

## Prospective scope (before qualification or fresh combat)

Target: primary LL Tower data and the offline Balance Harness. The [consolidated review](Tower-Balance-Status-20260929.md) found floor 10 alone still establishes just one viable composition: alternating armor-and-health, 55/256. The repeated counterpart has 23/256; the other 19 exact recipes have zero wins. This scope seeks a second actual composition using the unchanged `affinity-creation-with-benchmark-validation-v1` search.

Keep guardian **health 12.71 / offense 7.13** and all other game content unchanged. Preserve fifteen level-50 characters, six level-1 unascended/unevolved Essences each, tier-2 Legendary/Masterpiece/rank-5 equipment, baseline rolls, no styles and hypothetical complete ownership. The repeating equipment curve and stronger carried gear remain unchanged. No dungeon, acquisition, selectable-supply, migration or deployment work enters this scope.

The full historical family is `TestResults/balance/tower-floor10-family-confirmation-20260928`, manifest SHA **`95d6e270d606e6773d5b35d043867d6a04a684af397bd30c82db8a6f6f4d7944`**, result SHA **`b2fd5bbbf849d15699f8a66907f68e1c16b53e37837e4cc4e8a71842dfaa7be5`**: 21 exact recipes, three actual compositions and seven gear profiles. Preserve every scenario, actor/item/Essence identity, ordered Essence list and party position. Metadata conversion for the current harness must not rewrite any scenario. The separate 252 five-Essence controls remain historical diagnostics, not fresh samples or universal necessity evidence.

Entry Tower SHA **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**; exclusion union **908,432**. Import the entire current history, including failed/unused reservations, without changing old ledgers.

## Current-runtime qualification

Run a separately bounded native check before any fresh seed allocation. Authenticate the accepted historical archive and audit. Compare all shared Tower metadata and the complete floor-10 definition, allowing the already recorded changes on other floors. All captured catalogs must match except `items/items.json`, where exactly the six existing `item.tower_supply.v1.floor_{01,04,07,10,11,14}` additions are permitted; every older item must remain identical. This is a compatibility check and does not reinstate the withdrawn issuer. Compare sanitized combat settings natively.

Prepare all **21 exact scenarios with combat disabled**, then reconstruct **all 5,376 historical input hashes**. Replay **23 full reports**: the first archived win and first archived loss for each of the two armor-and-health recipes with wins, and the first archived seed for every other recipe. Freeze these IDs before invocation. Match the entire reports and use the current build without historical binary substitution. Qualification uses **zero new seeds**, at most **300 native seconds / 360 owner seconds / 128 MiB / 23 fights**, no retry. On a mismatch, preserve the evidence and stop dependent search work.

The native output also retains the 21 scenarios in the current harness's cell-metadata format. An independent Python check must prove every scenario unchanged before using that qualified snapshot as a preparation source. Its whole content snapshot is current, with old input/report compatibility recorded separately.

## Frozen search and acceptance rule

1. Prepare the qualified 21-cell family with zero fights and zero seed allocation. Require current settings and catalog equality.
2. Run the whole family on **32 fresh seeds**, 672 fights. Freeze the three distinct reference compositions at **armor-and-health**, ranked by wins, remaining guardian health and cell ID. The first measured reference is the search benchmark.
3. Run **one** supported search: 528 search fights plus all five nominees on **128 fresh seeds**, 1,168 fights. Preserve both generated finalists and all three exact projected references regardless of the internal promotion verdict. Do not introduce a search policy, Essence permutation or identity optimization.
4. Preserve all 21 original recipes, both exact finalists, each finalist across all seven original gear profiles and all exact search references. Deduplicate only identical whole scenarios. Count actual compositions by per-slot Essence sets, ignoring metadata, gear and Essence order without rewriting recipes. Permit **at most 40 exact cells**: 21 originals + two exact finalists + fourteen gear projections + three references. This corrects the review's provisional 38-cell estimate before execution; exact nominees need not coincide with their gear projections. A larger actual import stops before evaluation and needs a new declaration.
5. Project expanded-screen time/bytes from the reference screen; require both within **80% of 840 native seconds / 2 GiB**. Then evaluate every expanded-family cell on **96 fresh seeds**. Eligibility requires **every cell at most 33/96 wins**, and **at least two distinct compositions with a cell at least 20/96 wins**. These fixed selection margins are not acceptance bounds. If ineligible, close this search scope without confirmation or guardian adjustment.
6. If eligible, independently project confirmation resources from that screen using **256/96**, with the same 80% admission margins. Freeze the entire family and run exactly **one fresh 256-seed confirmation**. Use approximate simultaneous 95% Bonferroni-Wilson bounds over every exact cell. Require **every upper bound at most 50% and at least two actual compositions with lower bounds at least 10%**. The ordinary one-composition `Pass` alone is insufficient. No pooling, retries, sample extensions, alternate settings or dropped cells.
7. If accepted, verify every confirmed input and one full report per exact cell against unchanged current content, using historical confirmation seeds only. Otherwise preserve the result without application or automatic retuning.

Maximum **15,920 study fights + 40 conditional confirmation replays + 23 qualification replays = 15,983 executions**; **621 fresh reservations**. Search phases keep the existing **840 native seconds / 900 process seconds / 2 GiB / 20,000-fight** ceiling. A phase stops on a technical failure; do not silently rerun it. No seeds are allocated to unreached conditional phases.

## Implementation and initial verification

The new `BalanceHarnessFloor10QualificationTests.cs` is a separate opt-in fixture; the historical application fixture and accepted archives remain unchanged. Seven ordinary checks reject changes to existing items, unknown additions, target-floor changes and shared Tower metadata while permitting the declared differences. `analysis/qualify-floor10-current-family.py` owns and independently verifies the qualification. Subsequent search phases use the existing bounded owner and supported search unchanged.

Build artifacts: `TestResults/tower-floor10-diversity-build-20260929`. The first build encountered denied access to the user NuGet configuration; the permitted build with that access succeeded. **89 backend tests passed**, with **four intentional opt-in skips**, through `build/run-tests.ps1`. The preserved receipt is `TestResults/tower-floor10-diversity-regression-20260929.trx`; build logs preserve both the initial access failure and successful build. Qualification, search and confirmation have their own explicitly enabled native receipts.

This declaration must be captured before execution. Results, immutable source pins, all process receipts and continuation state will be recorded below. Evidence under ignored `TestResults` must be preserved separately from a clean checkout.

## First scope: closed technical failure

The first qualification passed: all **5,376 input hashes**, **23 full reports** and **21 exact recipes** matched current content/runtime. Qualified manifest **`1b40e1b23a9378601116e7efa769397781fbc9ec9b8662ba3cf158b361e7117a`**, result **`f04c6f5e6fa71dd75fa0af9955456850fa487d39c9e534c93b82396c497bd0e1`**. The 32-seed reference screen completed **672 fights**. The subsequent search failed because `BalanceHarnessAffinityFloorEvaluationTests.VersionFor` excluded floor 10. It attempted **zero search fights**, but its **237 unused reservations remain excluded**. No expanded-family screen or confirmation ran. Total first-scope execution: **672 study fights + 23 qualification replays**, **269 fresh reservations**, exclusion union **908,701**. Guardian values and game content stayed unchanged.

Preserve `TestResults/tower-floor10-search-challenge-driver-20260929`, its failed search study/owner, `failure-closure.json`, all process receipts and `TestResults/tower-floor10-search-challenge-failed-20260929.trx`. Never resume or overwrite this scope. The qualification owner's `owner-source.py` retains its original implementation; the maintained helper subsequently requires an explicit initial exclusion count. This is recorded source drift, not a reason to repin the historical qualification.

## Separate corrected scope (declared before new qualification/combat)

Only the native projection allowlist changes to include floor 10. Two ordinary projection/preparation cases now exercise fifteen level-50, six-Essence characters with tier-2 Legendary/Masterpiece/rank-5 armor-and-health equipment, benchmarks 1 and 2, and combat disabled. Search policy, budgets, gameplay data and archived recipes remain unchanged. Fresh build `TestResults/tower-floor10-diversity-supported-build-20260929` passes **91 tests**, four intentional opt-in skips; receipt `TestResults/tower-floor10-diversity-supported-regression-20260929.trx`. The earlier build remains untouched.

This is a new `floor10-supported-search` scope with **908,701 initial exclusions**, fresh output paths and fresh reservations. Repeat the same 23-report/current-input qualification on the corrected build, using `tower-floor10-supported-qualification-{owner,study}-20260929`. Then execute the unchanged seven-step search/selection/acceptance protocol above from the original 21 recipes. The completed reference screen from the failed scope is not pooled or reused for this scope's ranking. Preserve both finalists, seven gear projections per finalist, and every exact projected reference; cap 40 cells. The same **15,920 study fights / 40 conditional replays / 23 qualification replays / 621 fresh reservations** ceilings apply separately. Stop without confirmation if the 96-seed screen misses the predeclared two-composition gate. No guardian adjustment or extra search is authorized within this declared scope.

Freeze this revised protocol and the corrected driver before execution; record results separately below. Keep the entire first scope, including its unused values, in the history union.

## Corrected scope result: two new candidates, no confirmation

**Closed `NoEligibleDiversityConfirmation`.** The corrected current-runtime qualification again matched **5,376 inputs and 23 full reports**, with zero fresh seeds. The supported search then completed successfully and found **two new actual compositions**, expanding the family from **21 to 38 exact recipes**, from **three to five compositions**. All original scenarios and all five exact search nominees remain present. Nineteen import-provenance entries produced seventeen added scenarios after exact-scenario deduplication; all seven gear profiles remain represented.

| Composition | Change from the alternating original | Search held-out panel | Complete-family screen |
| --- | --- | ---: | ---: |
| New `c01a33fe…` | Slot 13: Alpha Wolf + Transparent Slime → Venomous Snake + Viper | 43/128 | **37/96 (38.54%)** |
| New `52889519…` | Slot 3: Poisonous Rat + Transparent Slime → Venomous Spiderling + Viper | 44/128 | **28/96 (29.17%)** |
| Alternating original | Exact original and separately retained projected reference | 23/128 projected | 16/96 each |
| Repeated original | Exact original and separately retained projected reference | 11/128 projected | 10/96 each |
| Authored original | Exact original and separately retained projected reference | 0/128 projected | 0/96 each |

Every row above uses armor-and-health gear. The other **30 exact recipes won 0/96 each**. The new compositions each change two Essences on one character; permutations, gear variants and identity differences do not inflate composition counts. Both remain close to the existing poison-related lineup. Their marker presence and outcomes do not isolate damage causality or establish broad archetype diversity.

The search's internal paired validation returned `BenchmarkRetained` (11 gained / 6 lost validation wins). The subsequent 128-seed nominee panel is a separate observation and does not retroactively change that internal decision. Both finalists were retained and tested regardless.

The complete-family screen met the two-composition minimum of 20 wins but failed the **maximum 33/96** selection margin: the leader won 37. This is **not proof that its true win rate exceeds 50%**. It means the declared conservative gate did not admit a confirmation. No confirmation seeds, post-confirmation replays, automatic retuning or game-data edits followed. The earlier accepted 21-cell evidence still establishes one viable floor-10 composition; this scope adds two promising, unconfirmed candidates. The collector's zero new confirmed viable compositions is not a claim that the existing accepted composition ceased to qualify.

## Accounting and verification

| Corrected phase | Fights | Fresh reserved values | Native seconds |
| --- | ---: | ---: | ---: |
| Current-runtime qualification | 23 historical replays | 0 | separately bounded |
| Preparation | 0 | 0 | 1.179 |
| Original-family reference screen | 672 | 32 | 27.794 |
| Search + five nominees | 1,168 | 237 | 61.495 |
| Expanded-family screen | 3,648 | 96 | 127.965 |
| Confirmation / subsequent parity | 0 | 0 | not reached |

Corrected scope: **5,488 study fights + 23 qualification replays = 5,511 executions**, **365 reservations**, **218.432 native study seconds**. Together with the first closed failure: **6,160 study fights + 46 qualification replays = 6,206 executions**, **634 reservations**. Exclusion union advanced from **908,432 to 909,066**, including all 237 unused failed-search values. No statistical samples were pooled between phases or scopes.

All completed native phases passed through `build/run-tests.ps1` and their independent owners/audits. Processes drained with zero retries. The final reconciliation checked **680 immutable study inputs** and all 183 qualification pins before publication. Fresh verification passed **91 backend tests**, four intentional opt-in skips, **14 Python reference/retention tests**, and **11 selection/family/accounting checks**. The failed first search remains an explicitly preserved technical failure, not a missing check. The corrected build and scientific phases succeeded. No required command remains blocked.

Whole-Tower SHA remains **`0b52aedce19700d542dbcb9a0b2441d272f1181457642e03ba8c7a14e20f01fd`**. Floor 10 remains **health 12.71 / offense 7.13 / defense 2.29 / resistance 2.29 / penetration 1 / regeneration 1**. All other floors, the repeating gear curve, dungeon behavior, withdrawn supplies and live configuration remain unchanged. No migration, deployment or database operation is involved.

| Immutable artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor10-search-challenge-driver-20260929/failure-closure.json` | `6e35e61d41b81d5222d68286f6c61fe4b79d5328301967960c46cf39c29386b1` |
| `tower-floor10-supported-qualification-study-20260929/files.json` | `149389ea1d8b2d36d3c449a7b301da2238b71ec950e75a0eae972b14b902d071` |
| `tower-floor10-supported-search-evidence-20260929.json` | `6a9c26622e9951d2c2969faa57e434f93f8f2c8572e1a4af926463d2053d1a4d` |
| `tower-balance-pass-floor10-supported-search-expanded-screen-study-20260929/files.json` | `75cbba8df703ec658527834ece8db87da4b3feb7b24c8bdca8dd049d6a393786` |
| `tower-balance-pass-floor10-supported-search-expanded-screen-study-20260929/result.json` | `c06a2ddd408e491885f9bc85b9e90eda44868ed8c4d9766488a0715c0d158f60` |
| `tower-balance-pass-floor10-supported-search-expanded-screen-owner-20260929/independent-audit.json` | `e8576178026c05395b36aeb89ba04014a7197d4ac955bf23a818f1d9dd8218f9` |
| `tower-balance-pass-floor10-supported-search-expanded-screen-owner-20260929/seed-ledger.json` | `682a024c378f41572b4436fd74845e18652832c2eb6413b63a8fb39c0aa9f0ab` |
| `tower-floor10-supported-search-driver-20260929/protocol.md` | `3ecdf45dce0e143ad9701bc793eb1e9121a225e3d2362b12c3d77092b685fa90` |
| `tower-floor10-supported-search-composition-differences-20260929.json` | `1b8550ca70e77c718c9fc7f1fc1a005d711d37db388a2d82858cc1f1b428040b` |

The corrected driver SHA is `bc342f700568ddcf8780105d1f9f3f10b5b0ba70b7e332cda14b1ea7545d5a61`. Appending these results changes the maintained report relative to its frozen `protocol.md`; preserve that snapshot and its pin. Do not repin the completed qualification to the published report or rerun any completed output path. Evidence is local under ignored `TestResults` and needs separate preservation outside a clean checkout.

Commands used (historical record; completed output paths refuse reuse):

```powershell
$filter = 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests|FullyQualifiedName~BalanceHarnessFloor10QualificationTests'
./build/run-tests.ps1 -ArtifactsPath TestResults/tower-floor10-diversity-supported-build-20260929 -Filter $filter
python -B -X utf8 'Balance Harness/analysis/qualify-floor10-current-family.py' --artifacts TestResults/tower-floor10-diversity-supported-build-20260929 --owner TestResults/tower-floor10-supported-qualification-owner-20260929 --output TestResults/tower-floor10-supported-qualification-study-20260929 --protocol 'Balance Harness/Tower-Floor10-Search-Challenge-20260929.md' --initial-exclusions 908701
python -B -X utf8 'Balance Harness/analysis/test-tower-reference-coverage.py'
python -B -X utf8 TestResults/tower-floor10-supported-search-checks-20260929.py
python -B -X utf8 TestResults/tower-floor10-supported-search-driver-20260929.py
python -B -X utf8 TestResults/tower-floor10-supported-search-collect-20260929.py
git -c core.safecrlf=false diff --check
```

Maintained work comprises the floor-10 qualification fixture/owner, the native projection allowlist and two preparation cases, this report, the handoff and guide notices. No search algorithm or gameplay rule changed.

## Original next-step recommendation (completed by separate calibration)

Use the **38-cell expanded-screen snapshot** as the complete candidate family for a separately declared **floor-10 health calibration**, retaining all five compositions, seven gear profiles and exact nominees. Keep offense, defense, resistance, penetration, regeneration and the party budget fixed. A small prospective health-increase grid can seek a setting with more margin for the strongest candidate while preserving the second candidate's viability. Freeze the grid, selection/stability rules, limits and fresh confirmation before allocation; only a full-family confirmation with every adjusted upper bound at most 50% and at least two actual compositions above the 10% lower bound can support acceptance.

Do not resume either closed scope, repeat the search, drop old recipes, pool these observations, import the same finalists again or call the 38-cell screen a confirmed family. The historical 21-cell confirmation remains the accepted floor-10 strength source until superseded. Floor-8 pacing still requires an approved duration target; broader archetype/gear coverage and ordinary acquisition remain separate questions. No active study or additional reservation remains.

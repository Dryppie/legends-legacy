# Floor 3: broader viable compositions

**Later continuation:** the [floor-5 diversity sweep](Tower-Floor5-Diversity-20260929.md) is closed after 61,440 fights with no eligible setting or content change. The floor-3 result below remains applied. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for the 902,402-value exclusion union and the next bounded challenge with the existing search; the earlier next-floor recommendation below is historical.

**Applied locally and verified:** floor-3 health is now **1.5047963378**, a **2.25% reduction**. Offense remains **6.7842480469**; all other guardian fields and floors are unchanged. The fresh 38-cell confirmation establishes **two distinct viable compositions**, with best results **90/256 and 56/256**, while every adjusted upper bound stays below 50%. All **9,728 inputs and 38 complete replays** match. These are closely related poison teams, not broad archetype diversity.

## Prospective scope

The user authorized the next Tower-balancing pass after the complete reference-coverage verification. Target **Morrowmaw on floor 3**, retaining all **38 exact recipes / five distinct Essence compositions** from the latest confirmation. These are five level-30 characters, four unascended/unevolved Essences each, tier-1 Rare / Standard / rank-2 gear, fixed positions, identity fields and canonical Essence order. Ownership remains hypothetical. No new teams, search algorithm, gear budgets, rewards, dungeon work or other floors enter this pass.

The baseline has health **1.5394335937**, offense **6.7842480469**. Its strongest composition won 55/256; the next distinct composition leaders won 25/256 and 24/256. Only one composition established the 10% viability threshold. Earlier discovery at roughly 1.5% lower health and offense suggested more viable routes, so this pass tests small scalar changes rather than replacing mechanics.

Source: `TestResults/tower-balance-pass-floor3-reference-coverage-confirmation-study-20260929`, manifest SHA-256 `eac253fb22e26ab551215a04cee0dd9b43d683cf530e4daf02619032880648ad`. Initial Tower SHA-256: `87a374fb1ff1ae0a822d055c661527e5fb479ec82f54aa46f5fab4022b337c9e`. Initial exclusion union: **899,778**, anchored by the floor-9 reference-coverage confirmation ledger, SHA-256 `015a47dbab651a6e45ccea735d084ac7752f0810b1d7e9537df8c770ac17d4c4`. Preserve all linked and subsequent ledgers, including unused reservations.

## Frozen protocol

Write this declaration and the driver before the first new fight. Use the existing bounded Tower owner and compiled `TestResults/tower-reference-coverage-build-20260929` runtime, after checking current source/runtime pins. Every phase uses a fresh output path and disjoint seeds. No pooling, extension, retry, recipe removal or identity/Essence-order optimization is allowed.

1. Authenticate the source and prepare all 38 saved recipes against current content with zero fights. Require native settings, target-floor and non-Tower catalog parity.
2. Run these **nine fixed screens**, in order, with **64 fresh seeds per cell**. Factors always multiply the starting baseline, not a preceding candidate:

   | Label | Health factor | Offense factor |
   | --- | ---: | ---: |
   | baseline | 1 | 1 |
   | health-015 | 0.985 | 1 |
   | health-030 | 0.97 | 1 |
   | offense-015 | 1 | 0.985 |
   | offense-030 | 1 | 0.97 |
   | joint-015-015 | 0.985 | 0.985 |
   | joint-030-015 | 0.97 | 0.985 |
   | joint-015-030 | 0.985 | 0.97 |
   | joint-030-030 | 0.97 | 0.97 |

3. Group cells by their actual per-slot Essence vectors. Each composition's leader is its highest-win cell; original/projected representations and gear variants do not create extra compositions. The baseline is diagnostic and cannot be selected for application.
4. A nonbaseline screen qualifies for a fresh stability panel if every cell has at most **28/64 wins**, and at least two distinct composition leaders have at least **12/64 wins**. Rank qualifying settings by: most such compositions, highest second composition win count, strongest cell closest to 30%, smallest sum of absolute scalar changes, then label. Freeze the first **up to three** settings; if none qualify, stop unapplied.
5. Run every frozen stability setting once with **128 fresh seeds per cell**, using exactly its captured content and family. Do not substitute a lower-ranked screen if a stability result disappoints. A setting qualifies for confirmation if every cell has at most **48/128 wins**, and at least two distinct composition leaders have at least **24/128 wins**. Rank by the same rule using the stability thresholds, freeze the first setting, and stop unapplied if none qualify. These screening/stability thresholds are selection margins, not statistical acceptance.
6. Before confirmation, project duration and storage at twice the selected stability phase. Require both within 80% of the existing **840-second native / 900-second process / 2-GiB** envelope. A failed preflight stops before reserving a confirmation panel.
7. Run **one fresh 256-seed confirmation of all 38 cells**. Independently recount every saved battle and compute the existing approximate simultaneous 95% Bonferroni-Wilson bounds over the complete 38-cell family. Require **every upper bound at most 50% and at least two distinct compositions with a cell whose lower bound is at least 10%**. A standard one-composition pass alone is insufficient. Preserve a failed/inconclusive diversity result without another confirmation or application.
8. Only after both acceptance conditions pass, apply the confirmed floor-3 health/offense values locally. Require the whole Tower JSON to match the confirmed snapshot after substituting only those two scalars. Verify all **9,728 native inputs and 38 full report replays** against the applied file, then run relevant regressions through `build/run-tests.ps1`. No deployment, API startup or database operation is authorized or required.

Maximum combat: **21,888 grid fights + 14,592 stability fights + 9,728 confirmation fights + 38 application replays = 46,246 fights**. Maximum new reservations: **1,216**. Run phases sequentially; technical failures stop dependent work. The normal maximum of 20,000 fights per native phase remains unchanged. Prepare, screen, validate and confirm before editing game content.

## Result

The first bounded pass completed **36,480 fights** in 13 phases and stopped **NoStableCandidate**, with no confirmation panel allocated and no guardian edit. Its three fresh 128-seed stability panels were:

| Setting | Composition leader wins, out of 128 | Decision |
| --- | --- | --- |
| Health −1.5%; offense unchanged | 34, 23, 22, 0, 0 | Second composition misses the 24-win selection margin |
| Health −1.5%; offense −3% | 62, 36, 30, 0, 0 | Strongest cell exceeds the 48-win selection ceiling |
| Health −3%; offense unchanged | 50, 37, 22, 0, 0 | Strongest cell exceeds the selection ceiling by two wins |

These are selection outcomes, not confirmation failures or new acceptance claims. Preserve all nine grid screens, all three stability panels and **960 new reservations**, giving an exclusion union of **900,738**. The initial driver completed; its scope is closed without retry, extension or application. The prospective source remains copied under `TestResults/tower-floor3-diversity-driver-20260929/protocol.md`.

## Separately declared health refinement

The completed health-only panels bracket a useful smaller change: −1.5% narrowly missed the second-composition margin, while −3% narrowly exceeded the strongest-team margin. The user has authorized continuing the floor-3 balancing work. This separate, bounded refinement narrows that bracket without changing the original decisions, reusing their samples or weakening their selection rules. Declare this section and its new driver before its first battle.

- Reuse the authenticated 38-cell seed-free preparation from the first pass. Current Tower content and runtime must still match its entry pins. Starting union: **900,738**; no confirmation seeds were reserved by the first pass.
- Test exactly three settings, each against the original baseline, with **128 fresh seeds per cell**: health factors **0.98, 0.9775 and 0.975**, offense factor **1** throughout. Complete all three panels; no substitution or further grid expansion belongs to this scope.
- Use the same stability gate: maximum **48/128 wins** across all cells, and at least two distinct composition leaders at **24/128 or higher**. Rank qualifying settings by the original deterministic rule. If none qualify, close unapplied; do not broaden the grid under this declaration.
- Freeze the first qualifying setting, pass the same 80% resource preflight, and run **one fresh 256-seed confirmation of all 38 cells**. Keep the original simultaneous bounds and two-distinct-composition acceptance criterion. No confirmation retry, pooling, extension, cell dropping or second confirmation is allowed.
- Apply only if both acceptance conditions pass, verify all 9,728 input hashes and 38 complete replays, and run the relevant backend regressions. Only floor-3 health may change; offense and every other field/floor must remain unchanged.

This refinement allows at most **14,592 screening fights + 9,728 confirmation fights + 38 replays = 24,358 fights**, and **640 new reservations**. Combined with the closed initial pass, the maximum is **60,838 fights / 1,600 new reservations**. The existing per-phase deadlines, storage limits and no-deployment rules remain unchanged. Its fresh owner paths include `floor3-diversity-refinement`; the completed first driver and archives are not overwritten or resumed.

## Refinement result and application

All three fresh refinement panels completed before selection:

| Health reduction | Composition leader wins, out of 128 | Decision |
| --- | --- | --- |
| 2% | 53, 28, 22, 0, 0 | Exceeds the strongest-team selection ceiling |
| **2.25%** | **41, 28, 26, 0, 0** | **Only eligible setting; frozen for confirmation** |
| 2.5% | 50, 23, 19, 0, 0 | Misses both selection margins |

The selected setting passed the existing resource preflight, projecting **124.50 native seconds / 154,108,870 bytes** for confirmation. The single fresh confirmation completed all **9,728 fights** and passed both the ordinary ceiling rule and the stricter two-composition diversity requirement. No other confirmation ran in either scope.

| Confirmed composition and gear | Wins | Adjusted interval | Mean engine seconds |
| --- | ---: | ---: | ---: |
| Generated variant B (`ec28b715…`), health-and-regeneration | 90/256 (35.16%) | 26.31–45.15% | 73.33 |
| Generated variant A (`170058c2…`), health-and-regeneration | 56/256 (21.88%) | 14.75–31.18% | 73.58 |
| Variant B, resistance-and-health | 50/256 (19.53%) | 12.82–28.60% | 72.64 |

All intervals use the complete 38-cell family. The last row is another gear option for variant B, so **three viable cells represent two compositions**. The retained poison composition won 36/256 and did not establish the 10% lower bound. The other two compositions had zero wins across all their evaluated gear profiles. The earlier and current rates are separate observations; no samples were pooled and no paired improvement claim is made.

The two confirmed teams differ by **one Essence on party slot 3**: Spider Queen's Royal Venom in variant B versus Venomous Snake in variant A. Their other four characters and other three slot-3 Essences match. This provides a modest alternative within a poison-focused team. It does not establish viability for unrelated archetypes, and permutations were never searched.

Only the floor-3 `guardianScaling.health` value changed in [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json): **1.5394335937 → 1.5047963378**. The application helper required the confirmed whole-Tower snapshot to differ solely in that health value, then matched **9,728 native input hashes and 38 complete report replays**. Final Tower SHA-256: **`9487aa92e78bb603f571a728aa43713647e11384f4463f95979a6694bca701b5`**. Application result SHA-256: `c0ce93b32b46541d4a6e56c1bc5c41d49d96911434749be8d7116593b3c0c687`.

## Evidence, verification and continuation

The two scopes completed **17 phases / 60,800 study fights**, plus **38 application replays**, for **60,838 total executions**. Native study execution totaled 799.74 seconds, excluding owner audits and application work. Every native phase completed without retry and every bounded process drained. The original `NoStableCandidate` scope remains closed and unapplied; the separately declared refinement supplied the only confirmation and application.

- [Evidence index](../TestResults/tower-floor3-diversity-evidence-20260929.json): SHA-256 **`19cbc7124d8fd2760a81cd597b118a759c94e05ccbf29b5d24586bef6e321d87`**. It pins both scopes, verifies every recipe unchanged, checks **1,062 immutable input/source/runtime pins**, and proves that only the authorized floor-3 health field changed.
- [Accepted confirmation](../TestResults/tower-balance-pass-floor3-diversity-refinement-confirmation-study-20260929/files.json): manifest SHA-256 **`8fe7e0cd82b13d34b4cfebc296117621281dee98393f5fa22b0a7f95cba1b561`**. Its owner directory contains the independent battle recount and 38-cell interval assessment; the refinement driver directory contains the additional distinct-composition acceptance assessment.
- Latest exclusion union: **901,378**, adding **1,600 disjoint reservations** across both scopes. [Latest ledger](../TestResults/tower-balance-pass-floor3-diversity-refinement-confirmation-owner-20260929/seed-ledger.json) SHA-256: **`7d6bedc984eebb48c7281088befa00cd8fb6120ee1f8e5cfb064c33953e36418`**. Preserve every linked ledger, including the first pass's 960 values and all older unused reservations.
- **82 backend regressions passed**, with three intentional opt-in skips. All 17 owned phases and the separate application check also passed their native execution tests. Five seed-free grouping/ranking checks verified that duplicate representations do not inflate the composition count and that selection follows the declared order. The [final TRX](../TestResults/tower-floor3-diversity-final-regression-20260929.trx) is retained separately from the wrapper's shared output.

The existing compiled runtime was reused after verifying its captured hashes and checking that no backend C# source was newer; this pass changed content, not backend code. The final regression command was:

```powershell
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
git -c core.safecrlf=false diff --check
```

Execution helpers, immutable declarations, original protocol copies, logs and receipts are under `TestResults/tower-floor3-diversity-*20260929*`; preserve them rather than rerunning or overwriting completed paths. `TestResults` is ignored local evidence and must be available to reproduce this chain. No active study remains. No required verification command was blocked. No migrations, new configuration keys, API startup, database changes or deployment were performed; the guardian data change is local and would require the normal content rollout to reach players.

**Next:** continue the diversity queue on **floor 5**, retaining its complete 60-cell family and its explicit five-Essence Epic/Fine/rank-3 budget. Floors **5, 7, 8, 12 and 13** still have only one clearly viable composition. Keep floor-3 archetype diversity as a remaining limitation rather than calling the floor fully balanced. Review floor-8 pacing separately (about 196 engine seconds), with floor-14 and floor-5 durations also recorded. Keep the supported search, repeating equipment curve and later-floor carried-Legendary assumptions unchanged; dungeon work and replacement supplies are not prerequisites.

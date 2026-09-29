# Tower floors 1–9: complete reference coverage

**Later floor-8 result:** the [midpoint refinement](Tower-Floor8-Health-Refinement-20260929.md) applies health **9.9064526367 (+5%)**, preserving offense and regeneration. The complete 67-cell confirmation establishes **47/160 (29.38%)** and **33/160 (20.62%)**; all 10,720 inputs and 67 full replays match. The report below retains its own historical setting and results. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **906,552 exclusions**, current content and the floor-12 queue.

**Later floor-7 result:** the [expanded-family refinement](Tower-Floor7-Diversity-Refinement-20260929.md) applies health **4.4454238281 (+5.5%)**, with offense and regeneration unchanged. Fresh confirmation across **60 exact recipes / eight compositions** establishes two new lineups at **95/256 and 82/256**; 15,360 inputs and 60 full replays match. The results below remain historical at their stated settings. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **905,278 exclusions**, the current Tower hash and the floor-8 queue.

**Latest floor-5 calibration:** the [expanded 89-cell family](Tower-Floor5-Expanded-Calibration-20260929.md) passed fresh confirmation at health **3.3102803755 (+4%)**, with offense unchanged. Two lineups qualify at 70/200 and 43/200. The table below remains the earlier coverage result at its prior setting. The [current handoff](Tower-Continuation-Handoff-20260928.md) records all 903,652 exclusions and the floor-7 queue.

**Latest floor-5 family:** the [supported-search challenge](Tower-Floor5-Search-Challenge-20260929.md) retains all 60 floor-5 recipes and adds 29 exact scenarios, including four new compositions. Its strongest fresh screen result is 80/128; no new acceptance or guardian change followed. The [current handoff](Tower-Continuation-Handoff-20260928.md) records all 903,004 exclusions and the next complete-family calibration.

**Later floor-5 result:** the [diversity sweep](Tower-Floor5-Diversity-20260929.md) retained all 60 floor-5 recipes across 61,440 fights and closed without an eligible setting. No floor-5 value changed. The [current handoff](Tower-Continuation-Handoff-20260928.md) records all 902,402 exclusions and the next bounded challenge with the existing search.

**Later floor-3 update:** the [diversity pass](Tower-Floor3-Diversity-20260929.md) reduced floor-3 health by 2.25% and independently confirmed two closely related poison compositions at 90/256 and 56/256. Its 9,728 inputs and 38 full replays matched. The table below preserves the earlier reference-coverage observations at the preceding setting; use the [current handoff](Tower-Continuation-Handoff-20260928.md) for the latest Tower hash, exclusions and floor-5 queue.

## Prospective scope

This pass closes the exact-recipe coverage gap identified after the floors-12–15 calibration. It evaluates the **currently applied guardian settings**, retaining each earlier confirmed family unchanged and adding all exact projected reference recipes from the corresponding completed searches. The supported affinity search, Essence order, equipment budgets and game content are unchanged. Ownership remains hypothetical.

The read-only audit is `TestResults/tower-earlier-reference-coverage-audit-complete-20260929.json`, SHA-256 `1297cd6fe3041b98b9a68fc311fcffe0c904ebbfc883211d29349bc9ec46ebc6`. It found 30 missing entries across ten searches. Two floor-5 entries repeat identical scenarios from another search: retain both provenance links but fight each identical scenario once. This adds **28 exact recipes** to the **336 previously confirmed cells**: **38 cells on each floor except floor 5, which has 60**, for **364 cells** total. Exact native identities are preserved; distinct Essence compositions are counted separately from representation and gear variants.

Starting Tower file SHA-256: `87a374fb1ff1ae0a822d055c661527e5fb479ec82f54aa46f5fab4022b337c9e`. Starting exclusion union: **897,186**, including all earlier failed, unused and superseded reservations. The entry ledger is `TestResults/tower-balance-pass-floor15-later-complete-large-confirmation-owner-20260929/seed-ledger.json`, SHA-256 `742287ea06eb77aad7b84ee1676be31dc76d34caa7f81bf25a7be16cfdb30db5`.

## Frozen execution rule

1. Authenticate each earlier confirmation and search archive. Import the saved cells without regenerating, reordering or rebudgeting them. Add only the three saved search references, deduplicating identical complete scenarios and retaining every provenance link.
2. Prepare the complete family without combat against current content. Require the target floor and every non-Tower catalog to match the earlier confirmation. Other floors may reflect subsequent applied calibration. Compare native combat settings after preparation, before any fight.
3. Run one **32-seed screen** per complete floor family at the applied settings. It estimates resources and describes the newly included recipes; it does not select a new setting or change the family.
4. Before confirmation, require projected duration and storage (eight times the screen) to fit 80% of the existing **840-second native / 900-second process / 2-GiB** envelope. Preserve a failed preflight without allocating a confirmation panel; proceed to independent floors.
5. For each admitted floor, run **one fresh 256-seed confirmation of every cell**, regardless of screening win rates. No pooling, sample extension, seed reuse, retries, recipe dropping or guardian tuning occurs in this pass. A failed or inconclusive result is a finding, not permission to repeat confirmation.
6. Independently recount every saved battle. Use the existing approximate simultaneous 95% Bonferroni-Wilson bounds within each complete floor family: every upper bound at most 50%, and at least one lower bound at least 10%. These are per-floor claims, not a joint nine-floor or universal-build guarantee.
7. For each passing confirmation, verify every input against the applied content and replay one complete report per cell using its original seed. Replays allocate no new seeds. Preserve every phase, process receipt, source hash, audit and reservation.

Maximum scheduled combat is **11,648 screening fights + 93,184 confirmation fights + 364 application replays = 105,196 fights**, with **2,592 new reserved seeds**. Run floors sequentially to preserve seed-history exclusion and avoid shared wrapper output races. A technical failure stops dependent work; do not silently resume or overwrite a phase.

The implementation is confined to the offline Tower harness and its tests. No migrations, economy changes, replacement supplies, deployment or database access are part of this pass. Current-content preparation captures the whole current Tower file so later application checks can verify it exactly.

## Execution and findings

**Completed: all nine expanded families pass at the applied settings. No guardian values changed.** Every confirmation completed its fresh 256-seed panel, and every current-content input and full replay matched. No phase failed, exceeded its resource preflight, retried, extended its sample or dropped a recipe.

| Floor | Strongest fresh result | Adjusted interval | Strongest added reference | Distinct viable compositions | Strongest team's mean engine seconds |
| ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 78/256 (30.47%) | 22.13–40.32% | 15/256 | 2 | 77.42 |
| 2 | 74/256 (28.91%) | 20.76–38.68% | 49/256 | 3 | 68.14 |
| 3 | 55/256 (21.48%) | 14.43–30.75% | 24/256 | 1 | 74.54 |
| 4 | 69/256 (26.95%) | 19.07–36.63% | 17/256 | 2 | 42.10 |
| 5 | 55/256 (21.48%) | 14.20–31.16% | 10/256 | 1 | 126.19 |
| 6 | 91/256 (35.55%) | 26.67–45.55% | 19/256 | 2 | 78.19 |
| 7 | 46/256 (17.97%) | 11.55–26.87% | 0/256 | 1 | 69.65 |
| 8 | 95/256 (37.11%) | 28.09–47.13% | 0/256 | 1 | 195.53 |
| 9 | 58/256 (22.66%) | 15.41–32.02% | 47/256 | 2 | 62.53 |

The intervals account for all 38 cells per floor, or all 60 on floor 5. Every cell's upper bound is below 50%; the table displays the strongest cell's interval. A viable composition has at least one cell with a lower bound at or above 10%. Count actual per-slot Essence vectors once even when multiple gear profiles or original/projected representations qualify. The strongest result on every floor remains a generated finalist already retained in the earlier family. These fresh rates are independent observations, not changes to guardian strength or pooled updates to the historical confirmations.

Floor 1 has viable armor-and-health, health-and-regeneration and resistance-and-health profiles. Every other floor's viable cells share one profile: armor-and-health on 2/4/8, health-and-regeneration on 3, resistance-and-health on 5/7/9, and restorer specialization on 6. The three added references on floors 7 and 8 all won zero. The earlier narrow-composition concern therefore remains despite closing the missing-reference gap.

## Evidence and continuation state

- **27 completed phases:** nine seed-free preparations, nine 32-seed screens and nine 256-seed confirmations. The screens used **11,648 fights**, the confirmations **93,184**, and application checks **364 complete replays**, totaling **105,196 actual fights**. Native study execution totaled 1,887.59 seconds; this excludes Python audits and application work. Every bounded native process drained.
- All **93,184 confirmation input hashes** and **364 complete battle reports** matched the applied content. The collector also checked every existing family as an unchanged prefix and every added scenario against its saved search source, including all 30 provenance entries.
- The [evidence index](../TestResults/tower-reference-coverage-evidence-20260929.json) pins every phase manifest, result audit and application receipt. SHA-256: `4678a51dfe8de6b2bc87bb34c4a6261fb20441f59d6b35696dd5b9e390568a3c`. Final confirmation paths are `TestResults/tower-balance-pass-floor{1..9}-reference-coverage-confirmation-study-20260929`; corresponding `owner` directories contain independent audits and seed ledgers. Application receipts are in `TestResults/tower-floor{1..9}-reference-coverage-application-owner-20260929`.
- Final exclusion union: **899,778**, adding 2,592 disjoint reserved values. Latest [seed ledger](../TestResults/tower-balance-pass-floor9-reference-coverage-confirmation-owner-20260929/seed-ledger.json) SHA-256: `015a47dbab651a6e45ccea735d084ac7752f0810b1d7e9537df8c770ac17d4c4`. Import all linked history and later balance-pass ledgers, preserving unused and failed reservations.
- The whole Tower file remains SHA-256 `87a374fb1ff1ae0a822d055c661527e5fb479ec82f54aa46f5fab4022b337c9e`. No gameplay code, equipment curve, budgets, configuration, migrations or deployment changed in this pass. The existing local guardian edits from earlier calibration remain intact.

`TestResults` is ignored local evidence; preserve it and verify availability in another checkout. The completed driver must not be rerun into its existing paths. There is no active study left from this pass.

## Next balancing decision

Keep the current guardian settings and the supported search. The known reference-coverage gap is closed for these declared families; another search-algorithm campaign is not the next priority.

**Completed follow-up:** the separately declared [floor-3 diversity pass](Tower-Floor3-Diversity-20260929.md) retained this complete 38-cell family, tested health and offense independently, and applied only a freshly confirmed 2.25% health reduction. Two closely related compositions now qualify; broad archetype diversity remains unestablished. The next bounded diversity pass starts on **floor 5**, retaining its complete 60-cell family. Never reuse this report's seeds or historical confirmation as acceptance for a new setting.

Then address the similarly narrow floors **5, 7, 8, 12 and 13**. Review floor-8 pacing separately: its strongest team averages **195.53 engine seconds**, versus roughly 132 on floor 14 and 126 on floor 5. No gameplay duration threshold has been approved, so the duration observations do not constitute a pacing pass/fail result. The later floors retain their own confirmed budgets and carried-Legendary assumptions. These fixed, fully owned party budgets do not establish ordinary-player acquisition or win rates; dungeon work and replacement supplies are not prerequisites for this balancing queue.

## Implementation

- `analysis/run-tower-balance-pass.py` adds a reference-only import option, `--add-references`, and seed-free `--current-content` preparation from a saved family. Every reference is copied as saved, including its scenario and native identity fields. Only identical complete scenarios merge; all source links remain in `reference-coverage.json`. Existing cells remain unchanged.
- `BalanceHarnessTowerBalancePassTests.cs` allows seed-free native preparation to load imported cells. Ordinary preparation still constructs its existing family when no cell file is supplied. Current-content refresh checks target-floor/catalog equality before preparation and native settings equality afterward, before any combat.
- `analysis/test-tower-reference-coverage.py` checks identity preservation, duplicate provenance, incomplete and invalid reference families, and the distinction between refreshing other floors and changing the target floor or catalogs.

No new generated teams, search policy, Essence permutations or identity optimization are introduced. A saved identity variant counts as a recipe representation; distinct composition counts use the actual per-slot Essence vectors.

The local execution driver is `TestResults/tower-reference-coverage-driver-20260929.py`; its immutable declaration and original prospective report are in `TestResults/tower-reference-coverage-driver-20260929/`. The collector is `TestResults/tower-reference-coverage-collect-20260929.py`. These helpers and their completed output paths are execution archives, not resumable commands. Choose fresh names and a new declared scope for any later work.

## Verification

The initial build passed 35 focused backend regressions, with three intentional opt-in skips. Final verification passed **82 backend regressions**, with the same three skips; the 27 study phases and nine application checks each passed their explicitly invoked native test separately. **Six Python import safeguards passed.** No required verification command was blocked. No API startup, database operation or deployment ran.

Commands used from the repository root (`python` denotes the bundled runtime):

```powershell
python -B -X utf8 'Balance Harness/analysis/test-tower-reference-coverage.py'
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~TowerProgressionEquipment'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
git -c core.safecrlf=false diff --check
```

The broader final filter names the actual progression-equipment, gear-profile and discovery-contract test classes. Build and final logs are `TestResults/tower-reference-coverage-build-20260929.log` and `TestResults/tower-reference-coverage-final-regression-20260929.log`. The final [TRX](../TestResults/tower-reference-coverage-final-regression-20260929.trx) is retained separately from the wrapper's shared output. The final source/runtime checks use `TestResults/tower-reference-coverage-final-checks-20260929.py` and write a new receipt; do not overwrite completed receipts.

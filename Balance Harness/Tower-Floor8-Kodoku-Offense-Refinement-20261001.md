# Floor 8: Kodoku 0.575 offense diagnostic — 1 October 2026

**Latest floor-8 result (1 October):** The [complete fixed 0.575 screen](Tower-Floor8-Kodoku-Fixed-Acceptance-20261001.md) closed **not accepted** after **95,232 fresh fights / 512 seeds per recipe**. Three eligible compositions passed viability: **254/512, 175/512 and 162/512**. Route A alone failed the ceiling (**57.57% adjusted upper bound; 50% required**). **No confirmation, local application or game catalog change occurred.** Next is one separately declared offense diagnostic inside the existing **0.575–0.600** range, keeping the complete family and all acceptance rules. No new coefficient or seeds have been allocated. The aggregate/application safeguards are implemented and verified: **342 Python checks and 368 backend cases pass; four intentional backend skips**. Final seed exclusions: **924,780**. Floor 8 remains unresolved; floors 8–10 and 12–15 equipment checks and the final current-version 1–15 sweep remain.

The fixed **0.575 offense** candidate is promising enough for formal acceptance testing. The three eight-item parties won **12/32, 12/32 and 13/32** (**37.5%, 37.5% and 40.6%**) across the complete 186-recipe diagnostic. Their twelve-item controls won **1/32, 2/32 and 0/32**; baseline and one-healer controls won zero. No other retained recipe exceeded **2/32**. Floor 8 remains unaccepted and unapplied.

Target: primary LL World Tower and offline Balance Harness. Continue the [0.60 offense diagnostic](Tower-Floor8-Kodoku-Offense-Diagnostic-20261001.md), which recorded 6/16, 1/16 and 1/16 wins for the three eight-item parties. It remains unaccepted and unapplied.

## Frozen protocol

Authenticate publication `836cd26bc3c9d96e11bb784f42f7deca08ca76e8d102e378fa61bc6f44cc34ad`, preserve source/document snapshots and all **924,236** excluded seeds. Use the exact unallocated `TestResults/tower-floor8-kodoku-offense-refinement-proposal-20261001.json`.

Keep all **186 recipes / nine actual compositions / 128 equipment-eligible recipes**. Retain raw Essence order, native identities, positions, progression and gear, including the three eight-item parties, every twelve-item Restoration setup, all one-healer variants and every armor control. Eligibility remains at most eight specialized items across two characters.

Implement the separate strict `tower-kodoku-eight-item-pressure-refinement-v1` candidate. Set guardian offense to **5.0749645996**, using factor **0.575 from the original source**. This is 4.17% lower than the isolated 0.60 candidate and 9.52% higher than 0.525. Preserve Health, penetration ×40, exclusive Venomspawn ArmorPenetration inheritance, both original Miasma effects and all unrelated content. Do not compound from another candidate. Both preceding fixed-factor contracts remain strict and closed.

Run the Python safeguard suites and backend tests through `build/run-tests.ps1`. Verify native guardian/summon Power scaling, preserved summon Health and armor mitigation. Prepare all 186 recipes under both original and candidate catalogs with zero combat. Original participants must match the preceding projection; only guardian Power and the declared penetration attributes may differ from the original, and only Power may differ from 0.60. Verify fresh/bound projections against the unchanged qualified production assemblies. Reuse the authenticated original-catalog preparation archive.

Freeze a separate `floor8-kodoku-offense-refinement-diagnostic-v1` declaration: **two complete sixteen-seed panels**, **2,976 fights each / 5,952 fights and 32 fresh reservations maximum**. Mark every outcome diagnostic-only and excluded from acceptance. Ordinary screen/search/confirm guards remain 16–512 seeds; the two older eight-seed diagnostics retain their exact contracts. No interim selection, extension, retry, replacement, extra coefficient, confirmation or application. Stop after both panels or any failure and preserve reservations/evidence. No historical outcomes transfer.

For the first panel, use completed 0.60 diagnostic panel 2 as the resource reference. Double its measured time and archive size per fight for 2,976 fights: the proposal projects **391.79 seconds**. Before panel 2, remeasure from completed panel 1. Require under **672 seconds** and 80% of 2 GiB, plus free disk for all remaining doubled archive estimates and 2 GiB. Preserve **840-second native / 900-second owner / 2 GiB archive** limits. Observe supervisor stdout only during native execution; leave active study, owner and control files unopened.

After both panels close, independently authenticate every archive member, recount all outcomes, verify each native participant against the prepared projection, and reconcile catalogs, settings, runtime and the exact seed union. Compare all three eight-item parties with their baseline, one-healer and twelve-item controls on the combined **32-seed** schedule. Retain all other controls. These descriptive counts cannot establish balance: later acceptance requires a fresh complete family-adjusted screen, at least two actual equipment-eligible compositions with lower bounds ≥10%, every recipe's upper bound ≤50%, then independent confirmation.

Update this report and current Tower/harness documents. No live game catalog change, migration, configuration change, database action, deployment, dungeon/acquisition work or search redesign is included.

## Completed result

The fixed **0.575 offense** candidate is promising enough for formal acceptance testing. The three eight-item parties won **12/32, 12/32 and 13/32** (**37.5%, 37.5% and 40.6%**) across the complete 186-recipe diagnostic. Their twelve-item controls won **1/32, 2/32 and 0/32**; baseline and one-healer controls won zero. No other retained recipe exceeded **2/32**. Floor 8 remains unaccepted and unapplied.

Both complete panels retained **186 recipes / nine actual compositions / 128 equipment-eligible recipes**. Total: **5,952 fresh diagnostic fights / 32 new reservations**, sixteen seeds per panel, with no interim statistical selection. The table combines only these two predeclared panels; no prior outcomes contribute.

| Composition | Baseline | Six items on slot 2 | Six items on slot 7 | Eight items across both healers | Twelve items across both healers |
| --- | ---: | ---: | ---: | ---: | ---: |
| A | 0/32 | 0/32 | 0/32 | 12/32 | 1/32 |
| B | 0/32 | 0/32 | 0/32 | 12/32 | 2/32 |
| C | 0/32 | 0/32 | 0/32 | 13/32 | 0/32 |

A/B/C retain the three parents from the original eight-item proposal, in the same order. The eight-item version specializes MainHand, Chest, Head and Necklace on both healers. All one-healer variants, twelve-item Restoration outliers, full-armor recipes and other original controls remain in the independent evidence. Gear variants do not add actual compositions.

This is the first tested offense setting where all three eight-item routes fall inside the intended 10–50% point-rate band together, with no observed dominant control. It justifies keeping **0.575 fixed**, rather than proposing another coefficient or changing the search algorithm. These are only 32 observations per recipe. The prior 0.525 and 0.60 results used different seeds and cannot be paired with or pooled into this sample. The three routes are actual composition differences: their Essence choices differ on healer slot 2; they are not reordered copies. Broader archetype diversity and ordinary-player ownership remain unestablished.

These **32-sample counts are descriptive**, not evidence that the simultaneous 10% minimum and 50% ceiling have passed. The 0.575 candidate remains unaccepted and unapplied. Any later acceptance requires a separately declared full-family screen and independent confirmation. No confirmation or application followed this diagnostic.

## Implementation and verification

Added `analysis/tower-kodoku-offense-refinement.py` for the single fixed 0.575 candidate and `analysis/tower-kodoku-offense-refinement-diagnostic.py` for its separate two-by-sixteen contract. The ability dispatcher and runner recognize these explicit versions. Shared diagnostic verification uses the validated declaration's exact panel size. Both closed 0.525/0.60 candidates and older eight-seed diagnostics remain strict. The new contract requires the unchanged original 186-recipe family, native preparation, pinned inputs, two declared paths and fresh seeds; all runtime/archive limits are preserved. Ordinary panels retain the 16–512 seed guard.

`LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs` independently validates the new version's exact sixteen-seed panel and non-acceptance declaration. `KodokuRefinementDiagnosticTests.cs` adds six scope-boundary cases, two native summon scaling/mitigation cases and one owned no-combat preparation fixture. It prepares every recipe under both catalogs, preserves all party participants, allows only Power/ArmorPenetration/MagicPenetration changes from the original guardian and only Power changes from the 0.60 guardian. Fresh and bound runtimes produce identical projections.

**316 distinct Python checks pass**, including 22 new candidate/diagnostic guards. **342 distinct backend cases pass / four intentional skips**, both in a fresh build and with the new test DLL bound to five unchanged qualified combat assemblies; these are the same 342 cases counted once. Both diagnostic native fixtures pass. The 372 original/candidate preparations allocate no fights or seeds. Backend tests used `build/run-tests.ps1`. One initial Python fixture used eight seeds for its positive case; it was corrected before the passing suite and before any diagnostic allocation.

The independent collector reauthenticated all archive members, recounted every outcome, checked every prepared participant against the native projection and verified candidate contents, settings, runtime, panel order, resource admission and the complete seed union. Final exclusions: **924,268**. No extra panel, retry or replacement seed ran.

| Panel | Fights | Native time | Doubled admission estimate |
| --- | ---: | ---: | ---: |
| 1 | 2,976 | 191.676s | 391.79s |
| 2 | 2,976 | 186.720s | 383.35s |

The unchanged limits are 672 seconds for doubled admission, 840 seconds native, 900 seconds owner and 2 GiB per archive, with the archive/disk guards checked before each allocation.

Changed maintained files: two new Python candidate/diagnostic helpers and their safeguard tests; shared catalog/diagnostic helpers, ability dispatcher and runner; native execution guard and new preparation/summon tests; this report and six current Tower/harness documents. Local orchestration and immutable evidence are under TestResults. **No live game catalog change, migration, configuration change, shared database action or deployment.** No required verification remains blocked.

## Next work

Keep the existing **0.575 candidate and all 186 recipes fixed**. The unallocated proposal `TestResults/tower-floor8-kodoku-fixed-acceptance-proposal-20261001.json` calls for a **512-seed full-family screen**, split into **32 complete sixteen-seed batches / 95,232 fights**, followed only on a full pass by an independent screen-sized confirmation. Maximum: **190,464 fresh study fights / 1,024 fresh reservations**, with no interim selection, retry, extension or extra coefficient. At 512 seeds, the unchanged family-adjusted rule requires at least two actual eligible compositions with a recipe at **76 or more wins**, and **every recipe at 214 wins or fewer**. This larger fixed sample avoids choosing a 256-seed panel whose adjusted ceiling boundary, 98/256, is already below the pilot leader. It does not guarantee acceptance. Diagnostic outcomes never count toward either phase. Implement and test a separate strict aggregate and full-confirmation application contract before declaring or allocating. A passing independent confirmation permits only the exact local catalog application with **95,232 matched inputs / 5,952 full historical replays** across all batches and fresh backend verification. No external deployment. The latest measured batch is **186.72s native**, giving **373.44s** doubled admission; roughly **99.6 minutes native per phase**, excluding archive/audit overhead. Recheck resources and remaining disk before every batch. This proposal is not implemented or declared; no acceptance seed is allocated.

Floor **8 remains unresolved** and floor **7 remains the latest locally applied Tower change**. Floors **8–10 and 12–15** still need their equipment checks, followed by the final current-version floors 1–15 sweep. Preserve the expected repeating gear curve, approved Essence progression and stronger equipment carried forward. Continue Tower balancing; no dungeon/acquisition work or search redesign is included.

## Evidence

- Independent evidence: `TestResults/tower-floor8-kodoku-offense-refinement-evidence-20261001.json`, SHA `0f261aed9bda0249384ada92ca109cf3ded461383f076ea66da9e39ad5f582f5`.
- Frozen declaration: `TestResults/tower-floor8-kodoku-offense-refinement-driver-20261001/declaration.json`, SHA `785ff0e8452e3da75b4d9cb47a99187d027f3efb7bb2388e13493e9cf20b284f`.
- Python checks: `TestResults/tower-floor8-kodoku-offense-refinement-tests-20261001/completion.json`.
- Native verification and prepared projections: `TestResults/tower-floor8-kodoku-offense-refinement-runtime-verification-20261001/completion.json`.
- Prepared source: `TestResults/tower-balance-pass-floor8-eight-item-preparation-study-20260929`.
- Diagnostic archives: `TestResults/tower-balance-pass-floor8-kodoku-offense-refinement-diagnostic-1-study-20260929` and `...-2-study-20260929`.
- Publication: `TestResults/tower-floor8-kodoku-offense-refinement-publication-check-20261001.json`.
- Commands: bundled Python `-B -X utf8` for the fourteen suites, driver, collector and publisher; recorded backend commands through `build/run-tests.ps1`; `git diff --check` and local Markdown link validation.

# Floor 7: expanded-family health refinement — 29 September 2026

**Later floor-8 result:** the [midpoint refinement](Tower-Floor8-Health-Refinement-20260929.md) applies health **9.9064526367 (+5%)**, preserving offense and regeneration. The complete 67-cell confirmation establishes **47/160 (29.38%)** and **33/160 (20.62%)**; all 10,720 inputs and 67 full replays match. The report below retains its own historical setting and results. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **906,552 exclusions**, current content and the floor-12 queue.

**Applied locally and verified:** Eydis health is **4.4454238281 (+5.5%)**. Offense remains **6.5953125** and regeneration **0.1**. A fresh confirmation across **60 exact recipes / eight compositions** establishes two viable lineups at **95/256 (37.11%)** and **82/256 (32.03%)**. Every adjusted upper bound is below 50%. All **15,360 native inputs and 60 full replays** match the applied content. **82 backend regressions and ten refinement selection checks passed**, with three intentional opt-in skips. These are related poison builds using resistance-and-health gear; broader archetypes and ordinary-player acquisition remain unestablished.

## Prospective refinement

The [initial floor-7 diversity scope](Tower-Floor7-Diversity-20260929.md) is closed `NoGridCandidate`. Its supported searches found three new compositions, and the strongest two at +4% health won 34/64 and 24/64: still above the declared screening ceiling. This new scope tests a narrow increase in difficulty against the entire expanded family. It does not extend a closed panel or repeat a confirmation.

Start from `TestResults/tower-balance-pass-floor7-diversity-grid-baseline-study-20260929`, manifest SHA **`7029d8a61225be266f94d00ec7c77b231b427a3bf3b1a5ff092914ca9321731e`**. Keep all **60 exact cells / eight actual compositions**, including every original reference, exact search nominee and projected gear variant. Current health remains **4.213671875**, offense **6.5953125**, regeneration **0.1**. Entry Tower SHA is **`0a53b4b4e453e13aee5a94cfedf8be65171928cdf8e95dc5a9bfdab954161cc8`**; initial exclusion union is **904,510**. The preceding evidence index SHA is **`efddd666031ecf702a301b49482fdeec801f120d164685dbce89e97c4e813369`**.

Preserve the approved five-character, level-40, five-Essence, tier-1 Unique / Exceptional / rank-4 budget, fixed rolls and no styles/ascension/evolution. Ownership remains hypothetical. No new search, supply change, acquisition work, identity rewrite or Essence permutation is part of this scope.

Freeze the following before combat:

1. Authenticate the complete source/runtime; use zero-fight current-content preparation and native settings/catalog parity.
2. Evaluate **health factors 1.045, 1.05, 1.055 and 1.06**, each relative to the entry baseline, on separate **64-seed complete-family panels**. Change no other field. Admit settings only when every cell is at most **28/64** and at least two distinct actual compositions have a cell at least **12/64**.
3. Rank by qualifying composition count, second composition's wins, strongest rate closest to 30%, least health change, then label. Advance at most two settings to separate **128-seed full-family stability panels**. Require every cell at most **48/128**, and at least two distinct compositions at least **24/128**; select at most one using the same ranking.
4. Require projected confirmation time/size to fit within 80% of the existing 840-second / 2-GiB native envelope. If admitted, run exactly one new **60 × 256 = 15,360-fight confirmation**. Accept only if every approximate simultaneous 95% Bonferroni-Wilson upper bound is at most 50% and at least two distinct compositions have a lower bound at least 10%. Count actual per-slot Essence sets for diversity, without changing saved recipes. Standard one-composition `Pass` alone is insufficient.
5. Only upon acceptance, apply the confirmed floor-7 health value locally. Require whole-Tower equality to the confirmed snapshot, preserving all other fields and floors. Check all 15,360 native inputs and one full replay per cell, with no new seeds. Run the relevant backend regressions through `build/run-tests.ps1`, then reconcile immutable inputs, seed/fight accounting and application evidence.

Maximum: **46,080 study fights + 60 conditional replays = 46,140 fights**, **768 fresh reserved values**. Each phase remains below 20,000 fights, native 840 seconds / process 900 seconds and 2 GiB. Stop on technical failure, resource refusal, no eligible setting or failed confirmation. No retry, extension, pooling, excluded recipe, alternate confirmation, or reused seed. Preserve all preceding and new rejected settings. No pacing threshold is implied by the acceptance band.

The local driver is `TestResults/tower-floor7-diversity-refinement-driver-20260929.py`. The existing owner, compiled runtime and supported search policy remain unchanged. Archive this declaration before running; do not overwrite paths or repin historical files.

## Completed selection and confirmation

Every panel retained the same 60 exact recipes. All observations below are from separate seed panels; none was pooled into confirmation.

| Health increase | Grid composition leaders, wins / 64 | Grid decision | Stability leaders, wins / 128 | Stability decision |
| --- | --- | --- | --- | --- |
| +4.5% | 30, 23, 10, 1, 1, 0, 0, 0 | Rejected: ceiling | — | Not admitted |
| +5% | 27, 25, 12, 2, 0, 0, 0, 0 | Advanced | 49, 32, 9, 1, 0, 0, 0, 0 | Rejected: 49 exceeds 48 |
| +5.5% | 22, 21, 3, 0, 0, 0, 0, 0 | Advanced | 46, 33, 7, 1, 0, 0, 0, 0 | Selected |
| +6% | 19, 12, 4, 0, 0, 0, 0, 0 | Eligible, ranked third | — | Not admitted |

The resource preflight projected **173.71 native seconds / 212,308,062 bytes**, within the declared confirmation envelope. Exactly one confirmation ran, at +5.5% health:

| Composition | Best measured gear | Wins / 256 | Adjusted interval | Mean engine seconds, all outcomes |
| --- | --- | ---: | --- | ---: |
| New `dae6cc…` | Resistance-and-health | 95 (37.11%) | 27.76–47.54% | 68.80 |
| New `0d375e…` | Resistance-and-health | 82 (32.03%) | 23.21–42.35% | 67.19 |
| New `a11ce0…` | Resistance-and-health | 25 (9.77%) | Below viability floor | 72.43 |
| Five earlier compositions | Every tested profile | 0 each | Below viability floor | See saved rows |

Intervals are approximate simultaneous 95% Bonferroni-Wilson intervals across this complete **60-cell family**. Only the first two cells qualify; they are distinct actual per-slot Essence compositions. Both retain poison synergies and share one gear profile, so this result establishes two viable lineup choices, not broad archetype or equipment diversity. The prior leaders did not remain viable at the stronger setting; their older results are not transferred to this content.

The second confirmed team came from the **health-and-regeneration search**, which retained its benchmark and measured that finalist at just 1/128 in its original gear. Keeping that exact nominee and testing all frozen gear variants exposed its viable resistance-and-health version. Preserve unsuccessful search nominees and complete recipe coverage in later work.

## Applied content and verification

Only **floor-7 `guardianScaling.health` changed: 4.213671875 → 4.4454238281**. Whole-file semantic comparison matched the confirmed snapshot; all other floor-7 fields and all other floors remained unchanged. The application verifier matched **15,360 inputs and 60 complete battle reports**, allocated no seeds and completed with no retry. The final Tower SHA is **`9781042377897c33360e2bff0bf85c76a610d39dcccfebe3b8f33cfc8e619a0b`**.

This refinement completed **eight phases / 46,080 study fights + 60 application replays**, reserving **768 fresh values**. Native study time was **540.911 seconds**, excluding audits and application verification. All **808 immutable runtime/input pins** matched; the authorized live Tower file was separately checked against the accepted snapshot. The final exclusion union is **905,278**. Together with the closed initial scope, this continuation ran **70,048 study fights + 60 application replays**, with **1,626 fresh reservations**. No active study remains.

Final verification: **82 backend tests passed**, three intentional opt-in skips. The initial scope also passed **14 maintained Python tests and ten selection checks**; this refinement passed **ten additional seed-free selection/accounting checks**. Existing compiled C# was reused after source-timestamp and runtime-hash checks. No search or combat implementation changed.

| Artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor7-diversity-refinement-evidence-20260929.json` | `bea160c3fbaf5ad7c66ec18c712c4fa79b0c85485a72dad1b8dcf1665e47dbe3` |
| `tower-balance-pass-floor7-diversity-refinement-confirmation-study-20260929/files.json` | `497df7007dc1a715006ff9ffcf128cc572cbfef4657d408ea607e546e7117f20` |
| `tower-balance-pass-floor7-diversity-refinement-confirmation-owner-20260929/independent-audit.json` | `cd26cbacad9adac470970da54b2ffadd83d60a7f9c90730d5222c379172db7a7` |
| `tower-balance-pass-floor7-diversity-refinement-confirmation-owner-20260929/seed-ledger.json` | `80a6fc4ff2a041002bfda661f4dacc7a9a1e0d6060f3b54f74e46d89e0d20f14` |
| `tower-floor7-diversity-refinement-application-owner-20260929/result.json` | `e5007333ebfaebc5b99544aaaeb9d46c08e382c7c3d5361f3b6ed897f6a4f626` |
| `tower-floor7-diversity-refinement-final-regression-20260929.trx` | `95be8e31350101312cc3e87c879fbf46e4a5df922f18238fd877907323832c2d` |

Use this accepted **60-cell confirmation** for later floor-7 work. Do not fall back to the old 38-cell family or reimport already retained searches. Preserve the initial `NoGridCandidate` scope and the rejected +5% stability panel. Whole-Tower snapshots on other floors need current-content preparation before reuse.

Changed maintained files:

- [tower-floors.json](../LL/src/API/API.LL/Data/world-tower/tower-floors.json): only floor-7 health changed in this continuation.
- This report and the [initial search/diversity report](Tower-Floor7-Diversity-20260929.md): prospective protocols, results, limitations and evidence.
- [Current handoff](Tower-Continuation-Handoff-20260928.md), [harness README](../LL/tools/BalanceHarness/README.md) and [search guide](../LL/tools/BalanceHarness/AFFINITY-SEARCH.md): latest accepted source, exclusions and next scope. Historical floor-1/3/7/9, reference-coverage and floor-5 reports receive follow-up notices without replacing their original results.

Completed commands used the bundled Python runtime and fresh output paths:

```powershell
python -B -X utf8 'Balance Harness/analysis/test-tower-reference-coverage.py'
python -B -X utf8 'TestResults/tower-floor7-diversity-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor7-diversity-refinement-driver-20260929.py'
python -B -X utf8 'TestResults/tower-floor7-diversity-refinement-apply-20260929.py'
./build/run-tests.ps1 -NoBuild -ArtifactsPath 'TestResults/tower-reference-coverage-build-20260929' -Filter 'FullyQualifiedName~BalanceHarnessTowerBalancePassTests|FullyQualifiedName~BalanceHarnessAffinityFloorEvaluationTests|FullyQualifiedName~BalanceHarnessTowerBalanceApplicationTests|FullyQualifiedName~BalanceHarnessProgressionEquipmentTests|FullyQualifiedName~BalanceHarnessGearProfileTests|FullyQualifiedName~BalanceHarnessTowerBossDiscoveryContractTests'
python -B -X utf8 'TestResults/tower-floor7-diversity-refinement-collect-20260929.py'
```

No required command remained blocked. This is a local content-data change, with no migration, application-setting change or deployment. The value will affect game content when later released through the normal process; no service was started or external environment changed here. Next: **floor-8 build diversity**, preserving its full 38-cell family and reviewing pacing separately; floors 12/13 and broader archetypes remain open. Dungeon work and replacement supplies are not prerequisites.

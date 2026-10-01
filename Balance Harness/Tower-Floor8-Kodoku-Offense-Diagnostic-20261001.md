# Floor 8: Kodoku 0.60 offense diagnostic — 1 October 2026

**Latest floor-8 result (1 October):** The [0.575 offense diagnostic](Tower-Floor8-Kodoku-Offense-Refinement-20261001.md) completed **5,952 fresh fights / 32 seeds** across all **186 recipes / nine compositions**. Independent recount: eight-item routes **12/32, 12/32 and 13/32** (37.5%, 37.5%, 40.6%); no other recipe exceeded **2/32**. **Hold 0.575 fixed and move to formal acceptance testing.** The diagnostic does not establish balance. Proposed next: a full-family **512-seed screen**, then independent **512-seed confirmation only on a pass**, using 32 sixteen-seed batches per phase. Maximum **190,464 fresh fights / 1,024 seeds**; the new aggregate remains unimplemented and unallocated. **316 Python checks and 342 backend cases pass / four intentional skips**, plus both diagnostic fixtures. Exclusions: **924,268**. No gameplay edit, migration, configuration change or deployment. Floor 8 remains unresolved; floor 7 remains the latest applied change.

The 0.60 offense diagnostic completed, but does not establish floor-8 balance. The three eight-item parties won **6/16, 1/16 and 1/16**; every other retained recipe won **0/16**, including all one-healer and twelve-item controls. The strongest route remains promising at **37.5%**, while the other two are below the intended 10% minimum in this small sample. The candidate is unaccepted and unapplied.

Target: primary LL World Tower and offline Balance Harness. Continue the [eight-item gear diagnostic](Tower-Floor8-Eight-Item-Diagnostic-20261001.md), which found three gear-limited routes winning 15/16, 13/16 and 16/16 against the isolated 0.525 candidate. That candidate remains rejected and unapplied.

## Frozen protocol

Authenticate publication `80d149df0b08a0df0ee003f6a8f9aa7d2014b08fc60eac98a657904d79d53e3f`, snapshot changed source and maintained documents, and preserve all **924,220** excluded seeds. Use the exact unallocated `TestResults/tower-floor8-kodoku-offense-proposal-20261001.json`.

Keep the entire **186-recipe / nine-composition / 128 equipment-eligible** family unchanged. Preserve raw Essence order, identities, positions, all gear and progression budgets, including the eight-item winners, every twelve-item Restoration setup and all armor controls. Equipment eligibility remains at most eight specialized items on two characters.

Implement a separate strict `tower-kodoku-eight-item-pressure-v1` candidate. From the original unchanged source, set offense factor **0.60**, yielding guardian offense **5.2956152344**, instead of the preceding isolated 0.525 factor. This is a **14.29% relative increase** over that candidate. Preserve guardian Health and every other scalar, penetration ×40, exclusive Venomspawn ArmorPenetration inheritance, original Miasma and all unrelated content. Do not compound onto the preceding candidate. The closed 0.525 contract must continue to reject changed coefficients.

Run candidate/provenance/diagnostic safeguards, backend tests through `build/run-tests.ps1`, and a fresh/bound native verification against the unchanged qualified production assemblies. Prepare all 186 recipes under original and candidate catalogs without combat. Original participants must match the preceding native projection. Only guardian Power, ArmorPenetration and MagicPenetration may differ from the original catalog; only guardian Power may differ from the preceding 0.525 candidate. Verify both initial and Insect Jar summon Power scaling with preserved Health and penetration/mitigation. Reuse the authenticated original-catalog 186-recipe preparation archive.

Freeze a separate `floor8-kodoku-offense-diagnostic-v1` declaration: **two complete eight-seed panels**, each covering every recipe, for **2,976 diagnostic fights / 16 fresh reservations maximum**. Native execution remains explicitly diagnostic-only and excluded from acceptance. Ordinary screen/search/confirm panels retain their 16–512 seed guard. No interim statistical selection, extra coefficient, extension, retry, replacement panel, confirmation or application. Stop after both panels or any failure, preserving all evidence and reservations. No historical outcome transfers.

Use the preceding eight-item diagnostic panel 2 as the first resource reference; double its per-fight runtime and archive size for 1,488 fights. Before panel 2, use panel 1's completed measurements. Require under **672 seconds** and 80% of 2 GiB, with free disk for all remaining doubled archive estimates plus 2 GiB. Keep **840-second native / 900-second owner / 2 GiB archive** limits. Observe supervisor stdout only during native execution; leave active study, owner and control artifacts unopened.

After both panels, independently authenticate the archives, recount every outcome, compare every prepared participant to the qualified native projection, verify exact runtime/settings/catalogs and reconcile all reservations. Compare the three eight-item routes with baseline, one-healer and twelve-item controls on this same combined 16-seed schedule; retain all other controls. Counts are descriptive and cannot establish balance. A later acceptance study needs fresh complete family-adjusted screening, at least two eligible actual compositions with lower bounds ≥10%, all upper bounds ≤50%, and independent confirmation.

Update this report and the current Tower/harness documentation. No live game catalog change, migration, configuration change, shared database action, deployment, dungeon/acquisition work or search redesign is included.

## Completed result

The 0.60 offense diagnostic completed, but does not establish floor-8 balance. The three eight-item parties won **6/16, 1/16 and 1/16**; every other retained recipe won **0/16**, including all one-healer and twelve-item controls. The strongest route remains promising at **37.5%**, while the other two are below the intended 10% minimum in this small sample. The candidate is unaccepted and unapplied.

Both complete panels retained **186 recipes / nine actual compositions / 128 equipment-eligible recipes**. Total: **2,976 fresh diagnostic fights / 16 new reservations**, eight seeds per panel, with no interim statistical selection. The table combines only these two predeclared panels; no prior outcomes contribute.

| Composition | Baseline | Six items on slot 2 | Six items on slot 7 | Eight items across both healers | Twelve items across both healers |
| --- | ---: | ---: | ---: | ---: | ---: |
| A | 0/16 | 0/16 | 0/16 | 6/16 | 0/16 |
| B | 0/16 | 0/16 | 0/16 | 1/16 | 0/16 |
| C | 0/16 | 0/16 | 0/16 | 1/16 | 0/16 |

A/B/C retain the three parents from the original eight-item proposal, in the same order. The eight-item version specializes MainHand, Chest, Head and Necklace on both healers. All one-healer variants, twelve-item Restoration outliers, full-armor recipes and other original controls remain in the independent evidence. Gear variants do not add actual compositions.

The preceding 0.525 diagnostic recorded **15/16, 13/16 and 16/16** for these same three recipes. The two settings used different fresh seed panels, so their counts cannot be treated as paired comparisons or pooled into acceptance evidence. The observed drop motivates testing one interior offense setting; sixteen observations per recipe cannot establish the true rates or prove a monotonic response. No gear, Essence order, party position, guardian Health, penetration or Miasma setting changed between these candidates.

These **16-sample counts are descriptive**, not evidence that the simultaneous 10% minimum and 50% ceiling have passed. The 0.60 candidate remains unaccepted and unapplied. Any later acceptance requires a separately declared full-family screen and independent confirmation. No confirmation or application followed this diagnostic.

## Implementation and verification

Added `analysis/tower-kodoku-offense.py` for the single fixed 0.60 candidate and `analysis/tower-kodoku-offense-diagnostic.py` for its separate two-panel contract. The ability dispatcher and runner recognize these explicit versions. Shared catalog-writing and verification helpers are reused without weakening the closed 0.525 candidate or preceding diagnostic. The new contract requires the exact original 186-recipe family, native preparation, pinned inputs, two declared paths, fresh seeds and unchanged limits. Ordinary screen/search/confirm panels still require 16–512 seeds.

`LL/tests/EssenceSystem.Tests/BalanceHarnessTowerBalancePassTests.cs` independently gates the new eight-seed diagnostic version and marks its results diagnostic-only with `usedForAcceptance: false`. `KodokuOffenseDiagnosticTests.cs` adds six scope-boundary cases, two native summon scaling/mitigation cases and one owned no-combat preparation fixture. It prepares all 186 recipes under both original and candidate catalogs, preserves every party participant, permits only guardian Power/ArmorPenetration/MagicPenetration changes from the original catalog, and permits only Power changes from the preceding 0.525 candidate. Fresh and bound runtimes produce identical projections.

**294 distinct Python checks pass**, including 20 new candidate and diagnostic safeguards. **333 distinct backend cases pass / four intentional skips**, both in a fresh build and with the new test DLL bound to the five unchanged qualified combat assemblies; these are the same 333 cases counted once. Both diagnostic native fixtures pass. The 372 original/candidate native preparations allocate no seeds or fights. Backend execution used `build/run-tests.ps1`.

The independent collector reauthenticated all archive members, recounted every outcome, checked every prepared participant against the native projection and verified candidate contents, settings, runtime, panel order, resource admission and the complete seed union. Final exclusions: **924,236**. No extra panel, retry or replacement seed ran.

| Panel | Fights | Native time | Doubled admission estimate |
| --- | ---: | ---: | ---: |
| 1 | 1,488 | 91.485s | 312.55s |
| 2 | 1,488 | 97.947s | 182.97s |

The unchanged limits are 672 seconds for doubled admission, 840 seconds native, 900 seconds owner and 2 GiB per archive, with the archive/disk guards checked before each allocation.

Changed maintained files: two new Python candidate/diagnostic helpers and their safeguard tests; shared catalog/diagnostic helpers, ability dispatcher and runner; native execution guard and new preparation/summon tests; this report and six current Tower/harness documents. Local orchestration and immutable evidence are under TestResults. **No live game catalog change, migration, configuration change, shared database action or deployment.** No required verification remains blocked.

## Next work

Keep all **186 recipes fixed**. The next unallocated proposal, `TestResults/tower-floor8-kodoku-offense-refinement-proposal-20261001.json`, tests offense factor **0.575** from the original source, giving guardian offense **5.0749645996**: **4.17% lower than 0.60**, while still **9.52% higher than 0.525**. Preserve penetration ×40, Venomspawn inheritance, Health, original Miasma and all other content. This is one bounded engineering hypothesis, not a win-rate estimate. Use a separately declared **two-by-sixteen diagnostic / 5,952 fights / 32 fresh seeds maximum** to reduce the uncertainty of these small counts. The completed second panel admits a sixteen-seed panel at a doubled estimate of **391.79s**, below 672 seconds; recheck time, archive and disk limits before each allocation. Implement separate strict candidate/diagnostic versions and native scaling checks first. No interim selection, retry, extension, extra coefficient, confirmation or application. No follow-on candidate has been materialized and no follow-on seed allocated. If a later diagnostic supports at least two routes without a dominant outlier, proceed to a fresh full-family acceptance screen and independent confirmation; these diagnostics never count toward either.

Floor **8 remains unresolved** and floor **7 remains the latest locally applied Tower change**. Floors **8–10 and 12–15** still need their equipment checks, followed by the final current-version floors 1–15 sweep. Preserve the expected repeating gear curve, approved Essence progression and stronger equipment carried forward. Continue Tower balancing; no dungeon/acquisition work or search redesign is included.

## Evidence

- Independent evidence: `TestResults/tower-floor8-kodoku-offense-evidence-20261001.json`, SHA `47adf449eff0125f1d63d38506ddd5487957c196a4bab229282f87738c9101b3`.
- Frozen declaration: `TestResults/tower-floor8-kodoku-offense-driver-20261001/declaration.json`, SHA `d06a9f024a4dfec6180efe4817b77782d67212bb8b876088be6a12fa27064176`.
- Python checks: `TestResults/tower-floor8-kodoku-offense-tests-20261001/completion.json`.
- Native verification and prepared projections: `TestResults/tower-floor8-kodoku-offense-runtime-verification-20261001/completion.json`.
- Prepared source: `TestResults/tower-balance-pass-floor8-eight-item-preparation-study-20260929`.
- Diagnostic archives: `TestResults/tower-balance-pass-floor8-kodoku-offense-diagnostic-1-study-20260929` and `...-2-study-20260929`.
- Publication: `TestResults/tower-floor8-kodoku-offense-publication-check-20261001.json`.
- Commands: bundled Python `-B -X utf8` for the thirteen suites, driver, collector and publisher; recorded backend commands through `build/run-tests.ps1`; `git diff --check` and local Markdown link validation.

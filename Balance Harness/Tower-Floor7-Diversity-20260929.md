# Floor 7: supported search and complete-family calibration — 29 September 2026

**Subsequent accepted result:** the separately declared [health refinement](Tower-Floor7-Diversity-Refinement-20260929.md) applies **+5.5% health**, confirming two new compositions at **95/256 and 82/256** across the complete 60-cell family. This initial search/selection scope remains closed `NoGridCandidate`; its observations were not pooled into that confirmation. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for the latest content hash and exclusions.

## Prospective scope

This is a new, bounded floor-7 study. It retains every exact recipe from the completed [reference-coverage pass](Tower-Reference-Coverage-20260929.md), whose strongest two compositions won 46/256 and 39/256 with resistance-and-health gear. Only the first cleared the family-adjusted viability floor. The target is at least two distinct viable compositions while maintaining the ceiling for the entire expanded recipe family. This does not reopen earlier studies or change the search algorithm.

The source is `TestResults/tower-balance-pass-floor7-reference-coverage-confirmation-study-20260929`, manifest SHA `c21696c919f476f84fbfd3b4e51e7c4d001642aa12999c766828205df68b41fe`: **38 exact cells / five actual compositions**. Current Tower SHA is `0a53b4b4e453e13aee5a94cfedf8be65171928cdf8e95dc5a9bfdab954161cc8`; initial exclusion union is **903,652**. Authenticate the historical owner snapshot for its old Python pin. Refresh the other floors through seed-free current-content preparation, requiring identical floor-7 content, non-Tower catalogs and native settings. Never repin the historical archive.

Keep five level-40 characters, five unascended/unevolved Essences each, tier-1 Unique / Exceptional / rank-4 equipment, fixed rolls and no styles. Ownership is hypothetical. Eydis starts at health **4.213671875**, offense **6.5953125**, regeneration **0.1**. Only health is a calibration variable. No supplies, dungeon changes, acquisition claim, Essence permutation, position change or new algorithm is authorized by this scope.

Declare the full sequence before combat:

1. Seed-free current-content preparation; then a fresh **38 × 64** baseline screen to select measured references at current whole-Tower content.
2. Exactly two existing `affinity-creation-with-benchmark-validation-v1` searches: **resistance-and-health**, then **health-and-regeneration**. The latter has the lowest remaining guardian health among the other historical profiles, although none won. Each search spends **528 search fights plus five exact nominees × 128 fresh evaluation seeds = 1,168 fights**. Select three distinct actual compositions per profile using the existing wins/remaining-health/ID ranking; the strongest is the explicit benchmark. Freeze each reference choice before its search. No retry or additional search.
3. Retain all 38 original cells, every exact reference and finalist from both searches, and each finalist across all seven existing gear profiles. Deduplicate only identical whole scenarios. Freeze the complete expanded family, at most **76 cells / nine actual compositions**, before scalar screening. A duplicate composition is still retained when its exact scenario differs.
4. Screen **health factors 1.00, 0.96, 0.98, 1.02 and 1.04**, each relative to the unchanged current baseline, with **64 fresh seeds per cell**. Offense and every other field remain fixed. Grid eligibility: every cell at most **28/64 wins** and at least two distinct compositions with a cell at least **12/64**. Rank by number of qualifying compositions, second composition's wins, strongest rate closest to 30%, smallest health change, then label. Advance at most two settings.
5. Evaluate each nominee on a separate **128-seed full-family stability panel**. Require every cell at most **48/128** and at least two distinct compositions at least **24/128**. Use the same ranking to select at most one setting. If no eligible setting remains, close without confirmation or application.
6. Before confirmation, require the selected stability panel's projected 256-seed native time and archive size to fit within 80% of the existing 840-second / 2-GiB phase envelope. If admitted, run exactly one fresh **256-seed complete-family confirmation**, at most **19,456 fights**. Require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50%, and at least two distinct actual compositions with a lower bound at least 10%. The native one-composition `Pass` alone is insufficient. No novelty condition is imposed: previously retained distinct teams can establish diversity. Count compositions by actual per-slot Essence sets, ignoring labels/identities/gear, without rewriting raw recipes.
7. Only after acceptance, apply the confirmed floor-7 health value locally (or retain it if the baseline wins). Require whole-Tower equality to the confirmation snapshot except for that authorized health edit. Verify every confirmation input and one full native replay per exact cell, without new seeds. Run the relevant backend regressions through `build/run-tests.ps1` and reconcile evidence.

Maximum study allowance: **68,000 fights**, plus at most **76 application replays**, and **1,370 newly reserved seed values**. Each phase remains capped at 20,000 fights, native 840 seconds / process 900 seconds and 2 GiB. Allocation is sequential, with the complete inherited exclusion union. No retries, seed reuse, panel extension, pooling, dropped references, or fallback confirmation after failure. Preserve every rejection and unused reservation. Duration is reported separately; there is no approved pacing threshold.

The frozen local driver is `TestResults/tower-floor7-diversity-driver-20260929.py`. It archives this protocol, source/runtime pins, exact recipes, decisions and bounds. The existing owner and compiled combat/search implementation remain unchanged. Local ignored evidence is needed to reproduce this audit trail and is not present in a clean checkout.

## Completed search and closed selection

This scope closed **`NoGridCandidate`** after **nine phases / 23,968 fights**, with no stability panel, confirmation, application replay or game-data change. The two searches added **three actual compositions**, expanding the family to **60 exact cells / eight compositions**. All 38 original cells and all ten exact search-nominee entries remain represented. Of 38 import provenance entries, 22 add scenarios; identical whole scenarios account for the remaining entries.

At resistance-and-health gear, the new finalists `dae6cc…` and `a11ce0…` won **100/128 and 67/128**, versus **26/128** for the retained benchmark. That search returned `ChallengerNeedsConfirmation`. Health-and-regeneration produced `a11ce0…` and `0d375e…`, each at **1/128**; its supported gate returned `BenchmarkRetained`. The latter finalists are still retained and tested across every frozen gear profile. These are selection observations, not confirmed balance rates.

All three new compositions are nearest to prior `a27a86…`:

| New composition | Slot | Removed Essences | Added Essences |
| --- | ---: | --- | --- |
| `dae6cc…` | 4 | Green Slime, Pixie | Venomous Spiderling, Viper |
| `a11ce0…` | 3 | Giant Bat | Spider Queen Royal Venom |
| `0d375e…` | 1 | Elder Treant Thornstorm, Forest Spirit | Poisonous Rat, Viper |

These are actual substitutions, with canonical ordering and fixed positions. They remain poison-focused variants, so broad archetype diversity is unestablished.

Each grid panel evaluated the complete 60-cell family on 64 fresh seeds:

| Health change | Best wins by distinct composition, descending | Grid eligible? |
| --- | --- | --- |
| Unchanged | 52, 47, 29, 10, 8, 0, 0, 0 | No: ceiling |
| −4% | 64, 57, 52, 36, 28, 2, 0, 0 | No: ceiling |
| −2% | 61, 52, 41, 23, 15, 0, 0, 0 | No: ceiling |
| +2% | 46, 36, 18, 5, 0, 0, 0, 0 | No: ceiling |
| +4% | 34, 24, 8, 1, 0, 0, 0, 0 | No: ceiling |

No setting met the prospectively declared maximum of 28/64. The expanded family exposes stronger routes than the earlier 38-cell confirmation could establish. The next step is the separately declared [health refinement](Tower-Floor7-Diversity-Refinement-20260929.md); this initial scope remains closed without extension or pooled observations.

## Verification and retained evidence

**82 backend regressions, 14 Python tests and ten seed-free selection/accounting checks passed**, with three intentional opt-in skips. All **779 immutable runtime/input pins** matched, all processes drained, and every native phase completed without retry. Native execution totaled **333.519 seconds**, excluding audits and wrapper overhead. The scope reserved **858 fresh values**, bringing the exclusion union to **904,510**. The Tower file remained byte-identical at the entry hash.

| Artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor7-diversity-evidence-20260929.json` | `efddd666031ecf702a301b49482fdeec801f120d164685dbce89e97c4e813369` |
| `tower-balance-pass-floor7-diversity-grid-baseline-study-20260929/files.json` | `7029d8a61225be266f94d00ec7c77b231b427a3bf3b1a5ff092914ca9321731e` |
| `tower-balance-pass-floor7-diversity-grid-health-plus-040-owner-20260929/seed-ledger.json` | `25d5703d2d2d00ee41b4e440ec7d54e558ff5675ea251a4eccfed4370c7c64e3` |
| `tower-floor7-diversity-final-regression-20260929.trx` | `3385830faf3c1fd7a2f6e984108e90a358af6b0485ec5329acbbfca07e3d2a12` |

Use the **expanded baseline source** in the table for subsequent current-content preparation: it has all 60 recipes and the unchanged guardian. Do not use the +4% candidate as though it were applied. The protocol snapshot, driver, frozen family, full IDs, command receipts and independent audits are retained under ignored `TestResults`. No maintained search/combat code changed. This report records the bounded result; no migration, application-setting change or deployment was performed.

# Floor 8: supported-search diversity challenge — 29 September 2026

**Later floor-8 result:** the [midpoint refinement](Tower-Floor8-Health-Refinement-20260929.md) applies health **9.9064526367 (+5%)**, preserving offense and regeneration. The complete 67-cell confirmation establishes **47/160 (29.38%)** and **33/160 (20.62%)**; all 10,720 inputs and 67 full replays match. The report below retains its own historical setting and results. Use the [current handoff](Tower-Continuation-Handoff-20260928.md) for **906,552 exclusions**, current content and the floor-12 queue.

## Prospective scope

Target: the primary game's floor-8 guardian content in `LL/src/API/API.LL` and the offline Balance Harness. Floor 8 currently has one clearly viable composition: the strongest two teams won **95/256 and 23/256**, both using armor-and-health gear. Before changing guardian scalars, use the existing supported search to challenge those teams and retain every exact recipe. This scope evaluates current content; it does not change the algorithm, player budget or guardian settings.

Source: `TestResults/tower-balance-pass-floor8-reference-coverage-confirmation-study-20260929`, manifest SHA **`0efa2950a5898554f825ffa03a534d98f1ab90caade57065214cd9aedc2c22e4`**, containing **38 exact cells / five actual compositions**. Current Tower SHA is **`9781042377897c33360e2bff0bf85c76a610d39dcccfebe3b8f33cfc8e619a0b`**; initial exclusion union is **905,278**. Authenticate the historical Python owner snapshot for its original pin, retaining all old archives unchanged. Current non-Tower catalogs and target-floor content match; no C# source is newer than the existing compiled runtime.

Keep **ten level-40 characters, five unascended/unevolved Essences each, tier-1 Unique / Exceptional / rank-4 gear**, fixed rolls, fixed positions and no styles. Ownership remains hypothetical. Kodoku's health/offense remain **9.4347167969 / 8.8260253906**, regeneration **1.0**. Do not restore withdrawn supplies or require dungeon/acquisition work. The preceding strongest-team duration was **195.53 engine seconds**; record duration separately because no pacing threshold is approved.

Freeze the following sequence before combat:

1. Seed-free current-content preparation, preserving the 38 recipes and requiring native settings/catalog parity. The historical whole-Tower snapshot predates later changes on floors 3/5/7. Then run a fresh **38 × 64** baseline panel to select measured references against current content.
2. Run exactly two **`affinity-creation-with-benchmark-validation-v1`** searches: **armor-and-health**, then **restorer-specialization**. The latter has the lowest historical remaining guardian health among nonwinning profiles. Select three distinct actual compositions per profile using the existing wins/remaining-health/ID ranking; the strongest is the explicit benchmark. Freeze choices before search. Each search spends **528 search fights + five exact nominees × 128 evaluation seeds = 1,168 fights**.
3. Keep all 38 old cells, every exact reference/finalist from both searches, and each finalist across all seven frozen gear profiles. Deduplicate only identical whole scenarios, preserving identity-distinct recipes and all provenance. Freeze the expanded family before evaluation, at most **76 cells / nine actual compositions**. Unsuccessful search nominees remain included.
4. Before expanding, project native time and archive size from the fresh 38-cell baseline, scaling by cell count and sample count. Require both to fit within **80% of the 840-second / 2-GiB phase envelope**. If admitted, run a separate **128-seed full-family screen**. Eligibility requires every cell at most **48/128 wins** and at least **two distinct actual compositions** with a cell at least **26/128**. Count actual per-slot Essence sets for diversity without rewriting raw inputs. New-composition status is recorded but is not an extra acceptance condition.
5. If eligible, project confirmation time/size from that screen with the same 80% admission rule. Freeze exactly one **192-seed complete-family confirmation**, at most **14,592 fights**. The smaller sample count is declared in advance because the ten-character floor is more expensive to simulate. Require every approximate simultaneous 95% Bonferroni-Wilson upper bound at most 50%, and at least two distinct compositions with a lower bound at least 10%. The standard one-composition `Pass` alone is insufficient. No observations are pooled across panels.
6. Only after acceptance, verify current whole-Tower equality to the confirmation snapshot, all confirmed native inputs and one full replay per exact cell, allocating no new seeds. No content edit is needed in this unchanged-setting scope. Run relevant backend regressions through `build/run-tests.ps1` and reconcile the evidence. If selection, resource admission or confirmation fails, close without parity or a gameplay change; any calibration requires a separately declared scope.

Maximum: **29,088 study fights + 76 conditional replays**, and **858 fresh reserved values**. Each phase remains below 20,000 fights, native 840 seconds / process 900 seconds and 2 GiB. Reserve panels sequentially against the complete inherited exclusion union. No retries, seed reuse, panel extension, omitted recipe, pooled result or fallback confirmation after failure. Preserve failures and unused reservations. No new search-policy development, migration, external deployment or application-setting change is part of this scope.

Frozen driver: `TestResults/tower-floor8-search-challenge-driver-20260929.py`. Existing owner: `Balance Harness/analysis/run-tower-balance-pass.py`; compiled runtime: `TestResults/tower-reference-coverage-build-20260929`. Immutable native reports and audits remain in ignored `TestResults`; a clean checkout does not contain them.

## Completed searches and retained family

Both searches completed **1,168 fights** with native reconstruction and independent audits. Armor-and-health returned `ChallengerNeedsConfirmation`: new `049002…` and `230ac3…` each won **93/128**, versus **40/128** for the benchmark. Restorer-specialization returned `BenchmarkRetained`; its two finalists and all three references won **0/128** in that gear. Those finalists were nevertheless retained across all frozen profiles.

The expanded family contains **67 exact recipes / nine actual compositions**, including **four new compositions**. All 38 originals and all ten exact search-nominee entries remain represented. The 38 import provenance entries add 29 whole scenarios; identical scenarios are evaluated once, without collapsing identity-distinct recipes.

Each new composition changes two Essences on one character relative to prior `4dca57…`:

| New composition | Slot | Removed Essences | Added Essences |
| --- | ---: | --- | --- |
| `049002…` | 2 | Forest Spirit, Lumo Wisp | Venomous Spiderling, Viper |
| `230ac3…` | 7 | Blue Slime, Goblin Shaman | Venomous Spiderling, Viper |
| `32e3fc…` | 2 | Goblin Shaman, Treant Sapling | Spider Queen Royal Venom, Viper |
| `641411…` | 8 | Giant Bat, Rotfly Toad | Venomous Snake, Viper |

These are actual substitutions with fixed positions and canonical ordering. They remain poison-focused; the result does not establish broad archetype diversity.

## Complete-family screen and closed decision

The resource projection admitted the full screen at **380.38 seconds / 281.89 MB**. It completed all **8,576 fights in 427.59 native seconds**, with zero retries. Every composition's best profile was armor-and-health except the two nonwinning original references, whose lowest remaining-health results used ability-haste.

| Composition | Best wins / 128 | Mean engine seconds, all outcomes |
| --- | ---: | ---: |
| New `230ac3…` | 91 (71.09%) | 178.18 |
| New `049002…` | 88 (68.75%) | 178.29 |
| New `32e3fc…` | 75 (58.59%) | 182.10 |
| Prior leader `4dca57…` | 38 (29.69%) | 195.08 |
| New `641411…` | 18 (14.06%) | 191.19 |
| Prior alternative `ec0f25…` | 10 (7.81%) | 183.48 |
| Remaining three original compositions | 0 each | See saved rows |

Four compositions met the screening viability margin, but the strongest **91/128** exceeds the ceiling of **48/128**. This scope closed **`NoEligibleDiversityConfirmation`**. No confirmation panel or parity replay was allocated, and no gameplay value changed. These are screening observations, not confirmed acceptance intervals. Preserve this closed scope; the next scalar work is the separately declared [expanded-family calibration](Tower-Floor8-Expanded-Calibration-20260929.md).

The third-strongest team came from the unsuccessful restorer search. Its **75/128 armor-and-health** result illustrates why a failed search-level benchmark challenge must not discard its exact finalists or their frozen gear variants.

## Pacing, verification and evidence

A read-only review of the earlier 256-seed confirmation found the old leader's median duration **194 seconds**, 90th percentile **208 seconds**; victory-only median/p90 **192/205 seconds**. The old runner-up's median/p90 was **183/192 seconds**. Quantiles use nearest ranks; medians use the arithmetic midpoint. These are descriptive results from saved battle reports, with no new fights and no pacing acceptance threshold. The new screen's leading builds average about 178 seconds at the unchanged setting, but that does not establish a final calibrated duration or ordinary-player experience.

This scope completed **five phases / 13,344 study fights**, reserving **666 fresh values** and bringing the exclusion union to **905,944**. Native study time totaled **704.611 seconds**, excluding audits and wrapper overhead. All **658 immutable input/runtime pins** matched, all processes drained, and the Tower file remained byte-identical. **82 backend regressions, 14 Python tests and ten selection/reference/budget checks passed**, with three intentional opt-in skips.

| Artifact under `TestResults` | SHA-256 |
| --- | --- |
| `tower-floor8-search-challenge-evidence-20260929.json` | `91912258fb7e4d2862f12c34641cfa92064b3af588e2a173fcee8110b798e1d4` |
| `tower-balance-pass-floor8-search-challenge-expanded-screen-study-20260929/files.json` | `bb1dcf707139cda5c881a72228369f595ee7d01d44e20ba2e8319ced66bb2509` |
| `tower-balance-pass-floor8-search-challenge-expanded-screen-owner-20260929/seed-ledger.json` | `6c2b330e9b4b5e8cba851cba88cb10b16200d094d7a563e9da49638ce9cfada3` |
| `tower-floor8-search-challenge-final-regression-20260929.trx` | `a50d2c8e09e189f1ab962c41d40e60e144212821da7c1052021236eb7932db0b` |

The local composition analysis, historical pacing review, protocol snapshot, exact recipes and audits remain under ignored `TestResults`. No maintained search/combat code changed. Future floor-8 work must retain the **67-cell family**, not revert to the earlier 38-cell source or reimport these searches.

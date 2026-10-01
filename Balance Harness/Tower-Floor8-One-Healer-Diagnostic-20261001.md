# Floor 8: one-healer Restoration diagnostic — 1 October 2026

**Latest floor-8 result (1 October):** The [eight-item Restoration diagnostic](Tower-Floor8-Eight-Item-Diagnostic-20261001.md) completed **2,976 fresh diagnostic fights / 16 seeds** across all **186 recipes / nine compositions**, independently audited. The three new eight-item parties won **15/16, 13/16 and 16/16**, versus **6/16, 8/16 and 2/16** for their twelve-item controls and zero for baselines. This finds strong gear-limited routes, but the current isolated candidate is too easy in the sample. **No acceptance, confirmation or gameplay edit.** **274 Python checks and 324 backend cases pass / four intentional skips**, plus three native fixtures. Exclusions: **924,220**. Next: freeze this family and test one proposed offense factor **0.60** instead of **0.525**, keeping penetration, summon inheritance and original Miasma fixed. Native validation and a separately declared two-by-eight diagnostic are required; no new coefficient is implemented or follow-on seed allocated. Floor 8 remains unresolved; floor 7 remains the latest applied change.

The one-healer gear diagnostic completed, but it does not justify a larger one-healer balance trial: the six variants won **1, 0, 0, 1, 0 and 0 out of 16**. Their full two-healer counterparts won **5/16, 6/16 and 6/16**; all three baselines won **0/16**. Floor 8 remains unresolved and the isolated candidate remains rejected and unapplied.

Target: primary LL World Tower and offline Balance Harness. Continue the [rejected shared-penetration trial](Tower-Floor8-Shared-Penetration-20260930.md) by checking the six proposed one-healer gear variants. The earlier candidate remains rejected and unapplied.

## Frozen diagnostic protocol

Authenticate publication `f0213267a83c85854b49092b9b327a9d2615c46b8b15f7c2c6bac88714a8bd9c`, snapshot maintained documents and changed source, and preserve all **924,188** excluded seeds. The exact proposal is `TestResults/tower-floor8-one-healer-proposal-20261001.json`, already bound by that publication.

Retain all **177 original recipes / nine actual compositions** and append six exact gear substitutions: for each of the three leading Restoration compositions, specialize only healer slot 2 or only healer slot 7 with the six saved Restoration items. Keep all other equipment, native character identities, Essence order, party positions and progression budgets unchanged. The complete family has **183 recipes / 125 equipment-eligible recipes**. Eligibility remains at most eight specialized items on two characters. Keep the twelve-item healer ceiling outlier and every full-armor control.

First validate exact derivation and provenance. Add seed-free preparation support separately from existing accepted-catalog qualification. Build tests through `build/run-tests.ps1`, bind the new test DLL to the unchanged five qualified combat assemblies, and rerun the relevant filter. Prepare every recipe under both the original and isolated candidate catalogs. Retained candidate participants must equal the previously verified native projection; each new party must equal its saved baseline with exactly one participant copied from its full-Restoration parent. Original/candidate preparation may differ only in guardian Power, ArmorPenetration and MagicPenetration. Preparation runs no combat and reserves no seeds.

Create an authenticated native preparation archive with the original unchanged catalog and all 183 recipes. Independently check its manifest, settings, runtime, raw cells and zero fight/seed counts. Use that archive as the unchanged source for the diagnostic, redeclaring exactly `tower-kodoku-shared-penetration-v1`: offense ×0.525, penetration ×40, Venomspawn ArmorPenetration inheritance, and original Miasma. No additional candidate or coefficient is introduced.

Freeze **one 16-seed panel across all 183 recipes: 2,928 new diagnostic fights / 16 fresh reservations**. Use the ordinary bounded native execution/archive path. Its legacy request mode is `screen`; this enclosing protocol designates the run **diagnostic only**, and its generic per-panel assessment supplies no acceptance decision. No confirmation, application, extension, retries, replacement panel, coefficient search or outcome-dependent allocation is permitted. Stop after the declared panel or any failure, preserving all reservations and evidence. The six new recipes must not be used to reinterpret the closed 177-recipe screen.

Before allocating seeds, use the preceding completed shared-penetration batch 8 as the measured resource reference. Double its per-fight runtime and archive cost for 2,928 fights; require less than 672 seconds and 80% of 2 GiB, plus free disk for that doubled archive estimate and 2 GiB. Keep the **840-second native / 900-second owner / 2 GiB archive** limits. During execution, observe supervisor stdout only and leave active study/owner/control files unopened.

After completion, independently reauthenticate the full archive, recount every outcome, compare every prepared participant to the qualified native projection, check exact candidate/runtime/settings, and reconcile the complete seed union. Report all six new recipe counts beside their baseline and twelve-item parent controls using the same 16-seed panel; retain and report other controls. Sixteen samples are descriptive and cannot establish the required balance bounds. Any later acceptance study must be newly declared, retain the entire enlarged family, adjust intervals for that family and use fresh complete screening plus independent confirmation.

Update this report, current Tower status, continuation handoff and harness documentation. No live game catalog change, dungeon/acquisition/search redesign, migration, shared database action, configuration change or deployment is included.

## Completed result

The one-healer gear diagnostic completed, but it does not justify a larger one-healer balance trial: the six variants won **1, 0, 0, 1, 0 and 0 out of 16**. Their full two-healer counterparts won **5/16, 6/16 and 6/16**; all three baselines won **0/16**. Floor 8 remains unresolved and the isolated candidate remains rejected and unapplied.

All **183 recipes / nine actual compositions** ran on the same **16 fresh seeds**: **2,928 diagnostic fights**. The family retains all 177 earlier controls, adds the six exact one-healer variants and contains **125 equipment-eligible recipes**. One specialized healer uses six items; the twelve-item two-healer controls remain ineligible for the limited-equipment requirement.

| Restoration composition | Baseline | Slot 2 only: six items | Slot 7 only: six items | Both healers: twelve items |
| --- | ---: | ---: | ---: | ---: |
| A | 0/16 | 1/16 | 0/16 | 5/16 |
| B | 0/16 | 0/16 | 1/16 | 6/16 |
| C | 0/16 | 0/16 | 0/16 | 6/16 |

A/B/C are the three parents frozen in the proposal, ordered by their earlier two-healer counts of 48/128, 33/128 and 27/128. Those older counts did not enter this diagnostic. All full-armor and other equipment controls remain in the independent evidence. Gear variants are not additional compositions.

The paired panel shows a large descriptive gap between concentrating six Restoration pieces on one healer and equipping both healers with twelve pieces. Sixteen observations per recipe cannot establish the true rates or the mechanism. The next useful hypothesis is to share the allowed eight pieces across both healers, retaining every original control and the twelve-item ceiling outliers, before changing Kodoku again.

These are descriptive 16-sample counts. They establish neither the 10% minimum nor the 50% ceiling. The earlier 177-recipe screen remains closed and rejected. No confirmation or application followed this panel.

## Implementation and checks

Added `analysis/tower-one-healer-family.py` and the seed-free `--one-healer-family` option in `analysis/run-tower-balance-pass.py`. The option authenticates the exact saved proposal and admits only original-catalog preparation, with no mixed candidate or search modifications. It preserves raw Essence order, native character identities, party positions, all original recipes and every non-target equipment item. Existing accepted-catalog qualification and aggregate contracts remain unchanged.

`analysis/test-tower-one-healer-family.py` adds 14 checks for altered recipes, dropped controls, duplicate compositions, parent provenance, equipment limits and illegal execution modes. Together with the ten existing suites, **254 distinct Python checks pass**. `LL/tests/EssenceSystem.Tests/TowerOneHealerPreparationTests.cs` prepares all 183 recipes under both catalogs without combat. Every retained candidate party matches its previously verified native projection; each new party equals the baseline with exactly one saved healer participant substituted. Only guardian Power, ArmorPenetration and MagicPenetration may differ between catalogs.

**309 distinct backend cases pass / four intentional skips**, both in a fresh build and after binding the new test assembly to the unchanged five qualified combat assemblies. These are the same cases counted once. Both native preparation and diagnostic fixtures pass. Backend execution used `build/run-tests.ps1`. The generic seed-free preparation archive also preserves exact recipes, settings and original catalog hashes.

The independent collector reauthenticated the full archives, recounted all **2,928 outcomes**, and checked every prepared participant against the native projection. It verified candidate contents, runtime, settings, resource admission and seed accounting. The doubled resource estimate was **424.79 seconds**, below the 672-second admission cap; the 840-second native, 900-second owner and 2 GiB limits remained unchanged. Final exclusions: **924,204**, adding exactly 16 fresh reservations. No replacement panel or extra phase ran.

Changed maintained files: the new family helper and its Python tests, the preparation runner, the new native test, this report and six current Tower/harness documents. Local orchestration and immutable evidence live under TestResults. **No live game catalog change, migration, configuration change, shared database action or deployment.** No required verification remains blocked.

## Next work

An exact **three-variant / 186-recipe proposal** is saved at `TestResults/tower-floor8-two-healer-eight-item-proposal-20261001.json`. For each of the same three compositions, use Restoration on **MainHand, Chest, Head and Necklace for both healer slots 2 and 7**: eight specialized items on two characters, preserving the baseline Ring and Relic specializations and all other raw inputs. This borrows the previously tested floor-6 gear pattern as a hypothesis, not as proof for floor 8. The entire 183-recipe diagnostic family is retained; equipment-eligible recipes rise to 128, still nine actual compositions. The proposal is **not natively prepared, fought or allocated**. First validate exact derivation and prepare all 186 parties under both catalogs. Then implement a separate **two-by-eight-seed diagnostic contract** without relaxing existing acceptance guards. The just-completed native panel took 347.201s; a doubled 186-by-16 estimate is **705.79s**, above the unchanged 672s admission cap. Two complete eight-seed panels project **352.89s each**; recheck time, archive and disk admission before each. Declare at most **2,976 diagnostic fights / 16 fresh seeds** with no interim selection, extension, retry, confirmation or application. No new coefficient is proposed. Any future acceptance must use fresh full-family screening and independent confirmation.

Floor **8 remains unresolved**; floor **7 is the latest locally applied Tower change**. The remaining equipment checks cover **8–10 and 12–15**, followed by a final current-version floors 1–15 sweep. Preserve the repeating expected progression gear curve, approved Essence budgets and stronger equipment carried forward. Continue actual Tower balancing; no dungeon, acquisition or search redesign is included.

## Evidence

- Independent result: `TestResults/tower-floor8-one-healer-evidence-20261001.json`, SHA `7b5870de5219f03c204d5a17814d1806e47cd5cdb01c0e053ce0011dca00459a`.
- Frozen declaration: `TestResults/tower-floor8-one-healer-driver-20261001/declaration.json`, SHA `14aab6a74682aedb470486e2d967b95adc02478c91c0d97e1d1e0cb89d5ce93c`.
- Python checks: `TestResults/tower-floor8-one-healer-tests-20261001/completion.json`.
- Fresh/bound native verification: `TestResults/tower-floor8-one-healer-runtime-verification-20261001/completion.json`.
- Prepared source: `TestResults/tower-balance-pass-floor8-one-healer-preparation-study-20260929`.
- Diagnostic archive: `TestResults/tower-balance-pass-floor8-one-healer-diagnostic-study-20260929`.
- Publication: `TestResults/tower-floor8-one-healer-publication-check-20261001.json`.
- Commands: bundled Python with `-B -X utf8` for the eleven recorded suites, driver, collector and publisher; the recorded backend commands through `build/run-tests.ps1`; `git diff --check` and local Markdown link validation.

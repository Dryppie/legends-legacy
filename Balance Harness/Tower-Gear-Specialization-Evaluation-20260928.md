# Floor-15 gear specialization evaluation

**Decision: armor-and-health is the confirmed working gear benchmark for this fixed floor-15 team and budget.** On the separate confirmation panel it won **256/256 fights**, versus **83/256** for the unchanged gear: **+67.58 percentage points**, with 173 gained wins and no lost wins. The supported Essence search algorithm is unchanged. This is a demonstrated equipment-selection improvement, not evidence that a new search algorithm is better.

## Frozen comparison

The [previous fixed-team confirmation](Tower-Floor15-Team-Confirmation-20260928.md) retained reference 2, `6396cb05afa89d35c0c653b604e4000363fdf6cc12f63165a9cb5d7d36d3d4e6`. This study holds that team's complete ordered Essence recipes and character positions fixed and compares six predeclared equipment-specialization bundles with its original gear.

Conditions remain floor 15, 15 characters, ten level-1 unascended/unevolved Essences each, level 90, tier 2/rank 4, Uncommon Fine equipment, baseline attribute rolls, no styles, hypothetical ownership and an uncleared floor without contributions. Captured settings/content remain attribute rules 18, equipment release 4 and `healing-v1`.

Each replacement retains the item's archetype, rarity, occupied slot, tier, rank, quality and roll multiplier. The production evaluator checks equal target budgets for every substitution. Weapon archetypes and item counts stay fixed. All character, item and Essence instance IDs are pinned to the original gear so a specialization change does not also change actor identity. All Essence membership, ordering and progression are preserved.

The optional identity field required a new compiled Services.LL build. The study records both executions; it does **not** claim the new DLL hashes equal the previous study's. Before seed allocation, the new build reproduced **all 512 archived baseline combat-input hashes**, prepared every profile without combat, and reproduced the baseline's frozen actors exactly. Both comparison arms then ran under the same new captured executable. Input parity is not a full old-versus-new battle replay.

## Design and results

All seven teams received the same 128 discovery seeds. The best of the six alternatives was selected by wins, then lower mean guardian health, then ordinal profile ID. Its identity and discovery scores were saved before confirmation. It and the original baseline then received a separate 256-seed panel.

The predeclared confirmation required at least **13 net gained wins out of 256** (at least five percentage points observed) and an exact one-sided paired-binomial p-value at most **0.05**. Only the selected profile was tested on confirmation; discovery results were not pooled into that decision. This supports the selected fixed profile, not all six alternatives. The practical improvement threshold concerns the observed difference, not a lower confidence bound on the true effect.

| Gear profile | Changes | Discovery wins |
| --- | --- | --- |
| Original baseline | None | 47/128 (36.72%) |
| Precision | Precision weapons and rings, all characters | 31/128 (24.22%) |
| Ability haste | Haste weapons, all characters | 59/128 (46.09%) |
| Restorer specialization | Restoration on weapons, heads, chests, rings, necklaces and relics in positions 2, 7 and 12 | 73/128 (57.03%) |
| **Armor and health** | **Armor heads, chests and legs; health necklaces, all characters** | **127/128 (99.22%)** |
| Resistance and health | Resistance heads, chests and legs; health necklaces, all characters | 56/128 (43.75%) |
| Health and regeneration | Regeneration heads; health chests and necklaces, all characters | 73/128 (57.03%) |

| Separate confirmation | Wins | Clear rate | Mean guardian health remaining |
| --- | --- | --- | --- |
| Original baseline | 83/256 | 32.42% | 9.766% |
| **Armor and health** | **256/256** | **100% observed** | **0%** |

The paired contrast has 173 gained wins and zero lost wins: exact p = `1 / 2^173`, approximately **8.35e-53**. It meets both frozen criteria. The 100% sample result does not guarantee every future fight will be won. The statistical interpretation treats distinct fresh pseudorandom seeds as independent draws under the captured model.

The experiment used **896 discovery fights + 512 confirmation fights = 1,408 fights**, with no retries, early stopping or extensions. No historical results were pooled into confirmation. The baseline's earlier 144/512 result belongs to a different panel and is not the denominator for this comparison.

## Meaning and next step

Equipment specialization has a large demonstrated effect in this encounter at the same item budget. It explains why holding generic gear fixed can limit otherwise reasonable Essence teams. This bundle experiment does not isolate armor from necklace health, prove global optimality, establish practical item acquisition, or establish the best gear for every enemy.

Retain `armor-and-health` as the working gear benchmark **for this exact team, floor and captured budget**. The complete seed-free baseline and all six variants are preserved in `TestResults/balance/tower-gear-screen-20260928/variants.json`; select the entry whose `id` is `armor-and-health`. This record does not replace production defaults or alter authored game content.

The next implementation step should make these small, legal gear-profile choices reusable by the existing offline search flow, retaining this confirmed profile as a reference where applicable. Then use a bounded encounter-coverage screen to determine where the same profile helps and where clear rates leave room to compare Essence searches. Floor 15 at this budget is now near a measurement ceiling; spending another large campaign comparing Essence algorithms here would provide little separation. Keep the supported affinity search and its independent confirmation rule while adding the missing gear decision. Any later coverage or search evaluation needs its own declared scope and budget; none is launched by this study.

## Changed files and design decisions

Target: the offline Balance Harness in the primary game service.

- `LL/src/Infrastructure/Service/Services.LL/PowerRatings/EquipmentReferenceBuildFactory.cs`: adds optional `IdentityEquipment` to reference builds. Null is omitted from serialization and preserves existing identities. Provided identity selections must match the occupied slots; actual equipment still controls stats and legality.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessTowerLoadoutFoundationTests.cs`: verifies fixed character/item/Essence identities across actual gear changes, unchanged default serialization, and invalid actual-equipment rejection.
- `LL/tests/EssenceSystem.Tests/CanonicalEquipmentBuildFactoryTests.cs`: uses the existing shared content-root resolver so canonical checks also run from isolated repository-root artifact directories.
- `LL/tools/BalanceHarness/Fixtures/tower-gear-specialization-screen.json`: defines the six fixed, data-driven specialization bundles.
- `LL/tests/EssenceSystem.Tests/BalanceHarnessGearScreenTests.cs`: adds profile admission, equal-budget and identity checks, the exact confirmation gate, and the opt-in bounded experiment. It reuses existing archive and seed-allocation infrastructure.
- `Balance Harness/analysis/run-tower-gear-screen.py`: pins the source, profiles and tested runtime, freezes the design, and owns one no-build test process with a deadline. Existing output directories are rejected; there is no retry or resume.
- This report and `LL/tools/BalanceHarness/AFFINITY-SEARCH.md`: record the result, evidence and next implementation decision.

The two seed blocks are reserved before combat. A parent Pending record prevents partial allocation from appearing complete. Source/content/runtime pins, raw battle reports, input/cache identities and the declared selection are verified before sealing the result. This reuses the existing scientific contract rather than adding another search policy.

## Verification

**131 focused backend checks passed**, with one scientific opt-in skipped. The real study then passed **all seven checks** in its fixture, including the opt-in execution. Existing analyzer warnings remain. The initial test run exposed seven content-root lookup failures in existing canonical tests; the shared-resolver fix removed them in the final run. No requested verification remains blocked.

An independent Python readback authenticated **1,540 files**, recomputed both allocation journals, checked all **1,408 raw reports**, reconstructed the declared selection and exact paired contrast, and verified unchanged content/settings, preserved Essence recipes and specialization-only changes. That audit ran zero fights and wrote its receipt outside the sealed archive. Python CLI loading and `git diff --check` passed.

Executed commands (Python denotes the bundled runtime):

```powershell
./build/run-tests.ps1 -ArtifactsPath 'TestResults/tower-gear-build-20260928' -Filter 'FullyQualifiedName~BalanceHarnessGearScreenTests|FullyQualifiedName~BalanceHarnessTowerLoadoutFoundationTests|FullyQualifiedName~CanonicalEquipmentBuildFactoryTests|FullyQualifiedName~BalanceHarnessTowerBalanceSelectionTests|FullyQualifiedName~BalanceHarnessTowerTests|FullyQualifiedName~BalanceHarnessComparisonTests'
python -B -X utf8 'Balance Harness/analysis/run-tower-gear-screen.py' --help
python -B -X utf8 'Balance Harness/analysis/run-tower-gear-screen.py' --package TestResults/tower-gear-screen-owner-20260928 --output TestResults/balance/tower-gear-screen-20260928 --artifacts TestResults/tower-gear-build-20260928
python -B -X utf8 'TestResults/tower-gear-screen-owner-20260928/audit.py'
git diff --check
```

No migrations, production configuration changes, deployments or shared database operations. Earlier working-tree changes and the user's attribute tooltip edits were preserved.

## Retained evidence

**384 fresh combat seeds** were disjoint from **833,162 historical exclusions**. All were used. The complete exclusion union is now **833,546**, with no allocation collisions. Future scientific allocation must include the completed study's ledger and the full existing registry. Master: `2026092871`; domains: `tower-gear-specialization-screen-v1/discovery` and `/confirmation`.

The experiment completed in **143.16 seconds**; the owner completed in **145.95 seconds** and drained all eight processes. The sealed study occupies **142,437,624 bytes**, including its manifest, against limits of 840 fixture seconds, 900 owner seconds and 1 GiB. Ordinary engineering-test combats are separate from the 1,408 scientific fights.

- Study, exact recipes, raw reports and ledger: `TestResults/balance/tower-gear-screen-20260928/`.
- Declaration, process receipt, native log and independent readback: `TestResults/tower-gear-screen-owner-20260928/`.
- Verification log: `TestResults/tower-gear-verification-final-20260928.log`.
- Original baseline source: `TestResults/balance/tower-floor15-confirmation-final-20260928/`.

Result SHA-256: `8c18d9d0aa34d0c95f8f3cbe2f114a361b6cd22bd6eaeb02c25c1f00de684d19`.

Archive manifest SHA-256: `20b196238a4a51576d0e07202002aebe6882aa68a9260bbc80f576b176e7aa8a`.

Source manifest SHA-256: `af804279840211ec7a3895e365c98549eadbbe4ea0d84b8aa3d4e14780a9226b`.

The pinned historical receipts and combat archives are local ignored evidence. A clean checkout alone cannot reproduce this exact experiment; retain those archives with their hashes.

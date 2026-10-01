# Floor 9: summon defense correction — 1 October 2026

**Latest follow-on:** The [corrected Ni replay diagnosis](Tower-Floor9-Corrected-Ni-Diagnostic-20261001.md) completed **96 exact replays**, with zero new seeds. Limited gear now removes most copies but still trails full resistance sharply. Next is the proposed, unimplemented **offense ×0.95 / penetration ×40** candidate, with full-family fresh testing and independent confirmation required. No live edit; exclusions remain **926,348**. The results and next actions below preserve their historical checkpoint meaning.

**Latest follow-on:** The [corrected-runtime offense calibration](Tower-Floor9-Offense-Calibration-20261001.md) is complete: **14,208 fresh fights**, no selected setting and no live edit. At ×1.25, limited routes win 10/32 and 9/32 but full-resistance counterparts win 29/32 and 30/32; ×1.5 eliminates all limited-route wins. Exclusions are **926,348**. Next is the proposed, unexecuted 96-replay diagnosis of that corrected-runtime gear gap. All results and proposed actions below retain their historical checkpoint meaning.

## Completed result

**The production bug is fixed locally. Floor 9 still needs numerical balancing.** Current-rule summon defense inheritance now stays in rating units. Ni's copies keep **1,125 Health (10%)**, and their rounded inherited rating is **365**, giving approximately **55.09% mitigation** instead of about 79.99%. The real-floor native test confirms that a raw 1,000-point physical or magical hit deals **449 damage**. All 102 catalog files retain their pre-change hashes; only shared engine code changes gameplay behavior.

The corrected-runtime screen completed **18,944 fresh fights / 128 new shared seeds** across every one of the **148 recipes / five actual compositions**. It is **not accepted**: **all 148 adjusted upper ceilings fail**, although all five compositions satisfy the lower viability gate. The **115 eligible recipes win 62–120 / 128**. No confirmation was allocated. The correctness fix remains in place; failing balance is a reason to tune the encounter, not restore inflated defenses.

| Actual composition | Baseline wins / 128 | Best eligible wins / 128 | Full Resistance + Health wins / 128 |
| --- | ---: | ---: | ---: |
| reference-1 | 98 | 98 | 127 |
| reference-2 | 62 | 62 | 123 |
| reference-3 | 108 | 118 | 127 |
| Saved lineup 5b6c297c… | 111 | 120 | 128 |
| Saved lineup 4a9f8b9f… | 111 | 111 | 128 |

The buggy 10%-Health runtime previously produced zero wins for every eligible recipe. These are separate fresh panels, not paired counterfactual battles or pooled estimates. The revised result demonstrates the practical effect of the correctness fix and does not establish final balance.

**Next: a bounded Ni damage/guardian-offense calibration on the corrected runtime.** Keep 10% copy Health, correct inherited defenses, copy count, existing mechanics, approved gear/progression, all 148 recipes and the same simultaneous acceptance rules. Select and freeze the next numerical candidate before new allocation. No follow-on coefficient, acceptance declaration or seeds have been created. Do not rerun this completed screen or return to the rejected 5% Health candidate. Floors 10 and 12–15 and the final current-version 1–15 sweep remain after floor 9.

## Verification and limitations

**766 backend cases pass**, including **40 new inheritance cases**, the expanded ability/attribute suites and existing Tower safeguards. Four owned-study tests intentionally skip during ordinary regression. Seven seed-free preparation probes retain exactly the same participant lists; the newly qualified source preserves all 148 raw recipes, settings and catalogs under freshly compiled production assembly hashes. **56 Python checks pass** in the aggregate, limited-equipment, resource-owner and status suites. All four native screening fixtures and the seed-free source-preparation fixture pass.

The first expanded run found two errors in the new zero-defense assertions: they selected both Damage and Death entries. The assertion now selects Damage entries explicitly, and both tests pass. It also found one existing failure in `AbilitySystemTests.Json_catalog_behavior_manifest_observations_pass`: Kharad's Crushing Verdict behavior manifest expects at least two Vulnerable stacks on hostile-1 but observes zero. The same test and message were reproduced on the preserved pre-fix runtime. **That existing diagnostic failure remains unresolved and is recorded separately; it was excluded from the repaired 766-case regression selection.** No passing claim includes it. Evidence: `TestResults/tower-floor9-summon-defense-baseline-20261001/completion.json`.

Independent reconstruction verifies every saved outcome, seed schedule, prepared participant, equipment gate, interval, stopping decision, resource receipt and native cleanup. Native screen time totals **480.685 seconds**; archives total **500,080,677 bytes**. All batches fit the declared limits, with zero retries. Final exclusions are **926,252**. No migrations, configuration changes, database actions or deployment. Unrelated design-system work remains untouched.

A separate raw-report check confirms all **170,496 recorded copies** have the original **1,125 Health** and zero damage done themselves. Receipt: `TestResults/tower-floor9-summon-defense-summon-check-20261001.json`.

Independent evidence: `TestResults/tower-floor9-summon-defense-evidence-20261001.json`, SHA **`8d3ee9a070e8617448791012b2b7640667fdbcf625b6ee6c044b0839093db08b`**. Declaration SHA: `0c556db59d366af63e9c64e623fb01679c6c4dffe1ac6e4338236c8478dd5ce5`. The corrected runtime is `TestResults/tower-floor9-summon-defense-runtime-20261001-repair1`; its verification receipt is `TestResults/tower-floor9-summon-defense-runtime-verification-20261001-repair1/completion.json`. The original protocol is preserved in `TestResults/tower-floor9-summon-defense-driver-20261001/protocol.md`. Publication: `TestResults/tower-floor9-summon-defense-publication-20261001/completion.json`.

## Original scope and frozen protocol

Target: the LL combat engine and offline World Tower balancing. Correct the current-rules summon defense unit boundary identified during the rejected [5% copy-Health trial](Tower-Floor9-Ni-Copy-Health-20261001.md).

## Correction and frozen verification

`RuntimeCombatant.GetAttribute(Armor/Resistance)` resolves to current defense ratings. Summon creation previously wrote those numbers under authored percentage keys; the summon constructor converted them again. Ni's 365.36816 rating (about 55.1% mitigation) therefore became about 80% mitigation on his copies.

When current-rule summons inherit defense ratings, write canonical ArmorRating/ResistanceRating values. Convert any authored percentage base/minimum into that same unit before combination. Preserve existing integer rounding, legacy-version behavior, static authored percentages, scaling from non-defense attributes, all other attributes and summon multipliers. No Ni-specific condition belongs in the engine fix. Ni is the only summon in the current catalog with this defense-inheritance definition.

Keep live copy Health at **10%**, all 102 catalog files unchanged, and the complete Ni kit. Do not combine this correctness fix with the rejected 5% coefficient. Run native current/legacy inheritance regressions, the full ability-system and attribute suites, prior Tower guards and seven seed-free preparation probes through `build/run-tests.ps1`. Compile a fresh production runtime and record its assembly hashes; do not bind the new tests to old combat assemblies. Preserve the previous runtime and reports as historical evidence.

Prepare the exact **148 recipes / five actual compositions / 115 eligible recipes** against the corrected runtime with zero combat or new seeds. Require byte-equivalent raw recipes, identical prepared participants, unchanged settings and catalogs. Original Essence order, actor identities, equipment order, party positions and approved expected-progression gear remain fixed. Floor 9 uses ten level-40 characters, five Essences each, tier-1 Unique / Exceptional / Rank-4 gear, fixed roll 1 and no styles.

## Prospective screen

Use the existing unchanged-catalog `tower-balance-limited-resistance-aggregate-v1` contract on the newly prepared corrected-runtime source. Freeze both phases and all code/runtime/source pins before allocation. Screen with four batches of 32 fresh shared seeds: **128 samples per recipe / 18,944 fights**. Only a complete passing screen admits an independent equal-size confirmation. Maximum **37,888 fresh fights / 256 reservations**. Exclude all **926,124** prior seeds; no historical pooling, interim decision, extension, retry, replacement seed or dropped control.

Keep the same approximate simultaneous Bonferroni-Wilson gate (family 148, alpha .05): at least two distinct eligible compositions with lower bounds at least 10%, and every recipe's upper bound at most 50%. Eligibility means at most eight specialized items on at most two characters. Integer acceptance gates: at least **25 qualifying wins**, at most **43 ceiling wins**, out of 128.

Use the closed 5% trial's first completed batch as initial resource reference, then the preceding completed batch within each phase. Require doubled runtime/archive projections below 672 seconds and 80% of 2 GiB before each batch; free disk must cover projected remaining archives plus 2 GiB. Native and owner limits remain 840/900 seconds, maximum 20,000 fights and 2 GiB per batch. Runtime has changed, so these are admission estimates; actual bounds remain enforced. Observe supervisor stdout only while native work is active.

Independently reconstruct all raw outcomes, preparations, intervals, equipment eligibility, reservations, stopping decisions and process cleanup after closure. If both phases pass, require all four confirmation batches' full native input/replay parity (18,944 inputs / 592 replays, no new seeds). A rejected screen leaves floor 9 unresolved; retain the correctness fix and plan subsequent numerical tuning separately. No deployment, migration, configuration change or database action.

Previous applied floor-8 data remains intact. Its old acceptance evidence is historical after a runtime change; retain it and use the final current-version 1–15 sweep for end-to-end acceptance.

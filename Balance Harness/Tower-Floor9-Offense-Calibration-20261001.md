# Floor 9: corrected-runtime offense calibration — 1 October 2026

**Latest follow-on:** The [corrected Ni replay diagnosis](Tower-Floor9-Corrected-Ni-Diagnostic-20261001.md) completed **96 exact replays**, with zero new seeds. Limited gear now removes most copies but still trails full resistance sharply. Next is the proposed, unimplemented **offense ×0.95 / penetration ×40** candidate, with full-family fresh testing and independent confirmation required. No live edit; exclusions remain **926,348**. The results and next actions below preserve their historical checkpoint meaning.

## Completed result

**14,208 fresh fights / 96 reservations completed. No setting was selected or applied. Floor 9 remains unresolved.** All three panels retain the complete 148 recipes, five actual compositions and 115 equipment-eligible recipes. The independent audit reconstructed every raw outcome, native preparation, schedule, catalog, runtime and resource receipt.

| Offense factor | Strongest recipe / 32 | Best eligible recipe / 32 | Distinct eligible compositions reaching 6/32 |
| --- | ---: | ---: | ---: |
| 1.25 (+25%) | 31 | 10 | 2 |
| 1.50 (+50%) | 11 | 0 | 0 |
| 2.00 (+100%) | 0 | 0 | 0 |

At 1.25, the two best limited-gear compositions win **10/32 and 9/32**. Their exact full Resistance + Health counterparts win **29/32 and 30/32**; their baselines win **2/32 and 3/32**. The strongest overall setup, A with full Armor + Health, wins **31/32**. At 1.5, **all 115 eligible recipes lose every fight**, although the strongest full-resistance setup still wins 11/32. At 2.0, every recipe loses every fight.

These results show substantial gear sensitivity in the tested range. They do **not** prove that every intermediate scalar would fail. However, the endpoint tradeoff does not justify choosing an untested midpoint or starting acceptance. No candidate met the frozen empirical selection rule; no confirmation or application occurred, and diagnostic observations contribute nothing to later acceptance counts.

**Next: explain the corrected-runtime gear gap with 96 exact historical replays**, using the closed 1.25 panel. Compare the two leading limited-gear compositions with their exact baseline and full-resistance counterparts on the first sixteen declared seeds. A specializes slots **2+4**; B specializes **3+9**. Trace early casualties, copy deaths, permanent Power gains, Ninth Seal damage, recovery and sustained party output before selecting a targeted encounter adjustment. The old replay diagnosis used the pre-fix summon defense conversion and is historical evidence only.

The unexecuted proposal is `TestResults/tower-floor9-corrected-pressure-proposal-20261001.json`, SHA **`a7bec338180ac7474e81e34b1697315104e5ede876c7f00ef032fd66963bda61`**. It allocates zero seeds/fights/replays. Implement a separate strict corrected-source admission path; preserve the old diagnostic's source pins and semantics. Require complete saved-report parity after removing only the event log. Keep the 840-second / 2-GiB overall and 60-second / 16-MiB per-replay limits, measured admission, and zero retries/extensions. No new ability coefficient or live setting is selected.

## Verification and evidence

**64 Python checks pass**, including eight new calibration safeguards. The new native preparation test passes in both a fresh build and a test assembly bound to the previously qualified corrected production DLLs: **444 original/candidate pairs**, with only Ni's prepared Power changing. All three native combat-study fixtures pass. The preceding **766 backend passes / four intentional skips** were authenticated and reused, not rerun. Their separately recorded pre-existing Kharad behavior-manifest failure remains unresolved and is excluded from that passing count.

The first sandboxed build could not read the existing NuGet configuration. A fresh authorized build completed successfully; the failed build receipt is retained, with zero fights or seeds. No combat operation was retried. Native study time totals **292.686 seconds**, archived studies total **361,919,939 bytes**, and final exclusions are **926,348**. All 102 live catalogs and the production engine remain unchanged by this calibration. The defense correctness fix and original 10% / 1,125-HP copies remain in place. No migration, configuration change, database action or deployment.

Independent evidence: `TestResults/tower-floor9-offense-evidence-20261001.json`, SHA **`30104b6a0ed4c2a87373886f2da0cde907e9e9568ed55ad310acae8ec640e635`**. Frozen declaration SHA: **`849915d91e37ef25696665a2cd6c326478585ebcf31105ab165acb350196c35d`**. Driver: `TestResults/tower-floor9-offense-driver-20261001`. Bound runtime: `TestResults/tower-floor9-offense-bound-runtime-20261001-authorized`. Publication: `TestResults/tower-floor9-offense-publication-20261001/completion.json`.

After floor 9, floors 10 and 12–15 and the final current-version 1–15 sweep remain. Floor-8 accepted catalog changes stay applied. This calibration does not close any floor's balance acceptance.

## Frozen scope

Ni remains too easy after the summon defense correctness fix: every one of 148 recipes exceeded the adjusted ceiling. Calibrate guardian offense with the existing isolated scalar owner. Keep original 10% copy Health, correct defense inheritance, the complete kit, all raw recipes and expected progression equipment unchanged.

Test exactly **1.25, 1.5 and 2.0 times** current offense, in that order: **5.8795166015, 7.0554199218 and 9.4072265624**, from **4.7036132812**. These spaced increases test the range after a large difficulty change without assuming a linear win-rate response. They are diagnostic candidates, not recommended live values.

Each setting receives **32 fresh shared seeds across all 148 recipes**, preserving all five actual compositions and 115 eligible recipes. All three settings are mandatory: **14,208 fights / 96 new reservations**. No interim selection, extra settings, extensions, retries, confirmation, application or historical outcome pooling. Exclude all **926,252** preceding seeds. Panels are disjoint; this is not a paired-seed comparison.

After all settings finish, select the lowest factor with **at most 12/32 wins on every recipe** and **at least 6/32 wins on eligible recipes from two distinct compositions**. Eligibility remains at most eight specialized items on at most two characters. These empirical margins identify a candidate for a later fresh study; they do not replace the simultaneous acceptance rule (two distinct eligible lower bounds ≥10%, all upper bounds ≤50%). If no setting qualifies, report no selection and retain the complete dose-response evidence.

Before allocation, authenticate the preceding publication, all catalogs and the corrected production assemblies; preserve mutable file snapshots; verify native preparation of every recipe under all three settings with zero fights. Only Ni's prepared Power may change. Keep actor identity, party slots, raw Essence/equipment order, settings and every other participant field exact. Bind the new preparation test to the already qualified corrected production assemblies.

Reuse authenticated 766-case backend verification (four intentional skips and the separately documented pre-existing Kharad manifest failure). Run new native preparation plus relevant Python checks. No production source edit is planned. Existing owner and scalar guards remain in use.

Use the corrected screen's largest-runtime batch as the initial resource reference, then the preceding completed batch. Before each allocation require doubled projected runtime below 672 seconds, doubled archives below 80% of 2 GiB, and free disk covering remaining projected archives plus 2 GiB. Native/owner limits remain 840/900 seconds, 20,000 fights and 2 GiB. Observe supervisor stdout only while a native owner runs.

Independently recount every raw outcome and verify every prepared participant against the seed-free native preparation, exact schedules, catalogs, settings, runtime, reservations, resource admission and stopping rule. Do not apply a diagnostic setting. Preserve floor-8 catalog changes and the shared defense fix. No migration, configuration, database action or deployment.

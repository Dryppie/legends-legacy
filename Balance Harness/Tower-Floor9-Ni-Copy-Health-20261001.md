# Floor 9: isolated Ni copy-Health trial — 1 October 2026

**Latest follow-on:** The [corrected Ni replay diagnosis](Tower-Floor9-Corrected-Ni-Diagnostic-20261001.md) completed **96 exact replays**, with zero new seeds. Limited gear now removes most copies but still trails full resistance sharply. Next is the proposed, unimplemented **offense ×0.95 / penetration ×40** candidate, with full-family fresh testing and independent confirmation required. No live edit; exclusions remain **926,348**. The results and next actions below preserve their historical checkpoint meaning.

**Latest follow-on:** The [corrected-runtime offense calibration](Tower-Floor9-Offense-Calibration-20261001.md) is complete: **14,208 fresh fights**, no selected setting and no live edit. At ×1.25, limited routes win 10/32 and 9/32 but full-resistance counterparts win 29/32 and 30/32; ×1.5 eliminates all limited-route wins. Exclusions are **926,348**. Next is the proposed, unexecuted 96-replay diagnosis of that corrected-runtime gear gap. All results and proposed actions below retain their historical checkpoint meaning.

**Latest follow-on:** The [defense-unit correction](Tower-Floor9-Summon-Defense-20261001.md) is now implemented and verified at the original 10% copy Health. A fresh complete screen still fails all 148 difficulty ceilings; exclusions are 926,252. Next is numerical damage tuning on the corrected runtime. References below to fixing the conversion describe the previous checkpoint.

## Completed result

**Not accepted.** All four screening batches completed: **18,944 fights / 128 fresh shared seeds per recipe**, all 148 recipes, five actual compositions and 115 equipment-eligible recipes. An independent collector reconstructed every raw outcome, seed schedule, prepared participant, eligibility decision and interval. All five compositions qualify on the lower viability gate, but **all 148 recipes fail the adjusted upper difficulty ceiling**. Eligible recipes range from **48/128 to 124/128**; multiple fully specialized controls win **128/128**. No confirmation, local application or live catalog change occurred.

| Actual composition | Baseline wins / 128 | Best eligible wins / 128 | Full Resistance + Health wins / 128 |
| --- | ---: | ---: | ---: |
| reference-1 | 69 | 69 | 122 |
| reference-2 | 48 | 48 | 122 |
| reference-3 | 107 | 124 | 128 |
| Saved lineup 5b6c297c… | 101 | 119 | 128 |
| Saved lineup 4a9f8b9f… | 105 | 105 | 128 |

The original unchanged-Ni screen had zero wins for every eligible recipe. Lower copy durability therefore has a large measured effect under the current engine, but 5% is too easy under the declared gate. This does not justify interpolating another coefficient while the newly discovered defense conversion remains wrong.

**Next: correct summon defense units with the original 10% copy Health.** Preserve current-rules ArmorRating/ResistanceRating through summon creation instead of converting those ratings again as legacy percentages. Retain legacy rules and authored static-defense semantics; verify actual Ni defense inheritance, both hit types, copy count/inert behavior, Strike/Seal scaling, health swaps and permanent-Power gains. Ni is the only authored summon currently using Armor/Resistance inheritance. Do not combine the correctness fix with the rejected 5% candidate. Qualify the corrected runtime before any new study, and test the same full family on fresh seeds; this trial's wins cannot be pooled into a corrected-runtime result.

**Verification:** 449 Python cases pass (21 suites); 464 distinct backend cases pass in both the fresh build and the bound runtime, with four intentional skips. All four native study fixtures pass. The initial two new fixture failures are preserved and explained below; the repaired build passes. The seed-free preparation check verifies all 148 original participant lists are identical. Production assemblies stayed unchanged throughout this experiment. All 102 live catalogs retain their original hashes. Native screening time totals **487.267 seconds**; archives total **500,441,571 bytes**. Every batch passed its resource and cleanup checks. Final exclusions: **926,124**. No outstanding failed verification command, migrations, configuration changes, database actions or deployment.

An additional raw-report check confirms **all 170,496 copies** across the 18,944 fights have **563 Health** and zero damage done themselves. This verifies the isolated candidate actually reached combat. Its receipt is `TestResults/tower-floor9-ni-copy-health-summon-check-20261001.json`.

The independent evidence is `TestResults/tower-floor9-ni-copy-health-evidence-20261001.json`, SHA **`c7b012c2574ea772b52fdc161c71fffe0cc17feadc685df063be1a683a048649`**. Declaration SHA: `96e94c170ecbd8b9c304e006305de92d1d6207691b6613ce737f8cefcccbebad`. The original protocol is preserved in `TestResults/tower-floor9-ni-copy-health-driver-20261001/protocol.md`; the repaired native verification is `TestResults/tower-floor9-ni-copy-health-runtime-verification-20261001-repair1/completion.json`. Publication closure is `TestResults/tower-floor9-ni-copy-health-publication-20261001/completion.json`.

## Frozen screening protocol

Target: LL World Tower and its offline Balance Harness. Test the frozen proposal `TestResults/tower-floor9-ni-copy-health-trial-proposal-20261001.json`, SHA `1e3e01d1cf847677a68f28e0565feb1767d06539cec7175e6b4aaa729b92f001`.

Only change `niCopy` MaxHealth inheritance from 10% to 5% and the matching Ninefold description in isolated content. Preserve copy count, inert behavior, Armor/Resistance inheritance, Strike and Seal coefficients and timing, health swaps, permanent initial-Power gains, guardian multipliers and all other catalogs. Preserve the accepted floor-8 midpoint and Venomspawn inheritance.

Retain the exact 148 recipes, five actual compositions and 115 eligible recipes from the closed floor-9 limited-resistance screen. Eligibility allows at most eight specialized items on at most two characters. Raw Essence order, character identities, equipment order and party positions stay unchanged. Ten level-40 characters, five Essences each, tier-1 Unique / Exceptional / Rank-4 equipment, fixed roll 1, no styles.

Use the separate `tower-balance-ni-copy-health-aggregate-v1` contract. Freeze both phases before allocating seeds. Screen with four batches of 32 fresh shared seeds: 128 samples per recipe and 18,944 fights. Only a complete passing screen admits an independent equal-size confirmation. Maximum 37,888 new fights and 256 new seeds. All 925,996 existing exclusions remain excluded. No pooling historical outcomes, interim acceptance, retries, replacement seeds, extension or dropped controls.

Acceptance uses approximate simultaneous Bonferroni-Wilson intervals, family 148, alpha .05: at least two distinct eligible compositions with lower bounds at least 10%, and every recipe's upper bound at most 50%. The integer gates are 25 qualifying wins and at most 43 wins per 128. Descriptive diagnostics cannot establish acceptance.

Before allocation, pass candidate isolation, aggregate, equipment and native mechanics guards; prepare all 148 recipes without combat and verify original participants are identical. Build and test through `build/run-tests.ps1`, bind the new test assembly to the qualified runtime, and verify all five production assembly hashes. Preserve current catalogs throughout testing.

Before every batch, re-admit from measured completed output with doubled native time/archive projections below 672 seconds and 80% of 2 GiB; require free disk for doubled remaining archives plus 2 GiB. Native/owner limits remain 840/900 seconds, 20,000 fights and 2 GiB per batch. Initial doubled estimate: 231.88 seconds and 247,145,964 bytes for 4,736 fights. Observe supervisor stdout only during native work.

Independently recount all raw outcomes, full seed schedules, prepared participants, recipe eligibility, intervals and stopping decisions after closure. Require all four confirmation batches' native parity (18,944 inputs and 592 complete replays, no new seeds) before considering local application. No application based on screening alone. No migrations, configuration changes, database actions or deployment.

If rejected, close the trial and report the full result before proposing another change. Do not silently tune a second coefficient under this declaration.

## Defense-unit issue discovered before allocation

The first native fixture build passed 462 cases and failed two new defense expectations (four intentional skips). The fixture expected inherited mitigation of 40%/30%, but copies mitigated about 80% of both hit types. Inspection identifies a pre-existing current-rules unit conversion: `RuntimeCombatant.GetAttribute(Armor/Resistance)` resolves to a rating, `CreateSummonAttributes` writes that rating under the old Armor/Resistance key, and the summon constructor converts it again as a legacy percentage. Thus preserving the catalog's inheritance coefficients preserves existing inflated mitigation, not equal effective defenses.

The repaired fixture compares baseline and candidate behavior directly, while checking nine inert copies with 5% Health and unchanged damage responses. It does not endorse the existing conversion as correct. The frozen Health-only experiment remains an isolated test against this runtime; its outcomes cannot establish correctness of defense inheritance. Resolve and verify this shared unit-boundary defect before final floor-9 balance acceptance or carrying its results into the final sweep. Preserve the failed build receipt; it allocated no trial seeds.

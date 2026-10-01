# Floor 9: Ni equipment-dependence diagnosis — 1 October 2026

**Latest follow-on:** The [corrected Ni replay diagnosis](Tower-Floor9-Corrected-Ni-Diagnostic-20261001.md) completed **96 exact replays**, with zero new seeds. Limited gear now removes most copies but still trails full resistance sharply. Next is the proposed, unimplemented **offense ×0.95 / penetration ×40** candidate, with full-family fresh testing and independent confirmation required. No live edit; exclusions remain **926,348**. The results and next actions below preserve their historical checkpoint meaning.

**Latest follow-on:** The [corrected-runtime offense calibration](Tower-Floor9-Offense-Calibration-20261001.md) is complete: **14,208 fresh fights**, no selected setting and no live edit. At ×1.25, limited routes win 10/32 and 9/32 but full-resistance counterparts win 29/32 and 30/32; ×1.5 eliminates all limited-route wins. Exclusions are **926,348**. Next is the proposed, unexecuted 96-replay diagnosis of that corrected-runtime gear gap. All results and proposed actions below retain their historical checkpoint meaning.

**Latest follow-on:** The [defense-unit correction](Tower-Floor9-Summon-Defense-20261001.md) is now implemented and verified at the original 10% copy Health. A fresh complete screen still fails all 148 difficulty ceilings; exclusions are 926,252. Next is numerical damage tuning on the corrected runtime. References below to fixing the conversion describe the previous checkpoint.

**Follow-on result:** The [5% copy-Health trial](Tower-Floor9-Ni-Copy-Health-20261001.md) completed and failed all 148 adjusted ceilings. No live edit. Exclusions are now 926,124. Next is the discovered summon defense-unit correction at the original 10% Health, before further tuning. The proposed experiment described below is historical and has been executed.

Target: the primary LL game's World Tower and offline Balance Harness. Explain the [closed limited-equipment screen](Tower-Floor9-Limited-Resistance-20261001.md): all 115 eligible recipes lost all 128 fights; the two original fully specialized leaders won 30/128 and 18/128. Floor 9 remains unresolved.

## Completed findings

**Completed: 96 exact historical replays / 244,278 independently audited events. Floor 9 remains unresolved.** Every replay matched its complete saved combat report; all 96 native attempts succeeded. No new acceptance fights, seed reservations or gameplay changes occurred.

**All nine copies remained alive at every first original-party casualty.** Ninefold Strike caused 60 of those first deaths (54 copy-repeat hits and six main hits), while Ninth Seal caused 36. The preceding damage occurred on the same tick in all 96 cases. Median first-casualty time was 16 seconds in each equipment group; some B fights lost a character at eight seconds. Resistance specialization does not remove the opening physical threat.

The stronger separation appears in copy removal. Across the 32 paired baseline battles, parties killed **zero copies**. The eight-item variants killed **nine copies total** (A: four; B: five). The forty-item variants killed **225 copies** (A: 123; B: 102). Each copy starts with **1,125 Health**, totaling **10,125 copy Health**, plus inherited defenses. Keeping copies alive preserves both Ninefold Strike's repeated physical hits and Ninth Seal's party-wide magical damage.

These results describe the selected sixteen-seed sample; its win counts are not new acceptance estimates and are not pooled with the completed 128-seed screen.

| Composition / equipment | Diagnostic wins | Copy deaths across 16 battles | Ninth Seal share of party Health damage | Mean Ni Health remaining |
| --- | ---: | ---: | ---: | ---: |
| A/baseline | 0/16 | 0 | 69.3% | 51.78% |
| A/limited-resistance | 0/16 | 4 | 62.1% | 43.51% |
| A/full-resistance | 5/16 | 123 | 46.5% | 15.22% |
| B/baseline | 0/16 | 0 | 68.3% | 52.82% |
| B/limited-resistance | 0/16 | 5 | 62.2% | 47.77% |
| B/full-resistance | 2/16 | 102 | 47.9% | 23.56% |

Ninth Seal supplies **62.1% and 62.2%** of Health damage to the two limited-equipment parties, versus about **69%** for baselines and **47%** for full specialization. This is a whole-profile comparison. Combined party MaxHealth changes **26,572 → 27,520 → 31,312** from baseline to limited to full specialization; combined ResistanceRating changes **1,091.4 → 1,676.04 → 4,014.60**, while regeneration falls **994.4 → 935.94 → 702.10**.

Opening damage to copies before 16 seconds is nearly identical across gear profiles: approximately **2,572–2,603 Health per battle**. Baseline output to Ni then becomes very small during seconds 32–48 as the party is lost; full specialization retains substantially more output in that window and removes many copies. This is not a claim that every party's damage rate falls immediately at its first death: the event-order rates vary, and full-specialization damage to Ni increases later as copies disappear.

**No One Among Nine health swap occurred in any of the 96 replays.** Ni received no logged healing and only **156 total Health regeneration** across all replays. Recovery therefore does not explain this sample's gap. All **234 copy deaths** corresponded to **234 permanent-Power notifications**, each adding **83 Power** from the original 5% initial-Power rule. These flat increments do not describe current Power after other modifiers. Copy removal reduces copy-driven attack scaling but retains this compensating increase in Ni's own pressure.

## Next isolated experiment

Test **niCopy MaxHealth inheritance 10% → 5%** of Ni's MaxHealth, with the matching Ninefold description update. Preserve all nine copies, inherited Armor/Resistance, inert behavior, Strike and Seal coefficients, Power gained on copy deaths, health-swap rules, guardian multipliers, raw parties and progression budgets. This is one proposed copy-durability change, not an accepted balance fix. Saved damage totals cannot predict counterfactual deaths, target allocation or win rates.

The hypothesis is that lower copy durability lets ordinary parties reach the existing mechanic that weakens Strike/Seal before losing most of their party. Earlier permanent-Power gains may help retain pressure on fully specialized parties. Both effects must be tested. Do not replace this with a health-swap nerf or a broad offense reduction without further evidence.

The frozen unimplemented, unallocated proposal is `TestResults/tower-floor9-ni-copy-health-trial-proposal-20261001.json`, SHA **`1e3e01d1cf847677a68f28e0565feb1767d06539cec7175e6b4aaa729b92f001`**. Add a separate strict two-catalog candidate and four-by-32 aggregate/native contract, with copy preparation and unchanged-mechanic guards, before allocating anything. Retain **all 148 recipes / five compositions / 115 equipment-eligible recipes**. Screen with **128 fresh shared seeds / 18,944 fights**, then run an independent equal-size confirmation only after a full pass. Maximum **37,888 fights / 256 new seeds**. Keep the original simultaneous family gates: at least two eligible compositions with lower bounds at least 10%, every recipe's upper bound at most 50% (**25–43 wins per 128**). No pooling, extension, retry, discarded control or live catalog edit during testing. Require complete native parity before any local application.

The first proposed 32-seed batch's measured doubled estimate is **231.88 seconds / 247,145,964 bytes**. Re-evaluate admission before each batch and retain the existing native/owner limits. This estimate does not authorize skipping native candidate verification or substituting historical outcomes.

## Verification and closure

**58 fresh Python checks and five fresh native Ni tests pass.** The five backend tests ran through `build/run-tests.ps1 -NoBuild` using the already verified runtime. The preceding **442 backend passes / four intentional skips** were authenticated and reused, not counted as fresh executions. Dedicated new coverage includes 29 Ni diagnostic cases; the other 29 checks exercise shared report/event/process handling. No production C#, catalog, search or acquisition code changed.

The owner independently recounted 192 saved outcomes before replay, then produced **96/96 complete-report matches with zero retries**. Independent closure reauthenticated every source/runtime binding, decompressed and checked each log byte-for-byte, recounted every event, checked native process cleanup and reconciled all original recipients, Ni and his copies. Native process time totals **129.718 seconds**; archived output is **42,505,187 bytes**; largest raw log is **5,684,304 bytes**, below the unchanged 16-MiB limit. All required verification completed. No migrations, application configuration changes, database actions or deployment.

Final exclusions remain **925,996**. Independent evidence SHA: `6d7e3ddb46fb0bd3eb5e89a5fc2909d5ce8aad34e07212eebc205982f18c5e7c`; owner manifest SHA: `303e856badcaa39babc54900f5e43a56ebe3dae6592fd1d9f2f65032d834e7c1`; mechanism review SHA: `2721707ac069afbad10d7f3ece90a334524570f0cbac9c2de351b7327118fc71`. Publication closure is `TestResults/tower-floor9-ni-publication-check-20261001.json`. The original protocol is preserved in the immutable owner's `protocol.md`; historical bindings to the evolving report resolve to that copy.

## Frozen protocol

Use the existing proposal `TestResults/tower-floor9-ni-pressure-diagnostic-proposal-20261001.json`, SHA `31d196272058c1f03a1e7861c272171c615732e379d858e087cf4a854c10473e`. Authenticate the complete first-batch archive, manifest `62b8b1d408209997bb5beab90dc09b20412d02bf612caee0554754aa3bf6ae77`, and the preceding publication receipt `7f1ec3e8638440873d29009aedf3bfaef8dda2827ec3de5e56452c528b90d0f2`. Preserve all **925,996 excluded seeds** and all **102 live catalog files**.

Replay exactly **96 saved battles**: baseline, selected eight-item Resistance + Health and original forty-item Resistance + Health for each of two original leader compositions, on the first sixteen declared seeds of the first completed screening batch. Partial equipment uses **A slots 3+4 and B slots 4+8**. These pairs were selected from the complete closed screen by wins, then lowest mean remaining guardian Health, then raw recipe ID; the replay sample is explanatory, not acceptance evidence. Retain all 148 recipes/five actual compositions. Recount all 192 saved first-batch outcomes of the six selected recipes. Preserve raw Essence order, identities, equipment order and party positions.

No new acceptance fight, new seed, gameplay change, confirmation, extension or candidate application belongs to this diagnosis. Keep ten level-40 characters with five Essences each, tier-1 Unique / Exceptional / Rank-4 gear, fixed roll 1, and no active styles. Use the verified native combat runtime. Run existing Ni authoring, nine-copy, Ninth Seal, health-swap and permanent-Power tests through `build/run-tests.ps1 -NoBuild`; authenticate the preceding 442 backend passes/four intentional skips instead of rebuilding unchanged code.

Before execution, pass dedicated Python guards for exact selection, raw build preservation, resource bounds, report parity, copy lifecycle, source attribution, Power notifications, health-swap accounting and verified log compression. Keep prior exact-report/event-accounting guards. Freeze the protocol, owner, tests, source catalog, runtime and ledgers in a declaration before the first replay.

Require each detailed replay to equal the complete saved report after removing only `eventLog`, including the native replay-success footer. Reconcile damage, mitigation, actual healing, regeneration and death timing for all original characters, Ni and his copies. Separate original party, friendly summons, Ni and hostile copies. Copies must remain inert.

Measure [0,16), [16,32), [32,48), [48,end) time windows and the complete battle, plus an event-order split immediately before the first original-party Death (the killing hit belongs to the preceding partition). Record the attack preceding every original-party death, Ninth Seal hits, copy spawn/end notifications, permanent-Power increments, actual health swaps, damage to Ni versus copies and output before/after casualties. Copy counts follow observed event order. Power increments are not a reconstruction of current Power after other modifiers. A health-swap Buff targets the copy but its magnitude is Ni's rounded Health gain; it is not healing received by that copy. Equipment changes multiple attributes, and temporal association does not prove a counterfactual effect.

Bound execution to **96 replays / 840 seconds / 2 GiB**, with **60 seconds / 16 MiB per native log**. Resource admission uses the authenticated completed 96-replay floor-8 diagnosis: double its **196.670 seconds** and **176,289,803 archive bytes**, plus an 80-MiB transient-log/report allowance. Estimates must stay below **672 seconds / 80% of 2 GiB**, with free disk exceeding the projected archive plus 2 GiB. This yields **393.34 seconds / 436,465,686 bytes**. The reference's largest raw log exceeded 16 MiB; that does not change the Ni per-log cap. Saved Ni sample durations range from 32.1 to 72.1 seconds; size is still enforced independently. A limit failure preserves completed work and stops the run without an automatic retry.

Use the established bounded Windows process owner. Compress each complete native log, verify decompressed bytes and hashes, then remove only that owner's temporary raw log. Retain process and cleanup receipts. During native work observe supervisor stdout only. Independently recount all 96 closed logs, raw outcomes, event totals, selection, source/runtime hashes, resource receipts and unchanged seed union before reporting results. No successful replay should be repeated for reporting.

## Evidence locations

- Authenticated entry and five native tests: `TestResults/tower-floor9-ni-entry-20261001`.
- Python safeguards: `TestResults/tower-floor9-ni-tests-20261001`.
- Bounded owner and replay archive: `TestResults/tower-floor9-ni-diagnostic-20261001`.
- Independent evidence: `TestResults/tower-floor9-ni-evidence-20261001.json`.

No migration, application configuration change, database action or deployment is planned. Keep floor 8 and all preceding accepted changes. Continue floor 9 before floors 10 and 12–15 and the final current-version 1–15 sweep; stay on Tower balancing.

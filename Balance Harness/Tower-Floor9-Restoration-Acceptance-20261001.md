# Floor 9: fixed Restoration acceptance study — 1 October 2026

## Completed and applied

**Floor 9 passed the declared limited-equipment balance gate and was applied locally.** Full screening and independent confirmation each used 512 fresh seeds across all 163 recipes. Three distinct compositions qualified in both phases, and every retained recipe passed the 50% upper-bound ceiling.

Only Ni's guardian penetration scaling changes **1 → 40** (native ArmorPenetration/MagicPenetration **0.96 → 38.4**). Original offense **4.7036132812**, Health **2.970703125**, 10% copy Health, corrected inherited defense and the entire kit stay unchanged. All other floors and catalogs are preserved.

| Eight-item composition | Screening | Independent confirmation | Adjusted confirmation interval | Both phases qualify |
| --- | ---: | ---: | --- | --- |
| reference-1 | 68/512 | 59/512 | 7.36%–17.60% | No |
| reference-2 | 20/512 | 26/512 | 2.56%–9.83% | No |
| reference-3 | 91/512 | 109/512 | 15.51%–28.49% | Yes |
| 5b6c297c… | 95/512 | 96/512 | 13.33%–25.72% | Yes |
| 4a9f8b9f… | 95/512 | 92/512 | 12.66%–24.86% | Yes |

These parties specialize MainHand, Chest, Head and Necklace on healer slots 2 and 7, retaining their baseline rings/relics and the rest of the party. The family contains **163 raw recipes / five actual compositions / 130 equipment-eligible recipes**. The strongest confirmation control is full Health/Regeneration at **178/512**; its adjusted upper bound is **42.66%**, below 50%. The screen's maximum upper bound was **39.03%**. This acceptance applies to the declared family and progression budget, not every possible loadout or a finished 1–15 balance sweep.

## Verification and evidence

- **166,912 fresh fights / 1,024 fresh reservations** completed. An independent collector verified every raw outcome, prepared participant set, adjusted interval, eligibility decision, and phase verdict. No retry, extension, replacement seed, pooled historical result or dropped control. Final exclusion union: **927,628**.
- All **83,456 confirmation input hashes and 5,216 full historical battles** matched the isolated candidate. Application copied the exact verified bytes to the live repository catalog and checked every catalog hash, all consumed Tower settings and the unchanged production runtime. Fresh post-application verification matched all **163 live participant sets** to the nominated candidate and passed **364 native tests / zero skips** through `build/run-tests.ps1`. **326 preparations** ran before the trial and another 326 after application; no extra study seeds or replay panel was allocated. Historical replay total stays **5,216**, within the frozen budget.
- **507 Python checks pass**, including the strict candidate/layout/equipment/confirmation and existing diagnostic/application guards. The initial application preflight stopped before editing because sanitized offline settings differ byte-for-byte from live appsettings. Comparing all settings consumed by `TowerBundle.ReadSettings` confirmed equivalence; live appsettings stayed unchanged. A final qualification mapping requires the new accepted aggregate's Tower hash as well as its abilities hash; the closed earlier helper bytes remain in an explicit post-closure snapshot. No native fight or statistical result changed.
- The broader prior proof of **766 backend passes / four skips** remains authenticated historical evidence. Its known pre-existing Kharad behavior-manifest diagnostic failure remains recorded; this work neither reran that broad suite nor claims to fix it.
- No migration, database operation, appsettings/environment configuration change or deployment. The gameplay edit is local repository data only. No required verification command remains blocked.

Implementation files: `tower-ni-restoration-acceptance.py` and its test; explicit candidate dispatch and owner helper copies; aggregate layout, confirmation, equipment and applied-catalog guards; `check-tower-balance-application.py`; native `NiRestorationAcceptanceTests.cs` and `BalanceHarnessTowerBalanceApplicationTests.cs`; `LL/src/API/API.LL/Data/world-tower/tower-floors.json`; this report and status/handoff/gear-coverage/harness guides. The generic penetration guard and old rejected/diagnostic contracts remain unchanged.

Commands and exact test filters are archived in `TestResults/tower-floor9-restoration-acceptance-runtime-verification-20261001` and `tower-floor9-restoration-acceptance-post-verification-20261001`. Python receipts are in `tower-floor9-restoration-acceptance-tests-20261001` and the post-closure regression directory. The driver and collector both parsed before freezing. Completed study owners must not be rerun.

Independent evidence: `TestResults/tower-floor9-restoration-acceptance-evidence-20261001.json`, SHA-256 `beda9301e4c3c02dad1373c3f960a5e893c26e13abb53df0da51285eff8e916b`.

Declaration: `TestResults/tower-floor9-restoration-acceptance-driver-20261001/declaration.json`, SHA-256 `b7a012079b0e6f65c3b3955936a654102796ddcd8b1552d14bf28254efd2e125`.

Application: `TestResults/tower-floor9-restoration-acceptance-application-20261001/completion.json`, SHA-256 `2e962f8e53181311e026312ad3f0a143336064b75fb56d74e6033eea6c81ba88`. The adjacent application declaration binds exact before/after catalog hashes, runtime, all isolated parity receipts and the fresh native post-check.

Publication: `TestResults/tower-floor9-restoration-acceptance-publication-20261001/completion.json`.

## Next

Review floor 10's saved family and limited-equipment coverage, then qualify the complete retained family against the newly applied catalog and corrected runtime before new combat. No next-floor candidate, fight or seed is allocated. After floor 10, floors 12–15 and the final current-version 1–15 sweep remain. Keep the supported search and approved expected progression unchanged.

## Frozen protocol

The complete offense diagnostic nominated one setting: **original Ni offense ×1.00, penetration ×40**. Keep every original recipe in order: **163 raw recipes / five actual compositions / 130 equipment-eligible recipes**. Preserve minimum-level expected progression, the repeating gear curve, Essence/actor/equipment order, 10% copy Health, corrected inherited defense and the full kit.

Run **eight 64-seed batches for screening** (512 samples per recipe, 83,456 fights). Only a complete passing screen permits **eight independent 64-seed confirmation batches**. Both phases were specified before allocation: maximum **166,912 fresh fights / 1,024 reservations**. Exclude all **926,604** preceding reservations. No diagnostic or historical outcomes transfer. No interim selection, retries, extensions, replacement seeds, extra candidates or dropped controls.

Each phase uses the unchanged approximate simultaneous 95% Bonferroni-Wilson bounds across all 163 recipes: at least two distinct equipment-eligible compositions with lower bound at least 10%, and every recipe with upper bound at most 50%. At 512 samples, this requires at least **76 wins** per qualifying recipe and at most **215 wins** on every recipe. Eligible means at most eight specialized items across at most two characters; recipe labels do not create compositions.

Admit each batch only below doubled measured time/byte limits and remaining projected disk plus 2 GiB. Limits stay 840 native seconds / 900 owner seconds / 2 GiB, with admission below 80%. A failed admission or execution stops the frozen trial without replacement. Verify every archive, raw outcome and native prepared participant. During native runs, observe only supervisor stdout; do not inspect active files or edit pinned inputs.

Only after independently passing both phases may local application proceed. First match all **83,456 confirmation inputs** and **5,216 full historical replays** against isolated content (first four saved seeds per batch for each recipe). After local application, verify the same parity and relevant backend regression. No deployment or database changes.

Implementation adds a separate exact candidate and aggregate version. Existing diagnostics, the rejected 0.95/148-recipe contract and generic penetration guard remain unchanged. **507 Python checks and 364 native tests / zero skips pass**; **326 zero-fight preparations** match every nominated participant and confirm that only guardian ArmorPenetration/MagicPenetration change. Production assembly hashes remain unchanged. The historical broader 766-pass/four-skip proof and known pre-existing Kharad manifest failure are retained.

Proposal: `TestResults/tower-floor9-restoration-acceptance-proposal-20261001.json`, SHA-256 `31d2fd371ad65b4c101a5f13d3f0bc73115efdbcdfcb459e5e5ae04db1b80c29`.

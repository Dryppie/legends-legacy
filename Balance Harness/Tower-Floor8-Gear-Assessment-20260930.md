# Floor 8 equipment assessment and current-version qualification — 30 September 2026

**Subsequent floor-8 trial:** **Latest floor-8 limited-armor result (30 September):** The [complete trial](Tower-Floor8-Limited-Armor-20260930.md) closed **`LimitedArmorScreenNotAccepted`** after **22,656 fresh fights / 128 reservations**, retaining **177 recipes / nine actual compositions**. The final phase has **0 qualifying limited-equipment compositions** and **0 ceiling failures** (largest adjusted upper **44.87%**). **157 Python cases and 214 backend cases / four intentional skips pass**, plus 4 native study fixtures. No gameplay edit. Exclusions: **923,900**. Floor 8 remains unresolved; next is the proposed, unallocated 96-historical-replay diagnostic. No coefficient selected or new diagnostic seed. The older protocols and results below remain historical.

**The review and native qualification are complete; floor 8 remains equipment-dependent.** Its two accepted historical compositions use **40 specialized items on all ten characters**, winning **47/160** and **33/160**. All six alternative saved gear profiles, including baseline, win **0/160** for both compositions. The current version reproduces all **10,720 historical inputs / 67 full saved-seed replays**. No guardian or ability value changed.

## What the saved battles establish

The full accepted family contains **67 exact recipes / nine actual compositions**. Every archived member and all **10,720 outcomes** were authenticated and recounted. Detailed review covers **2,240 reports** for both leading compositions across seven gear profiles. These reports contain summary statistics, not event timelines; this review does not claim to identify the damage source responsible for the first death.

| Gear profile | Specialized items / characters | Composition A wins | Composition B wins | Median first casualty, A / B |
| --- | ---: | ---: | ---: | ---: |
| Armor + Health | 40 / 10 | 47/160 | 33/160 | 121.20s / 118.40s |
| Baseline | 0 / 0 | 0/160 | 0/160 | 60.25s / 66.00s |
| Precision | 20 / 10 | 0/160 | 0/160 | 66.35s / 70.80s |
| Ability Haste | 10 / 10 | 0/160 | 0/160 | 71.45s / 66.80s |
| Restorer specialization | 12 / 2 | 0/160 | 0/160 | 94.90s / 82.55s |
| Resistance + Health | 40 / 10 | 0/160 | 0/160 | 59.10s / 60.90s |
| Health + Regeneration | 30 / 10 | 0/160 | 0/160 | 75.95s / 76.55s |

A is original composition `049002811aed3c4effada8495158670086d9c55a1f900e81239832ac4769f27b`; B is `230ac3d6a65620dd44e949a10dd0083c3ce922069aa051f5ee8fb899bbd2b3ca`. Actual composition counts ignore equipment and Essence order; raw recipes preserve both, along with identity fields and party positions.

The accepted historical adjusted intervals remain **18.93%–42.56%** and **11.97%–33.18%**, with the original 67-recipe Bonferroni-Wilson correction. All ceilings pass, but **zero limited-equipment compositions qualify**: the only nine recipes within eight specialized items on two characters are the nine baseline recipes. Restorer specialization equips two healers with six specialized items each, so it exceeds the item budget. Reproducing this evidence on the current runtime does not turn it into a fresh independent acceptance panel.

Armor + Health changes exactly Chest, Head, Legs and Necklace on each character. It also changes other attributes: for the inspected slot-1 build, Health rises **3,352 → 3,826**, Armor rises **44.29 → 60.06**, regeneration falls **99.44 → 70.21**, and Block/Tenacity are removed. The paired result supports investigating the whole equipment profile; it does not isolate Armor as the sole cause. No duration target has been approved.

## Progression and preserved gameplay

Floor 8 retains **ten level-40 characters, five level-1 unascended/unevolved Essences each, T1 Unique / Exceptional / rank-4 gear, fixed roll 1 and no styles**. Expected progression and hypothetical ownership assumptions remain explicit. Kodoku remains at health **9.9064526367**, offense **8.8260253906**, defense/resistance **2.61**, penetration/regeneration **1.0**. The accepted floor-7 change and all **102 live JSON catalogs** remain byte-for-byte unchanged.

## Implemented admission and qualified family

Added the separate `floor8-limited-armor-proposal-v1` validator. It retains all **67 original recipes** and exactly **110 new variants**: every one-character and two-character subset from each of the two leading armor compositions (**10 singles + 45 pairs per composition**). Each variant uses **four or eight specialized items**, preserves every other raw field, and includes all ten party slots. The complete family is **177 recipes / nine actual compositions**. This is the smallest exhaustive comparison of the declared one/two-character armor question; three-to-nine-character intermediates are outside this question, while the original full-party armor ceiling controls remain.

The owner now accepts this floor's seed-free qualified preparation. Existing floor-2/4/6/7 family contracts are preserved. **Fourteen additional Python cases** cover the ten-character family, later party slots, raw identity/order/budget preservation, missing/duplicate subsets, concealed specialization, and excessive equipment budgets.

Current-catalog qualification used the **complete applied floor-7 recovery aggregate**: both eight-batch phases, all eight live parity receipts and the application completion. It independently revalidated that provenance, then matched **10,720 native inputs and 67 complete historical battles** against the current catalog and pinned runtime. The historical archives were not rewritten. The accepted family then prepared natively with **177 exact recipes and zero fights**. Both native owners exited successfully with no timeout or active child.

## Next bounded experiment — proposed, not allocated

Test the prepared family against unchanged Kodoku before selecting a balance coefficient. Use **four complete-family batches of 32 fresh shared seeds per phase**, giving **128 observations per recipe**. Every batch retains all 177 recipes. Decide only after the complete phase; no batch-level selection or early acceptance.

- Screen: **22,656 fresh fights / 128 reservations**. Require at least two actual compositions to qualify with **at most eight specialized items on two characters**. Every recipe, including full-armor controls, must pass its upper ceiling.
- Bounds: original approximate simultaneous 95% Bonferroni-Wilson method across all **177 recipes**, lower **≥10%**, every upper **≤50%**. Exact count gates: **26–43 wins / 128**.
- Only a full screen pass permits one independent identical confirmation. Each phase stands alone. Maximum: **45,312 fresh fights / 256 reservations**. No historical pooling, retries, replacement seeds, extension, changed candidate or dropped controls.
- Each native batch has **5,664 fights**. Doubled historical resource estimates are **546.84 seconds / 361,609,528 bytes**, below **672 seconds / 80% of 2 GiB**. Retain 840-second native and 900-second owner limits, 20,000-fight ceiling, and the free-disk reserve. Recheck admission using the latest closed measured batch before each allocation.

**Before allocating that experiment, implement a separate four-by-32 complete-family aggregate contract and matching native guards.** The current four/eight-by-128 aggregate versions must remain unchanged. The resource split is necessary: a single 177 × 128 panel would exceed the native 20,000-fight limit. No trial batch path or seed is allocated by this review. Failed execution or a failed complete phase closes the declared scope; keep all reservations and evidence.

## Verification and evidence

**109 distinct Python checks pass**: reviewer 6, catalog/family qualification 64, recovery aggregate 10, reference coverage 29. The native qualification invocation passed **46 backend cases**; the seed-free preparation passed its native fixture. All backend execution used `build/run-tests.ps1`. The preceding **194 backend passes / four intentional skips** were authenticated and reused because production code, C# test code and gameplay data did not change; they are not reported as a fresh 194-case run.

An independent collector reconstructs every proposed raw variant, actual specialized-item count, whole-family identity, paired outcome total, native receipt, prepared family and seed union. **Zero new acceptance fights / zero new reservations**; **67 historical combat replays** ran for qualification. Exclusions remain **923,772**. No required verification command is blocked.

- Review and saved source snapshots: `TestResults/tower-floor8-gear-review-20260930`.
- Source manifest: `0218835433af0414f788d5624b8c67f05b05a6995c7aa7ce8c20cdfc4a2f9947`.
- Exact proposed family SHA: `7b4efc970724b05d5386190e37b2170f269e2f18389825c2f2fe56d4ef86ec8c`.
- Qualified preparation manifest: `5092e4c65735416affaf4dab4a083e920d88827d20d6fde325abc29876380ae3`.
- Independent evidence: `TestResults/tower-floor8-gear-evidence-20260930.json`, SHA `62a87bc416d260d07058d734c917848c61278d31841f1e8399ed32243cb2544d`.
- Frozen next-trial proposal: `TestResults/tower-floor8-limited-armor-trial-proposal-20260930.json`, SHA `b0b239c05f229bd42d6156d439a2f574ccab601e93fa51ac50e6ffd13646db8f`.
- Current publication: `TestResults/tower-floor8-gear-publication-check-20260930.json`.

Executed the bundled Python runtime with `-B -X utf8`: `tower-floor8-gear-review-20260930.py`, the four maintained test suites, `tower-floor8-gear-qualify-20260930.py`, `tower-floor8-gear-collect-20260930.py`, and this publication script. Fresh execution paths are mandatory; completed archives are immutable. Markdown links and `git diff --check` are verified at publication.

Changed maintained files: the catalog-family validator, its tests, the owner CLI floor admission, this report, continuation handoff, balance status, gear coverage, both harness guides and follow-up notices in the preceding floor-7/8 reports. No production combat/search code, guardian data, migration, environment configuration, database action or deployment changed. Floors **8–10 and 12–15**, then the final current-version 1–15 sweep, remain. No dungeon or acquisition work is included.
